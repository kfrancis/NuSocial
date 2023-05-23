using Polly;
using SQLite;
using System.Reflection;

namespace NuSocial.Services
{
    public interface IDatabase
    {
        Task DeleteAllDataAsync();

        Task<ObservableCollection<Relay>> GetRelaysAsync();

        Task<ObservableCollection<User>> GetUsersAsync();

        Task UpdateRelaysAsync(ObservableCollection<Relay> relays);

        Task UpdateUsersAsync(ObservableCollection<User> users);
    }

    /// <summary>
    /// Local storage manager for storing databases.
    /// </summary>
    public class LocalStorage : IDatabase, IAsyncDisposable
    {
        public const SQLite.SQLiteOpenFlags Flags =
            SQLiteOpenFlags.FullMutex | // multiple threads can safely attempt to use the same database connection at the same time
            SQLiteOpenFlags.ReadWrite | // open the database in read/write mode
            SQLiteOpenFlags.Create;

        private const string _databaseFilename = "NuSocial.db3";
        private readonly string _databasePath;
        private readonly SemaphoreSlim _lock = new(1, 1);

        private readonly SemaphoreSlim _semaphore = new(1, 1);

        // create the database if it doesn't exist
        private Task? _constructionTask;

        /// <summary>
        /// The database connection.
        /// </summary>
        private SQLiteAsyncConnection? _database;

        private bool _isDisposed;
        private bool _isInitialized;

        /// <summary>
        /// Initializes the database manager.
        /// </summary>
        public LocalStorage() : this(Path.Combine(FileSystem.AppDataDirectory, _databaseFilename))
        {
        }

        public LocalStorage(string databasePath)
        {
            _database = new SQLiteAsyncConnection(databasePath, Flags);
            _databasePath = databasePath;
        }

        public async Task CloseDatabaseAsync()
        {
            await _lock.WaitAsync().ConfigureAwait(false);

            if (_database == null)
            {
                _lock.Release();
                return;
            }

            try
            {
                await _database.CloseAsync().ConfigureAwait(false);
                _database = null;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Deletes and resets all database data.
        /// </summary>
        public async Task DeleteAllDataAsync()
        {
            // delete data
            try
            {
                await _lock.WaitAsync().ConfigureAwait(false);

                await Task.WhenAll(
                    //DeleteAllOfType<Service>(),
                    Task.CompletedTask
                ).ConfigureAwait(false);
            }
            finally
            {
                _lock.Release();
            }

            // reset data versions
            Config.ResetVersions();

            async Task DeleteAllOfType<T>(Action? reset = null) where T : class, new()
            {
                var db = await GetDatabaseConnection<T>(reset);
                await db.DeleteAllAsync<T>().ConfigureAwait(false);
            }
        }

        public async ValueTask DisposeAsync()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            await DisposeAsync(disposing: true).ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }

        public async Task<ObservableCollection<T>> GetItemsAsync<T>(Action? reset = null) where T : class, new()
        {
            try
            {
                var db = await GetDatabaseConnection<T>(reset).ConfigureAwait(false);
                await _lock.WaitAsync().ConfigureAwait(false);
                List<T>? items = await AttemptAndRetry(() => { return db.Table<T>().ToListAsync(); }).ConfigureAwait(false);
                return new ObservableCollection<T>(items);
            }
            finally
            {
                _lock.Release();
            }
        }

        public Task<ObservableCollection<Relay>> GetRelaysAsync() => GetItemsAsync<Relay>();

        /// <summary>
        /// Gets data elements from the database.
        /// </summary>
        public Task<ObservableCollection<User>> GetUsersAsync() => GetItemsAsync<User>();

        public Task UpdateRelaysAsync(ObservableCollection<Relay> relays) => UpdateIfPossible(relays);

        /// <summary>
        /// Updates data elements in the database.
        /// </summary>
        public Task UpdateUsersAsync(ObservableCollection<User> users) => UpdateIfPossible(users);

        protected virtual async Task DisposeAsync(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    await CloseDatabaseAsync().ConfigureAwait(false);

                    _constructionTask?.Dispose();
                    _constructionTask = null;
                    _semaphore?.Dispose();
                    _lock?.Dispose();
                }

                _isDisposed = true;
            }
        }

        protected async Task<SQLiteAsyncConnection> GetDatabaseConnection<T>(Action? reset = null) where T : class, new()
        {
            _database ??= new SQLiteAsyncConnection(_databasePath, Flags);
            if (_database.TableMappings.All(x => x.MappedType.Name != typeof(T).Name))
            {
                await _database.EnableWriteAheadLoggingAsync();
                await _database.CreateTableAsync<T>();
                if (reset != null)
                {
                    await reset.InvokeAsync();
                }
            }

            return _database;
        }

        private static Task<T> AttemptAndRetry<T>(Func<Task<T>> action, int numRetries = 10)
        {
            return Policy.Handle<SQLite.SQLiteException>().WaitAndRetryAsync(numRetries, pollyRetryAttempt).ExecuteAsync(action);

            static TimeSpan pollyRetryAttempt(int attemptNumber) => TimeSpan.FromMilliseconds(Math.Pow(2, attemptNumber));
        }

        private static Task AttemptAndRetry(Func<Task> action, int numRetries = 10)
        {
            return Policy.Handle<SQLite.SQLiteException>().WaitAndRetryAsync(numRetries, pollyRetryAttempt).ExecuteAsync(action);

            static TimeSpan pollyRetryAttempt(int attemptNumber) => TimeSpan.FromMilliseconds(Math.Pow(2, attemptNumber));
        }

        [Conditional("DEBUG")]
        private static void DebugLogging(ISQLiteAsyncConnection db)
        {
            db.Trace = true;
            db.Tracer = (msg) =>
            {
                if (!string.IsNullOrEmpty(msg))
                {
                    //_testOutputHelper?.WriteLine(msg);
                    Debug.WriteLine(msg);
                }
            };
            db.TimeExecution = true;
        }

        /// <summary>
        /// Checks to see if the table is consistent, drop and reset if it isn't
        /// </summary>
        /// <typeparam name="T">The type of table</typeparam>
        /// <param name="reset">An action used when resetting the app after dropping a table</param>
        /// <returns>The task</returns>
        private async Task DropAndReset<T>(Action? reset = null)
            where T : new()
        {
            ArgumentNullException.ThrowIfNull(_database);
            
            try
            {
                await _lock.WaitAsync().ConfigureAwait(false);
                var entityType = typeof(T);
                var entityProperties = entityType.GetProperties();
                var needsReset = false;

                // What we're doing here is trying to verify that the type we
                // are checking has all the expected columns (the properties of the underlying type).
                var columns = await _database.GetTableInfoAsync(typeof(T).Name).ConfigureAwait(false);
                foreach (PropertyInfo property in entityProperties)
                {
                    // If we can't find every column, then we need to reset the table for this type.
                    var columnName = property.Name;
                    var matchingColumn = columns.FirstOrDefault(x => x.Name == columnName);
                    if (matchingColumn == null)
                        needsReset = true;
                }

                if (needsReset)
                {
                    if (reset != null)
                        await reset.InvokeAsync().ConfigureAwait(false);

                    await _database.DropTableAsync<T>().ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                if (reset != null)
                    await reset.InvokeAsync().ConfigureAwait(false);

                await _database.DropTableAsync<T>().ConfigureAwait(false);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task UpdateIfPossible<T>(ObservableCollection<T> items, Action? reset = null)
            where T : class, new()
        {
            if (items.Any())
            {
                try
                {
                    var db = await GetDatabaseConnection<T>(reset).ConfigureAwait(false);
                    await _lock.WaitAsync().ConfigureAwait(false);
                    await AttemptAndRetry(() =>
                    {
                        return db.RunInTransactionAsync((dbc) => { foreach (var item in items) { dbc.InsertOrReplace(item); } });
                    }).ConfigureAwait(false);
                }
                finally
                {
                    _lock.Release();
                }
            }
        }
    }
}