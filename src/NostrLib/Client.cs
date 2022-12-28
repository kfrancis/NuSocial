using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NostrLib.Models;

namespace NostrLib
{
    public class Client : IDisposable, INostrClient
    {
        private bool _isDisposed;
        private ConcurrentDictionary<Uri, Relay> _relayInstances = new();
        private ObservableCollection<RelayItem> _relayList = new();

        public Client(string privateKey)
            : this(privateKey, Array.Empty<RelayItem>())
        {
        }

        public Client(string privateKey, RelayItem[] relays)
        {
            PrivateKey = privateKey;
            PublicKey = privateKey;

            ReconnectDelay = TimeSpan.FromSeconds(2);

            foreach (var relay in relays)
            {
                _relayList.Add(relay);
            }
        }

        ~Client()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public string? PrivateKey { get; set; }
        public string? PublicKey { get; set; }

        /// <summary>
        /// The time to wait after a connection drops to try reconnecting.
        /// </summary>
        public TimeSpan ReconnectDelay { get; set; }
        public Action<object, NostrPost> PostReceived { get; set; }

        public async Task ConnectAsync(Action<Client>? cb = null, CancellationToken token = default)
        {
            if (_relayList.Count < 1)
            {
                throw new Exception("Please add any relay and try again.");
            }
            for (var i = 0; i < _relayList.Count; i++)
            {
                var item = _relayList[i];
                var relay = new Relay(this)
                {
                    Url = item.Uri,
                    Name = item.Name
                };
                if (!_relayInstances.ContainsKey(item.Uri))
                {
                    relay.RelayPost += Relay_RelayPost;
                    relay.RelayConnectionChanged += Relay_RelayConnectionChanged;
                    relay.RelayNotice += Relay_RelayNotice;
                    if (await relay.ConnectAsync(token))
                    {
                        _relayInstances.TryAdd(relay.Url, relay);
                        cb?.Invoke(this);
                    }
                    else
                    {
                        var t = "";
                        // something went wrong
                    }
                }
                else
                {
                    // Already exists
                    if (_relayInstances.TryGetValue(item.Uri, out var existingRelay))
                    {
                        // re-init
                        relay.RelayPost -= Relay_RelayPost;
                        relay.RelayPost += Relay_RelayPost;
                        relay.RelayConnectionChanged -= Relay_RelayConnectionChanged;
                        relay.RelayConnectionChanged += Relay_RelayConnectionChanged;
                        relay.RelayNotice -= Relay_RelayNotice;
                        relay.RelayNotice += Relay_RelayNotice;
                        if (await existingRelay.ConnectAsync(token))
                        {
                            cb?.Invoke(this);
                        }
                    }
                }
            }
        }

        public Task DisconnectAsync()
        {
            if (_relayInstances?.Count > 0)
            {
                foreach (var relay in _relayInstances.Values)
                {
                    if (relay != null)
                    {
                        relay.RelayPost -= Relay_RelayPost;
                        relay.RelayConnectionChanged -= Relay_RelayConnectionChanged;
                        relay.RelayNotice -= Relay_RelayNotice;
                    }
                    relay?.Dispose();
                }
                _relayInstances.Clear();
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async Task<INostrEvent?> GetFollowingInfoAsync(string publicKey)
        {
            var filters = new List<NostrSubscriptionFilter>() {
                new NostrSubscriptionFilter()
                {
                    Kinds = new[] { NostrKind.Contacts },
                    Authors = new[]{ publicKey }
                }
            };
            var createdAt = DateTimeOffset.MinValue;
            var events = await GetEventsAsync(filters);
            INostrEvent? retVal = null;
            foreach (var evt in events.Values)
            {
                if (evt.CreatedAt > createdAt)
                {
                    createdAt = evt.CreatedAt.Value;
                    retVal = evt;
                }
            }
            return retVal;
        }

        public async Task<IEnumerable<NostrPost>> GetGlobalPostsAsync(int? limit = null, DateTime? since = null, List<string>? authors = null)
        {
            var globalFilter = new NostrSubscriptionFilter()
            {
                Kinds = new[] { NostrKind.TextNote }
            };
            if (limit > 0) globalFilter.Limit = limit;
            if (since != null)
            {
                globalFilter.Since = since;
            }
            if (authors?.Count > 0) globalFilter.Authors = authors.ToArray();

            var filters = new List<NostrSubscriptionFilter>()
            {
                globalFilter
            };

            var events = await GetEventsAsync(filters).ConfigureAwait(true);
            var posts = new List<NostrPost>();
            foreach (var nEvent in events)
            {
                posts.Add(EventToPost(nEvent.Value));
            }
            return posts.AsEnumerable();
        }

        public async Task<IEnumerable<string>> GetFollowerInfoAsync(string publicKey)
        {
            var filters = new List<NostrSubscriptionFilter>() {
                new NostrSubscriptionFilter()
                {
                    Kinds = new[] { NostrKind.Contacts },
                    PublicKey = new[]{ publicKey }
                }
            };
            var events = await GetEventsAsync(filters);
            return events.Values.ToList().ConvertAll(x => x.PublicKey);
        }

        public async Task<IEnumerable<NostrPost>> GetPostsAsync()
        {
            var filters = new List<NostrSubscriptionFilter>() {
                new NostrSubscriptionFilter(){
                    Kinds = new[] { NostrKind.TextNote },
                    Authors = new string[] { PublicKey }
                }
            };
            var events = await GetEventsAsync(filters);
            var posts = new List<NostrPost>();
            foreach (var nEvent in events)
            {
                posts.Add(EventToPost(nEvent.Value));
            }
            return posts.AsEnumerable();
        }

        public async Task GetProfileAsync(string publicKey)
        {
            var filters = new List<NostrSubscriptionFilter>() {
                new NostrSubscriptionFilter(){
                    Kinds = new[] { NostrKind.SetMetadata },
                    Authors = new[] { PublicKey },
                    Limit = 1
                }
            };
            var events = await GetEventsAsync(filters);
            var profileInfo = new NostrProfile();
            var latest = events.OrderByDescending(x => x.Value.CreatedAt).FirstOrDefault();
            if (latest.Value is INostrEvent<string> latestProfile &&
                !string.IsNullOrEmpty(latestProfile.Content))
            {
                using var doc = JsonDocument.Parse(latestProfile.Content);
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
                    profileInfo.Picture = aboutJson.GetString();
                }
            }
            //var followingInfo = await GetFollowingInfoAsync(publicKey);
            //if (followingInfo is INostrEvent<string> followInfoStr &&
            //    !string.IsNullOrEmpty(followInfoStr.Content))
            //{
            //    using var doc = JsonDocument.Parse(followInfoStr.Content);
            //    var json = doc.RootElement;
            //    foreach (var key in json.EnumerateObject())
            //    {
            //        profileInfo.Relays.Add(new { "", true, true });
            //    }
            //}
        }

        internal void Log(JsonElement json)
        {
            Debug.WriteLineIf(!string.IsNullOrEmpty(json.ToString()), json.ToString());
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (_relayInstances?.Count > 0)
                    {
                        foreach (var relay in _relayInstances.Values)
                        {
                            if (relay != null)
                            {
                                relay.RelayPost -= Relay_RelayPost;
                                relay.RelayConnectionChanged -= Relay_RelayConnectionChanged;
                                relay.RelayNotice -= Relay_RelayNotice;
                            }
                            relay?.Dispose();
                        }
                    }
                }

                _isDisposed = true;
            }
        }

        public async Task SetRelaysAsync(RelayItem[] relayItems)
        {
            await DisconnectAsync();

            _relayList.Clear();
            for (var i = 0; i < relayItems.Length - 1; i++)
            {
                _relayList.Add(relayItems[i]);
            }

            await ConnectAsync();
        }

        private NostrPost EventToPost(INostrEvent evt) => new NostrPost(evt);

        private async Task<ConcurrentDictionary<string, INostrEvent>> GetEventsAsync(List<NostrSubscriptionFilter> filters)
        {
            var events = new ConcurrentDictionary<string, INostrEvent>();
            var subEvents = _relayInstances.Values.Select(r => r.SubscribeAsync(PublicKey, filters.ToArray()));
            var results = await subEvents.WhenAll(TimeSpan.FromSeconds(5));
            foreach (var relayEvents in results)
            {
                foreach (var relayEvent in relayEvents)
                {
                    events.TryAdd(relayEvent.Id, relayEvent);
                }
            }
            return events;
        }

        private void Relay_RelayConnectionChanged(object sender, bool isConnected)
        {
            if (sender is Relay relay)
            {
                Debug.WriteLine($"Connection for '{relay.Url}' changed: {(isConnected ? "connected" : "disconnected")}");
            }
        }

        private void Relay_RelayNotice(object sender, string e)
        {
            if (sender is Relay relay)
            {
                Debug.WriteLineIf(!string.IsNullOrEmpty(e), $"RelayNotice from '{relay.Url}': {e}");
            }
        }

        private void Relay_RelayPost(object sender, RelayPost e)
        {
            if (sender is Relay relay)
            {
                //Debug.WriteLine($"Connection for '{relay.Url}' changed: {(isConnected ? "connected" : "disconnected")}");
            }
        }

        //protected void WsSend<T>(NostrKind evKind, T body)
        //                            where T : class
        //{
        //    if (_webSocket == null) return;

        //    _msgSeq++;

        //    var clientMessage = new NostrEvent<T>
        //    {
        //        Kind = evKind,
        //        Content = body
        //    };

        //    var msg = JsonSerializer.Serialize(clientMessage);
        //    _webSocket.Send(msg);
        //}
    }

    public class RelayItem
    {
        public string Name { get; set; }
        public Uri Uri { get; set; }
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