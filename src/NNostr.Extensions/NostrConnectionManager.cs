using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NBitcoin.Secp256k1;
using System.Text.Encodings.Web;
using NNostr.Client;
using NBitcoin.DataEncoders;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;

namespace NNostr.Extensions
{
    public class NostrConnectionManager : IDisposable
    {
        private readonly ConcurrentDictionary<Uri, NostrClient> _relayInstances = new();
        private readonly ObservableCollection<Uri> _relayList = new();
        private readonly CancellationTokenSource _cts = new();
        private bool _disposedValue;

        public ECPrivKey PrivateKey { get; }
        public string? PublicKey { get; set; }

        public NostrConnectionManager(string key, bool isPrivateKey = false)
            : this(key, isPrivateKey, Array.Empty<Uri>())
        {
        }

        public NostrConnectionManager(string key, bool isPrivateKey = false, Uri[]? relays = null)
        {
            if (isPrivateKey)
            {
                var hex = new HexEncoder();
                PrivateKey = Context.Instance.CreateECPrivKey(hex.DecodeData(key));
                PublicKey = PrivateKey.CreateXOnlyPubKey().ToBytes().ToHex();
            }
            else
            {
                PublicKey = key;
            }

            if (relays != null)
            {
                foreach (var relay in relays)
                {
                    _relayList.Add(relay);
                }
            }
        }

        public bool IsConnected { get; set; }

        public void Connect(Action<NostrConnectionManager>? cb = null, CancellationToken cancellationToken = default)
        {
            if (_relayList.Count < 1)
            {
                throw new InvalidOperationException("Please add any relay and try again.");
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
            try
            {
                cts.Token.ThrowIfCancellationRequested();
                foreach (var item in _relayList)
                {
                    using var relay = new NostrClient(item);
                    if (!_relayInstances.ContainsKey(item))
                    {
                        _ = Task.Run(async () => await relay.ConnectAndWaitUntilConnected(cts.Token), cts.Token);
                        _relayInstances.TryAdd(item, relay);
                        IsConnected = true;
                        cb?.Invoke(this);
                    }
                    //else
                    //{
                    //    // Already exists
                    //    if (_relayInstances.TryGetValue(item, out var existingRelay))
                    //    {
                    //        // re-init
                    //        if (!existingRelay.)
                    //        {
                    //            _ = Task.Run(async () => await existingRelay.ConnectAsync(ResultProcessor, cts.Token));
                    //        }
                    //        IsConnected = true;
                    //        cb?.Invoke(this);
                    //    }
                    //}
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_relayInstances?.Count > 0)
                    {
                        foreach (var relay in _relayInstances.Values)
                        {
                            relay?.Disconnect();
                            relay?.Dispose();
                        }
                    }
                    PrivateKey?.Dispose();
                    PublicKey = null;
                    _cts?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        ~NostrConnectionManager()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public async Task<NostrProfile> GetProfileAsync(string publicKey, CancellationToken cancellationToken = default)
        {
            var filter = new NostrSubscriptionFilter();
            filter.Kinds.Add((int)NostrKind.SetMetadata);
            filter.Authors = new Collection<string>() { publicKey };
            filter.Limit = 1;
            var filters = new List<NostrSubscriptionFilter>() { filter };
            var events = await GetEventsAsync(filters, cancellationToken);
            var profileInfo = new NostrProfile();
            var latest = events.GroupBy(e => e.Key).Select(x => x.First()).OrderByDescending(x => x.Value.CreatedAt).FirstOrDefault();
            if (latest.Value is INostrEvent<string> latestProfile &&
                !string.IsNullOrEmpty(latestProfile.Content))
            {
                using var doc = JsonDocument.Parse(latestProfile.Content.Replace("\\", "", StringComparison.OrdinalIgnoreCase));
                var json = doc.RootElement;
                if (json.TryGetProperty("name", out var nameJson))
                {
                    profileInfo.Name = nameJson.GetString();
                }
                if (json.TryGetProperty("about", out var aboutJson))
                {
                    profileInfo.About = aboutJson.GetString();
                }
                if (json.TryGetProperty("picture", out var pictureJson))
                {
                    profileInfo.Picture = pictureJson.GetString();
                }
                if (json.TryGetProperty("website", out var websiteJson))
                {
                    profileInfo.Website = websiteJson.GetString();
                }
                if (json.TryGetProperty("display_name", out var displayNameJson))
                {
                    profileInfo.DisplayName = displayNameJson.GetString();
                }
                if (json.TryGetProperty("nip05", out var nip05Json))
                {
                    profileInfo.Nip05 = nip05Json.GetString();
                }
            }

            var followingInfo = await GetFollowingInfoAsync(publicKey, cancellationToken);
            if (followingInfo is INostrEvent<string> followInfoStr &&
                !string.IsNullOrEmpty(followInfoStr.Content))
            {
                using var doc = JsonDocument.Parse(followInfoStr.Content.Replace("\\", "", StringComparison.OrdinalIgnoreCase));
                var json = doc.RootElement;
                foreach (var key in json.EnumerateObject())
                {
                    profileInfo.Relays.Add((key.Name, key.Value.GetProperty("read").GetBoolean(), key.Value.GetProperty("write").GetBoolean()));
                }
            }

            return profileInfo;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
