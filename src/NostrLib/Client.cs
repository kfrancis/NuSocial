using NostrLib.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace NostrLib
{
    public class Client : IDisposable
    {
        private readonly ClientWebSocketHandler _webSocketHandler;
        private readonly Channel<string> _pendingIncomingMessages = Channel.CreateUnbounded<string>();
        private readonly Channel<string> _pendingOutgoingMessages = Channel.CreateUnbounded<string>();
        private readonly Uri? _relay;
        private CancellationTokenSource _webSocketTokenSource = new();
        private ClientWebSocket? _clientWebSocket;
        private CancellationToken _disconnectToken;
        private bool _isDisposed;
        public EventHandler<string>? MessageReceived;
        public EventHandler<string>? NoticeReceived;
        public EventHandler<(string subscriptionId, NostrEvent[] events)>? EventsReceived;

        public Client(Uri relay)
        {
            _relay = relay;
            //https://damus.io/key/
            _disconnectToken = CancellationToken.None;
            ReconnectDelay = TimeSpan.FromSeconds(2);
            _webSocketHandler = new ClientWebSocketHandler(this);

            _ = ProcessChannel(_pendingIncomingMessages, HandleIncomingMessage, _webSocketTokenSource.Token);
            _ = ProcessChannel(_pendingOutgoingMessages, HandleOutgoingMessage, _webSocketTokenSource.Token);
        }

        private async Task<bool> HandleOutgoingMessage(string message, CancellationToken token)
        {
            try
            {
                return await WaitUntilConnected(token)
                    .ContinueWith(_ => _clientWebSocket?.SendMessageAsync(message, token), token)
                    .ContinueWith(_ => true, token);
            }
            catch
            {
                return false;
            }
        }

        private Task<bool> HandleIncomingMessage(string message, CancellationToken token)
        {
            var json = JsonDocument.Parse(message).RootElement;
            switch (json[0].GetString().ToLowerInvariant())
            {
                case "event":
                    var subscriptionId = json[1].GetString();
                    var evt = json[2].Deserialize<NostrEvent>();

                    if (evt?.Verify() is true)
                    {
                        EventsReceived?.Invoke(this, (subscriptionId, new[] { evt }));
                    }

                    break;
                case "notice":
                    var noticeMessage = json[1].GetString();
                    NoticeReceived?.Invoke(this, noticeMessage);
                    break;
                default:
                    break;
            }

            return Task.FromResult(true);
        }

        private async Task ProcessChannel<T>(Channel<T> channel, Func<T, CancellationToken, Task<bool>> processor,
            CancellationToken cancellationToken)
        {
            while (await channel.Reader.WaitToReadAsync(cancellationToken))
            {
                if (channel.Reader.TryPeek(out var evt))
                {
                    var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    linked.CancelAfter(5000);
                    if (await processor(evt, linked.Token))
                    {
                        channel.Reader.TryRead(out _);
                    }
                }
            }
        }

        /// <summary>
        /// The time to wait after a connection drops to try reconnecting.
        /// </summary>
        public TimeSpan ReconnectDelay { get; set; }

        ~Client()
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

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _clientWebSocket?.Dispose();
                }

                _clientWebSocket = null;

                _isDisposed = true;
            }
        }

        private async Task ConnectAndHandleConnection()
        {
            await PerformConnect(_disconnectToken);
            var linkedToken = CreateLinkedCancellationToken();
            await WaitUntilConnected(linkedToken);
            await _webSocketHandler.ProcessWebSocketRequestAsync(_clientWebSocket, linkedToken);
        }

        private async Task WaitUntilConnected(CancellationToken token)
        {
            while (_clientWebSocket.State != WebSocketState.Open && !token.IsCancellationRequested)
            {
                await Task.Delay(100, token);
            }
        }

        public Dictionary<string, NostrSubscriptionFilter[]> Subscriptions { get; set; } = new();

        public async Task Connect(CancellationToken disconnectToken = default)
        {
            _disconnectToken = disconnectToken;

            _webSocketTokenSource = CancellationTokenSource.CreateLinkedTokenSource(disconnectToken);

            while (!_webSocketTokenSource.IsCancellationRequested)
            {
                _ = ConnectAndHandleConnection().ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Debug.WriteLine(task.Exception.ToStringDemystified());
                    }
                    else if (task.IsCanceled)
                    {
                        //TryFailStart(null);
                    }
                }, TaskContinuationOptions.NotOnRanToCompletion);

                foreach (var subscription in Subscriptions)
                {
                    Debug.WriteLine($" relay {_relay} subscribing {subscription.Key}\n{JsonSerializer.Serialize(subscription.Value)}");

                    await CreateSubscription(subscription.Key, subscription.Value, disconnectToken);
                }

                _ = ListenForMessages();
                _clientWebSocket!.Abort();

                await Task.Delay(2000, disconnectToken);
            }
        }


        public virtual Task PerformConnect(CancellationToken token)
        {
            return PerformConnect(_relay, token);
        }

        private async Task PerformConnect(Uri url, CancellationToken token)
        {
            _clientWebSocket = new ClientWebSocket();

            await _clientWebSocket.ConnectAsync(url, token);
        }

        private async Task PerformConnect(string url, CancellationToken token)
        {
            var uri = UrlBuilder.ConvertToWebSocketUri(url);

            _clientWebSocket = new ClientWebSocket();

            await _clientWebSocket.ConnectAsync(uri, token);
        }

        public async Task ListenForMessages()
        {
            await foreach (var message in ListenForRawMessages())
            {
                Debug.WriteLine($"{message}");
                await _pendingIncomingMessages.Writer.WriteAsync(message);
                MessageReceived?.Invoke(this, message);
            }
        }

        public async IAsyncEnumerable<string> ListenForRawMessages()
        {
            var buffer = new ArraySegment<byte>(new byte[2048]);
            while (_clientWebSocket.State == WebSocketState.Open && !_webSocketTokenSource.IsCancellationRequested)
            {
                WebSocketReceiveResult result;
                await using var ms = new MemoryStream();
                do
                {
                    result = await _clientWebSocket!.ReceiveAsync(buffer, _webSocketTokenSource.Token);
                    ms.Write(buffer.Array!, buffer.Offset, result.Count);
                } while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                yield return Encoding.UTF8.GetString(ms.ToArray());

                if (result.MessageType == WebSocketMessageType.Close)
                    break;
            }

            _clientWebSocket.Abort();
        }

        private CancellationToken CreateLinkedCancellationToken()
        {
            // TODO: Revisit thread safety of this assignment
            _webSocketTokenSource = new CancellationTokenSource();
            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_webSocketTokenSource.Token, _disconnectToken);
            return linkedCts.Token;
        }

        public async Task CreateSubscription(string subscriptionId, NostrSubscriptionFilter[] filters, CancellationToken token = default)
        {
            var payload = JsonSerializer.Serialize(new object[] { "REQ", subscriptionId }.Concat(filters));

            await _pendingOutgoingMessages.Writer.WriteAsync(payload, token);
        }

        public async Task CloseSubscription(string subscriptionId, CancellationToken token = default)
        {
            var payload = JsonSerializer.Serialize(new[] { "CLOSE", subscriptionId });

            await _pendingOutgoingMessages.Writer.WriteAsync(payload, token);
        }
    }

    internal class UrlBuilder
    {
        internal static Uri ConvertToWebSocketUri(string url)
        {
            throw new NotImplementedException();
        }
    }
}