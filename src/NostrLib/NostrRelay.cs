using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin.Protocol;
using NostrLib.Converters;
using NostrLib.Models;
using Websocket.Client;
using Websocket.Client.Models;

namespace NostrLib
{
    public interface IListener
    {
        Action<INostrEvent?, bool> HandleEvent { get; set; }
        string SubscribeId { get; set; }
    }

    public interface INostrRelay
    {
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public Uri? Url { get; set; }
        public List<long> SupportedNips { get; }
    }

    public class NostrRelay : INostrRelay, IDisposable
    {
        private readonly NostrClient _client;
        private readonly TimeSpan _reconnectTimeout = TimeSpan.FromSeconds(30);
        private int _currentMessageId;
        private bool _isDisposed;
        private ConcurrentDictionary<string, IListener> _listeners = new();
        private IDisposable _pingSubscription;
        private WebsocketClient? _webSocket;
        private CancellationTokenSource _webSocketTokenSource = new();

        public NostrRelay(NostrClient client)
        {
            _client = client;
        }

        ~NostrRelay()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public event EventHandler<NostrPostReceivedEventArgs>? PostReceived;

        public event EventHandler<RelayConnectionChangedEventArgs>? RelayConnectionChanged;

        public event EventHandler<RelayNoticeEventArgs>? RelayNotice;

        public event EventHandler<RelayPostEventArgs>? RelayPost;

        public List<long> SupportedNips { get; } = new();

        private static readonly object GATE1 = new object();

        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public bool IsAlive => _webSocket?.IsRunning ?? false;
        public string? Name { get; set; }
        public Uri? Url { get; set; }

        public async Task<bool> ConnectAsync(CancellationToken token = default)
        {
            if (_webSocket != null)
            {
                await Close();
            }

            _webSocketTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

            // Perform NIP-11 check of supported features
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Accept", "application/nostr+json");

                var response = await client.GetAsync(Url, _webSocketTokenSource.Token);
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var rid = JsonSerializer.Deserialize<NostrRelayInfo>(responseBody);
                    if (rid != null)
                    {
                        Debug.WriteLine($"{Url} SupportedNips: {(string.Join(", ", rid.SupportedNips))}");
                        SupportedNips.AddRange(rid.SupportedNips);
                    }
                }
            }

            if (!SupportedNips.Any() || !SupportedNips.Contains(1))
            {
                Debug.WriteLine($"{Url} doesn't support enough NIPS to be useful. Stopping.");
                // If the relay doesn't at least support NIP-01, then we're going to stop here.
                return false;
            }

            _webSocket = new WebsocketClient(Url, () => new ClientWebSocket()
            {
                Options =
                {
                    KeepAliveInterval = TimeSpan.FromSeconds(5)
                }
            })
            {
                MessageEncoding = Encoding.UTF8,
                IsReconnectionEnabled = true,
                ReconnectTimeout = _reconnectTimeout,
            };

            _webSocket.MessageReceived.Where(msg => msg.MessageType == WebSocketMessageType.Text).Select(msg => Observable.FromAsync(async () => await WsReceived(msg))).Synchronize(GATE1).Concat().Subscribe();
            _webSocket.ReconnectionHappened.Subscribe(WsConnected);
            _webSocket.DisconnectionHappened.Subscribe(WsDisconnected);

            await _webSocket.Start();

            StartSendingPing(_webSocket);

            return IsAlive;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async Task SendEventAsync(INostrEvent nostrEvent)
        {
            var payload = JsonSerializer.Serialize(new object[] { NostrConsts.EVENT, nostrEvent });
            await SendMessageAsync(payload);
        }

        public async Task<List<INostrEvent>> SubscribeAsync(string subscribeId, NostrSubscriptionFilter[] filters, CancellationToken cancellationToken = default)
        {
            var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            if (!IsAlive)
            {
                await ConnectAsync(linked.Token);
            }

            var events = new ConcurrentBag<INostrEvent>();

            void HandleMethod(INostrEvent? nEvent, bool eose)
            {
                if (nEvent != null)
                {
                    events.Add(nEvent);
                    HandleEvent(subscribeId, nEvent);
                }
                else if (eose)
                {
                    // TODO: Hmm, this will only come through if NIP-15 is supported, otherwise we need another way to trigger.
                    linked.Cancel();
                }
            }

            if (!_listeners.ContainsKey(subscribeId) && _listeners.TryAdd(subscribeId, new RelayListener() { SubscribeId = subscribeId, HandleEvent = HandleMethod }))
            {
                var payload = JsonSerializer.Serialize(new object[] { NostrConsts.REQ, subscribeId }.Concat(filters));
                await SendMessageAsync(payload);

                while (!linked.IsCancellationRequested)
                {
                    await Task.Yield();
                }

                linked.Dispose();
                return events.ToList();
            }
            else
            {
                throw new InvalidOperationException("Couldn't add listener for some reason");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _pingSubscription?.Dispose();
                    _webSocket?.Dispose();
                    _webSocketTokenSource?.Dispose();
                }

                _webSocket = null;

                _isDisposed = true;
            }
        }

        private Task Close()
        {
            _webSocket?.Dispose();
            return Task.CompletedTask;
        }

        private void DeleteListener(string subscribeId)
        {
            if (_listeners.TryGetValue(subscribeId, out var listener))
            {
                listener?.HandleEvent?.Invoke(null, true);
            }
            if (!_listeners.TryRemove(subscribeId, out _))
            {
                _listeners = new ConcurrentDictionary<string, IListener>(_listeners.Where(x => x.Key != subscribeId));
            }
        }

        private void HandleEvent(string subscribeId, NostrEvent<string> rawEvent)
        {
            if (_listeners.TryGetValue(subscribeId, out var listener))
            {
                listener?.HandleEvent?.Invoke(rawEvent, false);
            }
        }

        private void HandleEvent(string? subId, INostrEvent ev)
        {
            switch (ev.Kind)
            {
                case NostrKind.SetMetadata:
                    // set_metadata :: nip-01
                    // https://github.com/nostr-protocol/nips/blob/master/01.md
                    break;

                case NostrKind.TextNote:
                    // text_note :: nip-01
                    PostReceived?.Invoke(this, new(new NostrPost(ev)));
                    break;

                case NostrKind.RecommendServer:
                    // recommend_server :: nip-01
                    break;

                case NostrKind.Contacts:
                    // contact list :: nip-02
                    // https://github.com/nostr-protocol/nips/blob/master/02.md
                    break;

                case NostrKind.EncryptedDM:
                    // encrypted direct message :: nip-04
                    // https://github.com/nostr-protocol/nips/blob/master/04.md
                    break;

                case NostrKind.Deletion:
                    // deletion :: nip-09
                    // https://github.com/nostr-protocol/nips/blob/master/09.md
                    break;

                case NostrKind.Reaction:
                    // reaction :: nip-25
                    // https://github.com/nostr-protocol/nips/blob/master/25.md
                    break;

                case NostrKind.ChannelCreate:
                    // channel create :: nip-28
                    // https://github.com/nostr-protocol/nips/blob/master/28.md
                    break;

                case NostrKind.ChannelMetadata:
                    // channel metadata :: nip-28
                    break;

                case NostrKind.ChannelMessage:
                    // channel message :: nip-28
                    break;

                case NostrKind.HideMessage:
                    // hide message :: nip-28
                    break;

                case NostrKind.MuteUser:
                    // mute user :: nip-28
                    break;

                //case NostrKind.Reserved1:
                //case NostrKind.Reserved2:
                //case NostrKind.Reserved3:
                //case NostrKind.Reserved4:
                //case NostrKind.Reserved5:
                //    // reserved_for_future_use :: nip-28
                //    break;

                default:
                    // not (yet) supported event
                    break;
            }
        }

        private async Task SendMessageAsync(string? message)
        {
            if (IsAlive && !string.IsNullOrEmpty(message))
            {
                System.Threading.Interlocked.Increment(ref _currentMessageId);
                Debug.WriteLine($"Sending {_currentMessageId}: {message}");
                await Task.Run(() => _webSocket!.Send(message));
            }
        }

        private void StartSendingPing(IWebsocketClient client)
        {
            _pingSubscription = Observable
                .Interval(TimeSpan.FromSeconds(31000))
                .Subscribe(_ => client.Send("ping"));
        }

        private void WsConnected(ReconnectionInfo obj)
        {
            RelayConnectionChanged?.Invoke(this, new(true));
        }

        private void WsDisconnected(DisconnectionInfo obj)
        {
            RelayConnectionChanged?.Invoke(this, new(false));
        }

        private Task WsReceived(ResponseMessage obj)
        {
            using var doc = JsonDocument.Parse(obj.Text);
            var json = doc.RootElement;
            NostrClient.Log(this, json);
            var messageType = json[0].GetString()?.ToUpperInvariant() ?? string.Empty;
            switch (messageType)
            {
                case NostrConsts.EVENT:
                    var subId = json[1].GetString() ?? string.Empty;
                    var ev = JsonSerializer.Deserialize<NostrEvent<string>>(json[2].ToString());
                    if (ev?.Verify() is true)
                    {
                        HandleEvent(subId, ev!);
                    }
                    break;

                case NostrConsts.NOTICE:
                    var msg = json[1].GetString();
                    if (!string.IsNullOrEmpty(msg))
                    {
                        RelayNotice?.Invoke(this, new RelayNoticeEventArgs(msg));
                    }
                    break;

                case NostrConsts.EOSE:
                    // NIP-15 :: End of Stored Events Notice
                    // https://github.com/nostr-protocol/nips/blob/master/15.md
                    // used to notify clients all stored events have been sent
                    DeleteListener(json[1].GetString() ?? string.Empty);
                    break;

                case NostrConsts.OK:
                    // NIP-20 :: Command Results
                    // https://github.com/nostr-protocol/nips/blob/master/20.md
                    RelayPost?.Invoke(this, new(json[1].GetString(), json[2].GetBoolean(), json[3].GetString()));
                    break;

                default:
                    // future
                    break;
            }

            return Task.CompletedTask;
        }
    }

    public class RelayListener : IListener
    {
        public Action<INostrEvent?, bool> HandleEvent { get; set; }
        public string SubscribeId { get; set; }
    }

    public class RelayPost
    {
        public RelayPost(string eventId, bool isSuccess, string message)
        {
            EventId = eventId;
            IsSuccess = isSuccess;
            Message = message;
        }

        public string EventId { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }
}
