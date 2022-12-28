using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using NostrLib;
using NostrLib.Models;
using NuSocial.Core.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuSocial.ViewModels
{
    public partial class PostListViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly INostrClient _nostrClient;
        private CancellationTokenSource? _clientCts;

        public string UnreadLabel => $"Load {(UnreadPostCount > 1000 ? "many" : UnreadPostCount)} unread";

        public INostrClient NostrClient => _nostrClient;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(UnreadLabel))]
        private int _unreadPostCount;
        private object _lock = new();

        [ObservableProperty]
        private ObservableCollection<Post> _items = new();

        public PostListViewModel(IDialogService dialogService, ICustomDispatcher customDispatcher, ISettingsService settingsService, INostrClient nostrClient) : base(dialogService, customDispatcher)
        {
            _settingsService = settingsService;
            _nostrClient = nostrClient;
        }

        [RelayCommand(CanExecute = "IsNotBusy")]
        public virtual Task LoadUnreadPosts()
        {
            return Task.CompletedTask;
        }

        public virtual Task<IEnumerable<NostrPost>> GetPosts(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
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
                if (posts != null && posts.Any())
                {
                    await Dispatcher.RunAsync(async () =>
                    {
                        AddItems(posts.OrderBy(x => x.CreatedAt));
                        await Snackbar.Make($"Found {posts.Count()} posts.", duration: TimeSpan.FromSeconds(5)).Show(_clientCts.Token).ConfigureAwait(false);
                    }).ConfigureAwait(true);
                }
                else
                {
                    var message = $"Nothing new to fetch";
                    var toast = Toast.Make(message, ToastDuration.Long, 30);
                    await toast.Show(_clientCts.Token).ConfigureAwait(false);
                }

                Debug.WriteLine("LoadDataAsync ended");
            });
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
