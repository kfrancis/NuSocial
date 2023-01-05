using NostrLib;
using NostrLib.Models;
using NuSocial.Core.Threading;

namespace NuSocial.ViewModels
{
    public partial class GlobalViewModel : PostListViewModel
    {
        private readonly ISettingsService _settingsService;

        public GlobalViewModel(IDialogService dialogService,
                               ICustomDispatcher customDispatcher,
                               ISettingsService settingsService,
                               INostrClient nostrClient,
                               IAuthorService authorService)
            : base(dialogService, customDispatcher, settingsService, nostrClient, authorService)
        {
            _settingsService = settingsService;

            IsAsync = false; // Stream posts
        }

        public override Task StartGettingPosts(CancellationToken cancellationToken = default)
        {
            return NostrClient.GetGlobalPostsAsync(cancellationToken: cancellationToken);
        }

        public override async void OnAppearing()
        {
            base.OnAppearing();

            await LoadDataAsync().ConfigureAwait(false);
        }
    }
}
