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

        public string UnreadLabel => $"Load {(UnreadPostCount > 1000 ? "many" : UnreadPostCount)} unread";

        public INostrClient NostrClient => _nostrClient;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(UnreadLabel))]
        private int _unreadPostCount;

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

        public override async Task InitializeAsync()
        {
            if (_nostrClient == null) return;

            await _nostrClient.DisconnectAsync().ConfigureAwait(true);
            await _nostrClient.ConnectAsync().ConfigureAwait(true);
        }

        public virtual Task<IEnumerable<NostrPost>> GetPosts()
        {
            throw new NotImplementedException();
        }

        public Task LoadDataAsync()
        {
            return SetBusyAsync(async () =>
            {
                await NostrClient.SetRelaysAsync(_settingsService.GetRelays());

                var posts = await GetPosts().ConfigureAwait(true);
                if (posts != null && posts.Any())
                {
                    await Dispatcher.RunAsync(() =>
                    {
                        AddItems(posts.OrderBy(x => x.CreatedAt));
                    }).ConfigureAwait(true);
                }
                else
                {
                    var message = $"Nothing new to fetch";
                    var toast = Toast.Make(message, ToastDuration.Long, 30);
                    await toast.Show().ConfigureAwait(false);
                }
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
