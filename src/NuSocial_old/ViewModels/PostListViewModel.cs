using System.Collections.Concurrent;
using CommunityToolkit.Maui.Alerts;
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
        private readonly IAuthorService _authorService;
        private readonly object _lock = new();
        private readonly INostrClient _nostrClient;
        private readonly ISettingsService _settingsService;
        private CancellationTokenSource? _clientCts;
        private ConcurrentQueue<NostrPost> _pending = new();

        private bool _disposedValue;

        [ObservableProperty]
        private string _filter;

        [ObservableProperty]
        private ObservableCollection<Post> _items = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(UnreadLabel))]
        private int _unreadPostCount;

        public PostListViewModel(IDialogService dialogService,
                                 ICustomDispatcher customDispatcher,
                                 ISettingsService settingsService,
                                 INostrClient nostrClient,
                                 IAuthorService authorService)
            : base(dialogService, customDispatcher)
        {
            _settingsService = settingsService;
            _nostrClient = nostrClient;
            _authorService = authorService;
        }

        protected bool IsAsync = true;

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

        /// <summary>
        /// Override me to provide an implementation that makes sense for the page you're on
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual Task StartGettingPosts(CancellationToken cancellationToken = default)
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

                if (IsAsync)
                {
                    var posts = await GetPosts(_clientCts.Token).ConfigureAwait(true);
                    await Dispatcher.RunAsync(async () =>
                    {
                        if (posts != null && posts.Any())
                        {
                            await AddItems(posts.OrderByDescending(x => x.CreatedAt), _clientCts.Token);
                        }
                        else
                        {
                            var message = $"Nothing new to fetch";
                            await Snackbar.Make(message).Show(_clientCts.Token).ConfigureAwait(false);
                        }
                    }).ConfigureAwait(true);
                }
                else
                {
                    await StartGettingPosts(_clientCts.Token);
                }

                Debug.WriteLine("LoadDataAsync ended");
            });
        }

        [RelayCommand(CanExecute = "IsNotBusy")]
        public virtual Task Refresh()
        {
            return Task.CompletedTask;
        }

        [RelayCommand(CanExecute = "IsNotBusy")]
        public virtual Task ReplyToPost(Post item)
        {
            return Task.CompletedTask;
        }

        [RelayCommand(CanExecute = "IsNotBusy")]
        public virtual Task Block(Post item)
        {
            return Task.CompletedTask;
        }

        [RelayCommand(CanExecute = "IsNotBusy")]
        public virtual Task BoostPost(Post item)
        {
            return Task.CompletedTask;
        }

        [RelayCommand(CanExecute = "IsNotBusy")]
        public virtual Task ReactToPost(Post item)
        {
            return Task.CompletedTask;
        }

        [RelayCommand(CanExecute = "IsNotBusy")]
        public virtual Task LoadUnreadPosts()
        {
            return Task.CompletedTask;
        }

        [ObservableProperty]
        private bool _isRefreshing = false;

        public override void OnAppearing()
        {
            base.OnAppearing();

            lock (_lock)
            {
                _clientCts ??= new();

                // sub
                _nostrClient.PostReceived += NostrPostReceived;
            }
        }

        private void NostrPostReceived(object? sender, NostrPostReceivedEventArgs e)
        {
            _pending.Enqueue(e.Post);
            UnreadPostCount++;
        }

        public override void OnDisappearing()
        {
            lock (_lock)
            {
                // unsub
                _nostrClient.PostReceived -= NostrPostReceived;
            }

            base.OnDisappearing();
        }

        [RelayCommand(CanExecute = "IsNotBusy")]
        public async Task PerformFilter(string filter)
        {
            await SetBusyAsync(async () =>
            {
                if (Items.Count > 0 && !string.IsNullOrEmpty(filter))
                {
                    var filterItems = Items.Where(x => x.Content.Contains(filter, StringComparison.OrdinalIgnoreCase));
                    await Dispatcher.RunAsync(() =>
                    {
                        Items.Clear();
                        foreach (var item in filterItems)
                        {
                            Items.Add(item);
                        }
                    });
                }
            });
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

        private async Task AddItems(IOrderedEnumerable<NostrPost> nostrPosts, CancellationToken cancellationToken = default)
        {
            if (nostrPosts is null)
            {
                throw new ArgumentNullException(nameof(nostrPosts));
            }

            Items.Clear();
            foreach (var nostrPost in nostrPosts)
            {
                var author = await _authorService.GetInfoAsync(nostrPost.Author, cancellationToken);
                var contact = new Models.Contact() { PublicKey = nostrPost.Author, PetName = author.DisplayName ?? author.Name ?? string.Empty };
                if (!string.IsNullOrEmpty(author.Picture))
                {
                    contact.Picture = new Picture(new Uri(author.Picture));
                }

                contact.Nip05 = author.Nip05;
                if (!string.IsNullOrEmpty(contact.Nip05) && !string.IsNullOrEmpty(author.DisplayName) && contact.Nip05.StartsWith(author.DisplayName, StringComparison.OrdinalIgnoreCase))
                {
                    contact.Nip05 = contact.Nip05.Replace(author.DisplayName, string.Empty, StringComparison.OrdinalIgnoreCase);
                }

                var post = new Post()
                {
                    CreatedAt = nostrPost.CreatedAt ?? DateTime.UtcNow,
                    Content = nostrPost.Content,
                    Contact = contact
                };
                Items.Add(post);
            }
        }
    }
}
