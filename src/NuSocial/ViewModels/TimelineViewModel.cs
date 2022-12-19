using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Messaging;
using NNostr.Client;
using NuSocial.Core.Threading;
using NuSocial.Messages;

namespace NuSocial.ViewModels;

public partial class TimelineViewModel : BaseViewModel, IRecipient<NostrEventsChangedMessage>
{
    private readonly NostrService _nostr;
    private readonly SampleDataService dataService;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private ObservableCollection<Post> items;

    public TimelineViewModel(SampleDataService service)
    {
        dataService = service;
    }

    public TimelineViewModel(SampleDataService service, NostrService nostr, IDialogService dialogService, ICustomDispatcher customDispatcher) : base(dialogService, customDispatcher)
    {
        dataService = service;

        _nostr = nostr;
        _nostr.NoticeReceived += NoticeReceived;

        WeakReferenceMessenger.Default.Register<NostrEventsChangedMessage>(this);
    }

    public override async Task InitializeAsync()
    {
        await _nostr.StartAsync(CancellationToken.None).ConfigureAwait(false);
    }

    public async Task LoadDataAsync()
    {
        //await _nostr.Subscribe("", Array.Empty<NostrSubscriptionFilter>());
        await _nostr.ToggleRelay(new Uri("wss://relay.damus.io"));
        Items = new ObservableCollection<Post>(await dataService.GetItems());
    }

    [RelayCommand]
    public async Task LoadMore()
    {
        var items = await dataService.GetItems();

        foreach (var item in items)
        {
            Items.Add(item);
        }
    }

    public override void OnDisappearing()
    {
        base.OnDisappearing();
    }

    public void Receive(NostrEventsChangedMessage message)
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    private async void GoToDetails(Post item)
    {
        await Shell.Current.GoToAsync(nameof(TimelineDetailPage), true, new Dictionary<string, object>
        {
            { "Item", item }
        });
    }

    private void NoticeReceived(object? sender, (string tuple, Uri known) e)
    {
        var message = $"{e.known}: {e.tuple}";
        var toast = Toast.Make(message, ToastDuration.Long, 30);
        toast.Show();
    }

    [RelayCommand]
    private async void OnRefreshing()
    {
        IsRefreshing = true;

        try
        {
            await LoadDataAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }
}