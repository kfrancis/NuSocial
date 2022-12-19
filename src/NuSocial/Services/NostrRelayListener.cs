using NNostr.Client;

namespace NuSocial.Services
{
    public partial class NostrRelayListener : ObservableObject, IDisposable
    {
        public EventHandler<(string subscriptionId, NostrEvent[] events)> EventsReceived;
        public EventHandler<string> NoticeReceived;
        public EventHandler StatusChanged;

        private readonly Uri _uri;
        private CancellationToken _ct;
        private bool _isDisposed;
        private NostrClient _nostrClient;

        [ObservableProperty]
        private RelayStatus _status = RelayStatus.Disconnected;

        public NostrRelayListener(Uri uri)
        {
            _uri = uri;
        }

        ~NostrRelayListener()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public Dictionary<string, NostrSubscriptionFilter[]> Subscriptions { get; set; } = new();

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async Task SendEvent(NostrEvent evt)
        {
            Debug.WriteLine($"Sending evt to relay {_uri} : {JsonSerializer.Serialize(evt)} ");
            await _nostrClient.PublishEvent(evt, _ct);
        }

        public async Task StartAsync(CancellationToken ct = default)
        {
            _ct = ct;

            _nostrClient = new NostrClient(_uri);
            _nostrClient.NoticeReceived += NoticeReceived;
            _nostrClient.EventsReceived += EventsReceived;
            _nostrClient.MessageReceived += (sender, s) => { Debug.WriteLineIf(!string.IsNullOrEmpty(s), $"Relay {_uri} sent message: {s}"); };

            while (!ct.IsCancellationRequested)
            {
                Status = RelayStatus.Connecting;
                await _nostrClient.ConnectAndWaitUntilConnected(ct);
                Status = RelayStatus.Connected;
                foreach (var subscription in Subscriptions)
                {
                    Debug.WriteLine($" relay {_uri} subscribing {subscription.Key}");
                    await _nostrClient.CreateSubscription(subscription.Key, subscription.Value, ct);
                }

                await _nostrClient.ListenForMessages();
                Status = RelayStatus.Disconnected;
                await Task.Delay(2000, ct);
            }
        }

        public async Task StopAsync(CancellationToken token)
        {
            await _nostrClient.Disconnect();
        }

        public async Task Subscribe(string directDm, NostrSubscriptionFilter[] getMessageThreadFilters)
        {
            await Unsubscribe(directDm);
            if (Subscriptions.TryAdd(directDm, getMessageThreadFilters) && Status == RelayStatus.Connected)
            {
                try
                {
                    await _nostrClient.CreateSubscription(directDm, getMessageThreadFilters, _ct);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("OnSubscribe: " + e.ToStringDemystified());
                }
            }
        }

        public async Task Unsubscribe(string directDm)
        {
            if (Subscriptions.Remove(directDm))
            {
                try
                {
                    await _nostrClient.CloseSubscription(directDm, _ct);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("OnUnsubscribe: " + e.ToStringDemystified());
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {                     
                    _nostrClient?.Dispose();
                }

                _isDisposed = true;
            }
        }
    }
}