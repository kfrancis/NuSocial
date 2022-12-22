using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Messaging;
using NostrLib;
using NostrLib.Models;
using NuSocial.Core.Threading;
using NuSocial.Messages;

namespace NuSocial.ViewModels;

public partial class TimelineViewModel : BaseViewModel, IRecipient<NostrEventsChangedMessage>
{
    private readonly Client _nostr;
    private readonly SampleDataService dataService;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private string _key;

    [ObservableProperty]
    private ObservableCollection<Post> _items = new();

    public TimelineViewModel(SampleDataService service)
    {
        dataService = service;
        _nostr = new Client(new Uri("wss://relay.damus.io"));
    }

    public TimelineViewModel(SampleDataService service, IDialogService dialogService, ICustomDispatcher customDispatcher) : base(dialogService, customDispatcher)
    {
        dataService = service;
        _nostr = new Client(new Uri("wss://relay.damus.io"));
        WeakReferenceMessenger.Default.Register<NostrEventsChangedMessage>(this);
    }

    public override async Task InitializeAsync()
    {
        ArgumentNullException.ThrowIfNull(_nostr);

        await Task.Run(() =>
        {
            LoadDataAsync();
        });
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

    public async Task LoadDataAsync()
    {
        if (_nostr == null) return;

        _nostr.PostReceived -= Nostr_PostReceived;
        _nostr.PostReceived += Nostr_PostReceived;

        var filters = new List<NostrSubscriptionFilter>() {
                new NostrSubscriptionFilter(){
                    Kinds = new[] { 1, 7 }
                }
            };

        var callback = (Client sender) =>
        {
            sender.PostReceived -= (s, post) => { };
        };

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(1));
        await _nostr.ConnectAsync("id", filters.ToArray(), callback, cts.Token);
    }

    [RelayCommand]
    private async void GoToDetails(Post item)
    {
        await Shell.Current.GoToAsync(nameof(TimelineDetailPage), true, new Dictionary<string, object>
        {
            { "Item", item }
        });
    }

    private void Nostr_PostReceived(object? sender, NostrPost e)
    {
        if (IsRefreshing) return;
        if (e.RawEvent is INostrEvent<string> contentEvent)
        {
            if (!string.IsNullOrEmpty(contentEvent.Content) && Items.Count < 10)
            {
                Dispatcher.Run(() =>
                {
                    Items.Insert(0, new Post() { Content = contentEvent.Content });
                });
            }
        }
    }

    private void NoticeReceived(object? sender, (string tuple, Uri known) e)
    {
        var message = $"{e.known}: {e.tuple}";
        var toast = Toast.Make(message, ToastDuration.Long, 30);
        toast.Show();
    }

    [RelayCommand]
    private void ToggleFeed()
    {
        IsRefreshing = !IsRefreshing;
    }

    [RelayCommand]
    private async void OnRefreshing()
    {
        IsRefreshing = true;

        try
        {
            //await LoadDataAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }
}