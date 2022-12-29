using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using NostrLib;
using NostrLib.Models;
using NuSocial.Core.Threading;

namespace NuSocial.ViewModels
{
    /// <summary>
    /// Pages that show lists of nostr posts can use this as a base class
    /// </summary>
    public partial class PostListViewModel : BaseViewModel, IDisposable
    {
        private readonly INostrClient _nostrClient;
        private readonly ISettingsService _settingsService;
        private CancellationTokenSource? _clientCts;

        private bool _disposedValue;

        [ObservableProperty]
        private ObservableCollection<Post> _items = new();

        private readonly object _lock = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(UnreadLabel))]
        private int _unreadPostCount;

        public PostListViewModel(IDialogService dialogService, ICustomDispatcher customDispatcher, ISettingsService settingsService, INostrClient nostrClient) : base(dialogService, customDispatcher)
        {
            _settingsService = settingsService;
            _nostrClient = nostrClient;
        }

        ~PostListViewModel()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public INostrClient NostrClient => _nostrClient;
        public string UnreadLabel => $"Load {(UnreadPostCount > 1000 ? "many" : UnreadPostCount)} unread";

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Override me to provide an implementation that makes sense for the page you're on
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual Task<IEnumerable<NostrPost>> GetPosts(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task LoadDataAsync()
        {
            Debug.WriteLine("LoadDataAsync started");
            lock (_lock)
            {
                _clientCts ??= new();
            }
            return SetBusyAsync(async () =>
            {
                await NostrClient.SetRelaysAsync(_settingsService.GetRelays(), false, _clientCts.Token).ConfigureAwait(true);

                var posts = await GetPosts(_clientCts.Token).ConfigureAwait(true);
                await Dispatcher.RunAsync(async () =>
                {
                    if (posts != null && posts.Any())
                    {
                        AddItems(posts.OrderBy(x => x.CreatedAt));
                    }
                    else
                    {
                        var message = $"Nothing new to fetch";
                        await Snackbar.Make(message).Show(_clientCts.Token).ConfigureAwait(false);
                    }
                }).ConfigureAwait(true);

                Debug.WriteLine("LoadDataAsync ended");
            });
        }

        [RelayCommand(CanExecute = "IsNotBusy")]
        public virtual Task LoadUnreadPosts()
        {
            return Task.CompletedTask;
        }

        public override void OnAppearing()
        {
            base.OnAppearing();

            lock (_lock)
            {
                _clientCts ??= new();
            }
        }

        public override void OnDisappearing()
        {
            //lock (_lock)
            //{
            //    _clientCts?.Cancel();
            //}

            base.OnDisappearing();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _clientCts?.Dispose();
                }

                _disposedValue = true;
            }
        }

        private void AddItems(IOrderedEnumerable<NostrPost> nostrPosts)
        {
            if (nostrPosts is null)
            {
                throw new ArgumentNullException(nameof(nostrPosts));
            }

            Items.Clear();
            foreach (var nostrPost in nostrPosts)
            {
                var post = new Post()
                {
                    CreatedAt = nostrPost.CreatedAt ?? DateTime.UtcNow,
                    Content = nostrPost.Content,
                    Contact = new Models.Contact() { PublicKey = nostrPost.Author }
                };
                Items.Add(post);
            }
        }
    }
}
