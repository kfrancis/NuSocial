﻿using NostrLib.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using Websocket.Client;
using Websocket.Client.Models;

namespace NostrLib
{
    public class Client : IDisposable
    {
        private readonly Uri? _relay;
        private CancellationToken _disconnectToken;
        private ManualResetEvent _exitEvent;
        private bool _isDisposed;
        private int _msgSeq;
        private WebsocketClient _webSocket;
        private CancellationTokenSource _webSocketTokenSource = new();

        public Client(Uri relay)
        {
            _relay = relay;
            //https://damus.io/key/
            _disconnectToken = CancellationToken.None;
            ReconnectDelay = TimeSpan.FromSeconds(2);
        }

        ~Client()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        /// <summary>
        /// The time to wait after a connection drops to try reconnecting.
        /// </summary>
        public TimeSpan ReconnectDelay { get; set; }

        public void Connect(string subId, NostrSubscriptionFilter[] filters, CancellationToken token)
        {
            _webSocket = new WebsocketClient(_relay, () =>
            {
                return new ClientWebSocket();
            })
            {
                MessageEncoding = Encoding.UTF8,
                IsReconnectionEnabled = false
            };

            _exitEvent = new ManualResetEvent(false);

            _webSocket.MessageReceived.Subscribe(WsReceived);
            _webSocket.ReconnectionHappened.Subscribe(WsConnected);
            _webSocket.DisconnectionHappened.Subscribe(WsDisconnected);

            _webSocket.Start();

            if (!string.IsNullOrEmpty(subId))
            {
                var payload = JsonSerializer.Serialize(new object[] { "REQ", subId }.Concat(filters));
                _webSocket.Send(payload);
            }

            _exitEvent.WaitOne();
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
                    _webSocket?.Dispose();
                }

                _webSocket = null;

                _isDisposed = true;
            }
        }

        protected virtual void HandleEvent(INostrEvent ev)
        {
            switch (ev.Kind)
            {
                case 0:
                    // set_metadata :: nip-01
                    // https://github.com/nostr-protocol/nips/blob/master/01.md
                    break;

                case 1:
                    // text_note :: nip-01
                    break;

                case 2:
                    // recommend_server :: nip-01
                    break;

                case 3:
                    // contact list :: nip-02
                    // https://github.com/nostr-protocol/nips/blob/master/02.md
                    break;

                case 4:
                    // encrypted direct message :: nip-04
                    // https://github.com/nostr-protocol/nips/blob/master/04.md
                    break;

                case 5:
                    // deletion :: nip-09
                    // https://github.com/nostr-protocol/nips/blob/master/09.md
                    break;

                case 7:
                    // reaction :: nip-25
                    // https://github.com/nostr-protocol/nips/blob/master/25.md
                    break;

                case 40:
                    // channel create :: nip-28
                    // https://github.com/nostr-protocol/nips/blob/master/28.md
                    break;

                case 41:
                    // channel metadata :: nip-28
                    break;

                case 42:
                    // channel message :: nip-28
                    break;

                case 43:
                    // hide message :: nip-28
                    break;

                case 44:
                    // mute user :: nip-28
                    break;

                case 45:
                case 46:
                case 47:
                case 48:
                case 49:
                    // reserved_for_future_use :: nip-28
                    break;

                default:
                    // not (yet) supported event
                    break;
            }
        }

        protected void WsSend<T>(int evKind, T body)
                                    where T : class
        {
            _msgSeq++;

            var clientMessage = new NostrEvent<T>
            {
                Kind = evKind,
                Content = body
            };

            var msg = JsonSerializer.Serialize(clientMessage);
            _webSocket.Send(msg);
        }

        private void WsConnected(ReconnectionInfo obj)
        {
            Debug.WriteLine("Connected.");
        }

        private void WsDisconnected(DisconnectionInfo obj)
        {
            Debug.WriteLine("Disconnected.");
            _exitEvent?.Set();
        }

        private void WsReceived(ResponseMessage obj)
        {
            var ev = JsonSerializer.Deserialize<NostrEvent<string>>(obj.Text);
            if (ev != null)
            {
                HandleEvent(ev);
            }
        }
    }

    internal static class EnumeratorExtensions
    {
        public static IEnumerable<T> ToEnumerable<T>(this IEnumerator<T> enumerator)
        {
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }
    }
}