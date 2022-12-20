using NostrLib.Models;
using System;
using System.Collections.Generic;
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
        public EventHandler<(string subscriptionId, NostrEvent[] events)>? EventsReceived;
        public EventHandler<string>? MessageReceived;
        public EventHandler<string>? NoticeReceived;
        private readonly Channel<string> _pendingIncomingMessages = Channel.CreateUnbounded<string>();
        private readonly Channel<string> _pendingOutgoingMessages = Channel.CreateUnbounded<string>();
        private readonly Uri? _relay;
        private CancellationTokenSource? _cancellationTokenSource = new();
        private ClientWebSocket? _clientWebSocket;
        private bool _isDisposed;
        private CancellationTokenSource _messageCancellationTokenSource = new();

        public Client(Uri relay)
        {
            _relay = relay;

            _ = ProcessChannel(_pendingIncomingMessages, HandleIncomingMessage, _messageCancellationTokenSource.Token);
            _ = ProcessChannel(_pendingOutgoingMessages, HandleOutgoingMessage, _messageCancellationTokenSource.Token);
        }

        ~Client()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public async Task CloseSubscription(string subscriptionId, CancellationToken token = default)
        {
            var payload = JsonSerializer.Serialize(new[] { "CLOSE", subscriptionId });

            await _pendingOutgoingMessages.Writer.WriteAsync(payload, token);
        }

        public async Task Connect(CancellationToken token = default)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                await ConnectAndWaitUntilConnected(_cancellationTokenSource.Token);
                _ = ListenForMessages();
                _clientWebSocket!.Abort();
            }
        }

        public async Task ConnectAndWaitUntilConnected(CancellationToken token = default)
        {
            if (_clientWebSocket?.State == WebSocketState.Open)
            {
                return;
            }

            _cancellationTokenSource ??= CancellationTokenSource.CreateLinkedTokenSource(token);

            _clientWebSocket?.Dispose();
            _clientWebSocket = new ClientWebSocket();
            var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            await _clientWebSocket.ConnectAsync(_relay, cts.Token);
            await WaitUntilConnected(cts.Token);
        }

        public async Task CreateSubscription(string subscriptionId, NostrSubscriptionFilter[] filters,
            CancellationToken token = default)
        {
            var payload = JsonSerializer.Serialize(new object[] { "REQ", subscriptionId }.Concat(filters));

            await _pendingOutgoingMessages.Writer.WriteAsync(payload, token);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async Task ListenForMessages()
        {
            await foreach (var message in ListenForRawMessages())
            {
                await _pendingIncomingMessages.Writer.WriteAsync(message);
                MessageReceived?.Invoke(this, message);
            }
        }

        public async IAsyncEnumerable<string> ListenForRawMessages()
        {
            var buffer = new ArraySegment<byte>(new byte[2048]);
            while (_clientWebSocket.State == WebSocketState.Open && !_cancellationTokenSource.IsCancellationRequested)
            {
                WebSocketReceiveResult result;
                await using var ms = new MemoryStream();
                do
                {
                    result = await _clientWebSocket!.ReceiveAsync(buffer, _cancellationTokenSource.Token);
                    ms.Write(buffer.Array!, buffer.Offset, result.Count);
                } while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                yield return Encoding.UTF8.GetString(ms.ToArray());

                if (result.MessageType == WebSocketMessageType.Close)
                    break;
            }

            _clientWebSocket.Abort();
        }

        public async Task PublishEvent(NostrEvent nostrEvent, CancellationToken token = default)
        {
            var payload = JsonSerializer.Serialize(new object[] { "EVENT", nostrEvent });
            await _pendingOutgoingMessages.Writer.WriteAsync(payload, token);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _clientWebSocket?.Dispose();
                    _messageCancellationTokenSource?.Cancel();
                    _messageCancellationTokenSource?.Dispose();
                }

                _clientWebSocket = null;

                _isDisposed = true;
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
            }

            return Task.FromResult(true);
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

        private async Task WaitUntilConnected(CancellationToken token)
        {
            while (_clientWebSocket.State != WebSocketState.Open && !token.IsCancellationRequested)
            {
                await Task.Delay(100, token);
            }
        }
    }
}