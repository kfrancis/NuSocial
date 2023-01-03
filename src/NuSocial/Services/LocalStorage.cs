using SQLite;

namespace NuSocial.Services
{
    public interface IDatabase
    {
        Task DeleteAllDataAsync();

        Task<User?> GetUserAsync(string key);

        Task<ObservableCollection<User>> GetUsersAsync();

        Task UpdateUserAsync(User user);

        Task UpdateUsersAsync(ObservableCollection<User> users);
    }

    /// <summary>
    /// Local storage manager for storing databases.
    /// </summary>
    public class LocalStorage : IDatabase, IDisposable
    {
        public const SQLite.SQLiteOpenFlags Flags =
            // open the database in read/write mode
            SQLite.SQLiteOpenFlags.ReadWrite |
            // create the database if it doesn't exist
            SQLite.SQLiteOpenFlags.Create |
            // enable multi-threaded database access
            SQLite.SQLiteOpenFlags.SharedCache;

        private readonly Task _constructionTask;

        private SemaphoreSlim? _semaphore = new(1, 1);

        /// <summary>
        /// The database connection.
        /// </summary>
        private SQLiteAsyncConnection? _database;

        private bool _isInitialized;
        private bool _isDisposed;

        /// <summary>
        /// Initializes the database manager.
        /// </summary>
        public LocalStorage() : this(Path.Combine(FileSystem.AppDataDirectory, "NuSocial.db3"))
        {
        }

        public LocalStorage(string databasePath)
        {
            _database = new SQLiteAsyncConnection(databasePath, Flags);

            _constructionTask = Task.Run(async () =>
            {
                // CabMD-related tables
                await Setup<User>();

                _isInitialized = true;
            });
        }

        /// <summary>
        /// Deletes and resets all database data.
        /// </summary>
        public async Task DeleteAllDataAsync()
        {
            // create tables if they do not exist
            await Setup<User>();

            var db = await GetDatabase();

            // delete data
            await db.DeleteAllAsync<User>();
        }

        public async Task<User?> GetUserAsync(string key)
        {
            var db = await GetDatabase();
            return await db.Table<User>().FirstOrDefaultAsync(x => x.Key == key);
        }

        public async Task<ObservableCollection<User>> GetUsersAsync()
        {
            var db = await GetDatabase();
            var results = await db.Table<User>().ToListAsync();
            return new ObservableCollection<User>(results ?? new List<User>());
        }

        public Task UpdateUserAsync(User user)
        {
            return UpdateIfPossible(user);
        }

        public Task UpdateUsersAsync(ObservableCollection<User> users)
        {
            return UpdateIfPossible(users);
        }

        /// <summary>
        /// Checks to see if the table is consistant, drop and reset if it isn't
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
                // What we're doing here is trying to access something very innocuous
                // to verify that we can normally access the table/table-data. If we
                // can't, then we know that something has to be done to reconcile the
                // table (ie. deleting it).
                if (await _database.Table<T>().FirstOrDefaultAsync() == null)
                {
                    reset?.Invoke();
                    await _database.DropTableAsync<T>();
                }
            }
            catch (Exception)
            {
                reset?.Invoke();
                await _database.DropTableAsync<T>();
            }
        }

        private async Task<SQLiteAsyncConnection> GetDatabase()
        {
            _database ??= new SQLiteAsyncConnection(Path.Combine(FileSystem.AppDataDirectory, "Data.db3"), Flags);

            if (!_isInitialized)
            {
                await _semaphore!.WaitAsync();
                try
                {
                    await _constructionTask;
                    _isInitialized = true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            return _database;
        }

        /// <summary>
        /// Setup a table by dropping the table (if need be) and creating the table new
        /// </summary>
        /// <typeparam name="T">The type to become a table</typeparam>
        /// <param name="reset">An action used when resetting the app after dropping a table</param>
        /// <returns>The task</returns>
        private async Task Setup<T>(Action? reset = null)
            where T : new()
        {
            ArgumentNullException.ThrowIfNull(_database);

            await DropAndReset<T>(reset);
            await _database.CreateTableAsync<T>();
        }

        private async Task UpdateIfPossible<T>(T item)
        {
            var db = await GetDatabase();
            await db.RunInTransactionAsync((dbc) =>
            {
                dbc.InsertOrReplace(item);
            });
        }

        private async Task UpdateIfPossible<T>(ObservableCollection<T> items)
        {
            if (items.Any())
            {
                var db = await GetDatabase();
                await db.RunInTransactionAsync((dbc) =>
                {
                    for (var i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        if (item == null) continue;

                        dbc.InsertOrReplace(item);
                    }
                });
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _semaphore?.Dispose();
                }

                _semaphore = null;
                _isDisposed = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~LocalStorage()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}