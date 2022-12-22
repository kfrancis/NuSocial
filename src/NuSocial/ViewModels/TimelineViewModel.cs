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

namespace NuSocial.ViewModels;

public partial class TimelineViewModel : BaseViewModel, IRecipient<NostrEventsChangedMessage>
{
    private readonly Client _nostr;
    private ConcurrentStack<NostrPost> _postStack = new ConcurrentStack<NostrPost>();
    private RateLimiter _limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions()
    {
        PermitLimit = 2,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        QueueLimit = 1,
        Window = TimeSpan.FromMilliseconds(50),
        AutoReplenishment = true
    });

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private string _key = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UnreadLabel))]
    private int _unreadPostCount;

    partial void OnKeyChanged(string value)
    {
        Task.Run(async () =>
        {
            await LoadDataAsync();
        });
    }

    public string UnreadLabel => $"Load {(UnreadPostCount > 1000 ? "many" : UnreadPostCount)} unread";

    [ObservableProperty]
    private ObservableCollection<Post> _items = new();

    public TimelineViewModel()
        : this(Ioc.Default.GetRequiredService<IDialogService>(), Ioc.Default.GetRequiredService<ICustomDispatcher>())
    {
    }

    public TimelineViewModel(IDialogService dialogService, ICustomDispatcher customDispatcher) : base(dialogService, customDispatcher)
    {
        _nostr = new Client(new Uri("wss://relay.damus.io"));

        WeakReferenceMessenger.Default.Register<NostrEventsChangedMessage>(this);
    }

    [RelayCommand()]
    public async Task LoadUnreadPostsAsync()
    {
        await SetBusyAsync(async () =>
        {
            await Dispatcher.RunAsync(() =>
            {
                Items.Clear();
                UnreadPostCount = 0;
                int popCount = 100;
                if (_postStack.Count < 100)
                {
                    popCount = _postStack.Count;
                }
                var resultBuffer = new NostrPost[popCount];
                if (_postStack.TryPopRange(resultBuffer) > 0)
                {
                    var resultList = resultBuffer.ToImmutableList().ConvertAll(x => new Post() { Content = (x.RawEvent as INostrEvent<string>)?.Content ?? string.Empty });
                    foreach (var item in resultList)
                    {
                        Items.Add(item);
                    }
                }
            });
        });
    }

    public override async Task InitializeAsync()
    {
        ArgumentNullException.ThrowIfNull(_nostr);

        await LoadDataAsync();
    }

    public override void OnDisappearing()
    {
        base.OnDisappearing();
    }

    public void Receive(NostrEventsChangedMessage message)
    {
        throw new NotImplementedException();
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
    }

    public async Task LoadDataAsync()
    {
        if (_nostr == null) return;
        if (string.IsNullOrEmpty(Key)) return;

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
        using RateLimitLease lease = _limiter.AttemptAcquire(permitCount: 1);
        if (lease.IsAcquired)
        {
            // Do action that is protected by limiter
            if (e.RawEvent is INostrEvent<string> contentEvent)
            {
                if (!string.IsNullOrEmpty(contentEvent.Content))
                {
                    Dispatcher.Run(() =>
                    {
                        UnreadPostCount++;
                        _postStack.Push(e);
                    });
                }
            }
        }
    }

    private void NoticeReceived(object? sender, (string tuple, Uri known) e)
    {
        var message = $"{e.known}: {e.tuple}";
        var toast = Toast.Make(message, ToastDuration.Long, 30);
        toast.Show();
    }
}