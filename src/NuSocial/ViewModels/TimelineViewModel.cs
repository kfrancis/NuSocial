using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using NostrLib;
using NostrLib.Models;
using NuSocial.Core.Threading;
using NuSocial.Messages;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading.RateLimiting;
using Contact = NuSocial.Models.Contact;

namespace NuSocial.ViewModels;

public partial class TimelineViewModel : PostListViewModel
{
    public TimelineViewModel(IDialogService dialogService,
                             ICustomDispatcher customDispatcher,
                             ISettingsService settingsService,
                             INostrClient nostrClient)
        : base(dialogService, customDispatcher, settingsService, nostrClient)
    {
    }

    [ObservableProperty]
    private string _key = string.Empty;

    public override Task<IEnumerable<NostrPost>> GetPosts(CancellationToken cancellationToken = default)
    {
        return NostrClient.GetPostsAsync(cancellationToken);
    }

    [RelayCommand(CanExecute = "IsNotBusy")]
    public Task GoToDetails(Post item)
    {
        return SetBusyAsync(() =>
        {
            return Shell.Current.GoToAsync(nameof(TimelineDetailPage), true, new Dictionary<string, object>
            {
                { "Item", item }
            });
        });
    }

    public override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadDataAsync().ConfigureAwait(false);
    }
}