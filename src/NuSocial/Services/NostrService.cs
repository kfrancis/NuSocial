using NNostr.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuSocial.Services
{
    public class NostrService : IDisposable
    {
        private readonly NostrClient _client;
        private bool _isDisposed;

        public NostrService(NostrClient client)
        {
            _client = client;
        }

        public Task StartAsync(CancellationToken ct = default)
        {
            _client.NoticeReceived += NoticeReceived;
            _client.MessageReceived += MessageReceived;
            return _client.ConnectAndWaitUntilConnected(ct);
        }

        public Task StopAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            return _client.Disconnect();
        }

        private void MessageReceived(object? sender, string e)
        {
            throw new NotImplementedException();
        }

        private void NoticeReceived(object? sender, string e)
        {
            throw new NotImplementedException();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _client?.Dispose();
                }

                _isDisposed = true;
            }
        }

        ~NostrService()
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
}
