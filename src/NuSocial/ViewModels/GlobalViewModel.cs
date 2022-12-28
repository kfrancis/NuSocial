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
using Contact = NuSocial.Models.Contact;

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

        

        public override Task<IEnumerable<NostrPost>> GetPosts()
        {
            return NostrClient.GetGlobalPostsAsync();
        }

        public override async void OnAppearing()
        {
            base.OnAppearing();

            await LoadDataAsync();
        }
    }
}
