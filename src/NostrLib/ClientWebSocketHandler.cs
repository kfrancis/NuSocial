using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace NostrLib
{
    internal class ClientWebSocketHandler
    {
        private readonly Client _client;

        public ClientWebSocketHandler(Client client)
        {
            _client = client;
        }

        internal Task ProcessWebSocketRequestAsync(ClientWebSocket? clientWebSocket, CancellationToken linkedToken)
        {
            return Task.CompletedTask;
        }
    }
}