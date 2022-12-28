using NostrLib.Models;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Websocket.Client;
using Websocket.Client.Models;

namespace NostrLib
{
    public interface IListener
    {
        string SubscribeId { get; set; }
        Action<INostrEvent?, bool> HandleEvent { get; set; }
    }

    public class RelayListener : IListener
    {
        public string SubscribeId { get; set; }
        public Action<INostrEvent?, bool> HandleEvent { get; set; }
    }

    public interface INostrRelay
    {
        public Uri Url { get; set; }
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
    }

    public class Relay : INostrRelay, IDisposable
    {
        private readonly Client _client;
        private Uri? _url;
        private string? _name;
        private WebsocketClient? _webSocket;
        private int _connectionTimeoutMs = 15_000;
        private bool _connected = false;
        private bool _reconnect = false;
        private bool _manualClose = false;
        private ConcurrentDictionary<string, IListener> _listeners = new();
        private bool _isDisposed;
        private CancellationTokenSource _webSocketTokenSource = new();

        public Uri? Url { get => _url; set => _url = value; }
        public string? Name { get => _name; set => _name = value; }
        public bool CanRead { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool CanWrite { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Relay(Client client)
        {
            _client = client;
        }

        public async Task<bool> ConnectAsync(CancellationToken token = default)
        {
            _webSocketTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

            _webSocket = new WebsocketClient(_url, () => new ClientWebSocket())
            {
                MessageEncoding = Encoding.UTF8,
                IsReconnectionEnabled = false
            };

            _webSocket.MessageReceived.Subscribe(WsReceived);
            _webSocket.ReconnectionHappened.Subscribe(WsConnected);
            _webSocket.DisconnectionHappened.Subscribe(WsDisconnected);

            await _webSocket.Start();

            _connected = true;

            return _connected;
        }

        public event EventHandler<bool>? RelayConnectionChanged;

        public event EventHandler<string>? RelayNotice;

        public event EventHandler<RelayPost>? RelayPost;

        public event EventHandler<NostrPost>? PostReceived;

        private void WsDisconnected(DisconnectionInfo obj)
        {
            RelayConnectionChanged?.Invoke(this, false);
        }

        private void WsConnected(ReconnectionInfo obj)
        {
            RelayConnectionChanged?.Invoke(this, true);
        }

        private void WsReceived(ResponseMessage obj)
        {
            using var doc = JsonDocument.Parse(obj.Text);
            var json = doc.RootElement;
            _client.Log(json);
            var messageType = json[0].GetString()?.ToUpperInvariant() ?? string.Empty;
            switch (messageType)
            {
                case "EVENT":
                    var subId = json[1].GetString();
                    var ev = json[2].Deserialize<NostrEvent<string>>();
                    if (ev?.Verify() is true)
                    {
                        HandleEvent(subId, ev);
                    }
                    break;

                case "NOTICE":
                    var msg = json[1].GetString();
                    if (!string.IsNullOrEmpty(msg))
                    {
                        RelayNotice?.Invoke(this, msg);
                    }
                    break;

                case "EOSE":
                    // NIP-15 :: End of Stored Events Notice
                    // https://github.com/nostr-protocol/nips/blob/master/15.md
                    // used to notify clients all stored events have been sent
                    DeleteListener(json[1].GetString());
                    break;

                case "OK":
                    // NIP-20 :: Command Results
                    // https://github.com/nostr-protocol/nips/blob/master/20.md
                    RelayPost?.Invoke(this, new(json[1].GetString(), json[2].GetBoolean(), json[3].GetString()));
                    break;

                default:
                    // future
                    break;
            }
        }

        public async Task SendEvent(INostrEvent nostrEvent)
        {
            if (_webSocket != null && _webSocket.IsStarted)
            {
                var payload = JsonSerializer.Serialize(new object[] { "EVENT", nostrEvent });
                await _webSocket.SendInstant(payload);
            }
        }

        private void HandleEvent(string subscribeId, NostrEvent<string> rawEvent)
        {
            if (_listeners.TryGetValue(subscribeId, out var listener))
            {
                listener?.HandleEvent?.Invoke(rawEvent, false);
            }
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
                    PostReceived?.Invoke(this, new NostrPost(ev));
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

                case NostrKind.Reserved1:
                case NostrKind.Reserved2:
                case NostrKind.Reserved3:
                case NostrKind.Reserved4:
                case NostrKind.Reserved5:
                    // reserved_for_future_use :: nip-28
                    break;

                default:
                    // not (yet) supported event
                    break;
            }
        }

        public async Task<List<INostrEvent>> SubscribeAsync(string subscribeId, NostrSubscriptionFilter[] filters, CancellationToken cancellationToken = default)
        {
            if (_webSocket?.IsStarted == true)
            {
                var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var payload = JsonSerializer.Serialize(new object[] { "REQ", subscribeId }.Concat(filters));
                var events = new ConcurrentBag<INostrEvent>();

                void handleMethod(INostrEvent? nEvent, bool eose)
                {
                    if (nEvent != null)
                    {
                        events.Add(nEvent);
                    }
                    else if (eose)
                    {
                        linked.Cancel();
                    }
                }

                if (_listeners.ContainsKey(subscribeId) || _listeners.TryAdd(subscribeId, new RelayListener() { SubscribeId = subscribeId, HandleEvent = handleMethod }))
                {
                    linked.CancelAfter(5000);
                    _webSocket.Send(payload);

                    while (!linked.IsCancellationRequested)
                    {
                        await Task.Yield();
                    }

                    return events.ToList();
                }
                else
                {
                    throw new Exception("Couldn't add listener for some reason");
                }
            }
            else
            {
                throw new Exception("Websocket not ready");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _webSocket?.Dispose();
                }

                _isDisposed = true;
            }
        }

        ~Relay()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
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