﻿using NostrLib;
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
                               INostrClient nostrClient)
            : base(dialogService, customDispatcher, settingsService, nostrClient)
        {
            _settingsService = settingsService;
        }

        public override Task<IEnumerable<NostrPost>> GetPosts(CancellationToken cancellationToken = default)
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