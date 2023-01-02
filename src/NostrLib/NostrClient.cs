using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NostrLib.Converters;
using NostrLib.Models;

namespace NostrLib
{
    public class NostrClient : IDisposable, INostrClient
    {
        private readonly ConcurrentDictionary<Uri, NostrRelay> _relayInstances = new();
        private readonly ObservableCollection<RelayItem> _relayList = new();
        private bool _isDisposed;

        public NostrClient(string privateKey)
            : this(privateKey, Array.Empty<RelayItem>())
        {
        }

        public NostrClient(string privateKey, RelayItem[] relays)
        {
            if (relays is null)
            {
                throw new ArgumentNullException(nameof(relays));
            }

            PrivateKey = privateKey;
            PublicKey = privateKey;

            ReconnectDelay = TimeSpan.FromSeconds(2);

            foreach (var relay in relays)
            {
                _relayList.Add(relay);
            }
        }

        ~NostrClient()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        //public Action<object, NostrPost> PostReceived { get; set; }

        public string? PrivateKey { get; set; }

        public string? PublicKey { get; set; }

        /// <summary>
        /// The time to wait after a connection drops to try reconnecting.
        /// </summary>
        public TimeSpan ReconnectDelay { get; set; }
        public bool IsConnected { get; set; }

        public async Task ConnectAsync(Action<NostrClient>? cb = null, CancellationToken cancellationToken = default)
        {
            if (_relayList.Count < 1)
            {
                throw new InvalidOperationException("Please add any relay and try again.");
            }
            for (var i = 0; i < _relayList.Count; i++)
            {
                var item = _relayList[i];
                using var relay = new NostrRelay(this)
                {
                    Url = item.Uri,
                    Name = item.Name
                };
                if (!_relayInstances.ContainsKey(item.Uri))
                {
                    relay.RelayPost += Relay_RelayPost;
                    relay.RelayConnectionChanged += Relay_RelayConnectionChanged;
                    relay.RelayNotice += Relay_RelayNotice;
                    if (await relay.ConnectAsync(cancellationToken))
                    {
                        _relayInstances.TryAdd(relay.Url, relay);
                        IsConnected = true;
                        cb?.Invoke(this);
                    }
                    else
                    {
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
                        if (await existingRelay.ConnectAsync(cancellationToken))
                        {
                            IsConnected = true;
                            cb?.Invoke(this);
                        }
                    }
                }
            }
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
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

            IsConnected = false;

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async Task<IEnumerable<string>> GetFollowerInfoAsync(string publicKey, CancellationToken cancellationToken = default)
        {
            var filter = new NostrSubscriptionFilter();
            filter.Kinds.Add((int)NostrKind.Contacts);
            filter.Authors.Add(publicKey);
            var filters = new List<NostrSubscriptionFilter>() { filter };
            var events = await GetEventsAsync(filters, cancellationToken);
            return events.Values.ToList().ConvertAll(x => x.PublicKey);
        }

        public async Task<INostrEvent?> GetFollowingInfoAsync(string publicKey, CancellationToken cancellationToken = default)
        {
            var filter = new NostrSubscriptionFilter();
            filter.Kinds.Add((int)NostrKind.Contacts);
            filter.Authors = new Collection<string>() { publicKey };
            var filters = new List<NostrSubscriptionFilter>() { filter };
            var createdAt = DateTimeOffset.MinValue;
            var events = await GetEventsAsync(filters, cancellationToken);
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

        public async Task<IEnumerable<NostrPost>> GetGlobalPostsAsync(int? limit = null, DateTime? since = null, Collection<string>? authors = null, CancellationToken cancellationToken = default)
        {
            await EnsureConnectedAsync(cancellationToken);

            var globalFilter = new NostrSubscriptionFilter();
            globalFilter.Kinds.Add((int)NostrKind.TextNote);

            if (limit > 0) globalFilter.Limit = limit;
            if (since != null)
            {
                globalFilter.Since = since;
            }
            if (authors != null)
            {
                foreach (var author in authors)
                {
                    globalFilter.Authors.Add(author);
                }
            }

            var filters = new List<NostrSubscriptionFilter>()
            {
                globalFilter
            };

            var events = await GetEventsAsync(filters, cancellationToken);
            var posts = new List<NostrPost>();
            foreach (var nEvent in events.OrderByDescending(x => x.Value.CreatedAt))
            {
                posts.Add(EventToPost(nEvent.Value));
            }
            return posts.Take(limit ?? int.MaxValue).AsEnumerable();
        }

        /// <summary>
        /// Fetch posts for the current key
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<IEnumerable<NostrPost>> GetPostsAsync(CancellationToken cancellationToken = default)
        {
            await EnsureConnectedAsync(cancellationToken);

            var posts = new List<NostrPost>();
            if (!string.IsNullOrEmpty(PublicKey))
            {
                var filter = new NostrSubscriptionFilter();
                filter.Kinds.Add((int)NostrKind.TextNote);
                filter.Authors = new Collection<string>() { PublicKey };
                var filters = new List<NostrSubscriptionFilter>() { filter };
                var events = await GetEventsAsync(filters, cancellationToken);

                // Hmm, we should have a list of events from all relays when we get here.
                foreach (var nEvent in events.OrderByDescending(x => x.Value.CreatedAt))
                {
                    posts.Add(EventToPost(nEvent.Value));
                }
            }
            else
            {
                throw new InvalidOperationException("Need a key");
            }

            return posts.AsEnumerable();
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
            var latest = events.OrderByDescending(x => x.Value.CreatedAt).FirstOrDefault();
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

        public async Task SetRelaysAsync(RelayItem[] relayItems, bool shouldConnect = false, CancellationToken cancellationToken = default)
        {
            if (relayItems?.Length > 0)
            {
                await DisconnectAsync(cancellationToken);

                _relayList.Clear();
                for (var i = 0; i < relayItems.Length; i++)
                {
                    _relayList.Add(relayItems[i]);
                }

                if (shouldConnect)
                    await ConnectAsync(cancellationToken: cancellationToken);
            }
        }

        public void UpdateKey(string key)
        {
            PrivateKey = key;
            PublicKey = key;
        }

        [Conditional("DEBUG")]
        internal static void Log(NostrRelay sender, JsonElement json)
        {
            Debug.WriteLineIf(!string.IsNullOrEmpty(json.ToString()), $"From {sender.Url}: {json}");
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

        private static NostrPost EventToPost(INostrEvent evt) => new(evt);

        private async Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConnected && _relayList.Count > 0)
            {
                await DisconnectAsync(cancellationToken);
                await ConnectAsync(cancellationToken: cancellationToken);
            }
        }

        private async Task<ConcurrentDictionary<string, INostrEvent>> GetEventsAsync(List<NostrSubscriptionFilter> filters, CancellationToken cancellationToken = default)
        {
            var events = new ConcurrentDictionary<string, INostrEvent>();

            if (!string.IsNullOrEmpty(PublicKey))
            {
                var subEvents = _relayInstances.Values.Select(r => r.SubscribeAsync(PublicKey, filters.ToArray(), cancellationToken));
                var results = await subEvents.WhenAll(TimeSpan.FromSeconds(10));

                foreach (var relayEvents in results)
                {
                    foreach (var relayEvent in relayEvents)
                    {
                        events.TryAdd(relayEvent.Id, relayEvent);
                    }
                }
            }

            return events;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "<Pending>")]
        public static string GenerateKey()
        {
            // Generate a new random mnemonic phrase
            var mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
            var mnemonicString = mnemonic.ToString();

            // Convert the mnemonic to a seed
            var seed = mnemonic.DeriveSeed();

            return BitConverter.ToString(seed).Replace("-", string.Empty, StringComparison.InvariantCultureIgnoreCase).ToLowerInvariant();
        }

        private void Relay_RelayConnectionChanged(object sender, RelayConnectionChangedEventArgs args)
        {
            if (sender is NostrRelay relay)
            {
                Debug.WriteLine($"Connection for '{relay.Url}' changed: {(args.IsConnected ? "connected" : "disconnected")}");
            }
        }

        private void Relay_RelayNotice(object sender, RelayNoticeEventArgs args)
        {
            if (sender is NostrRelay relay)
            {
                Debug.WriteLineIf(!string.IsNullOrEmpty(args.Message), $"RelayNotice from '{relay.Url}': {args.Message}");
            }
        }

        private void Relay_RelayPost(object sender, RelayPostEventArgs args)
        {
            if (sender is NostrRelay relay)
            {
                Debug.WriteLine($"Relay message for '{relay.Url}': {(args.WasSuccessful ? "Success" : "Fail")} :: {args.Message}");
            }
        }

        public Task<INostrEvent> SendTextPostAsync(string message)
        {
            return SendPostAsync(message);
        }

        public Task<INostrEvent> SendReplyPostAsync(string message, INostrEvent e)
        {
            return SendPostAsync(message, "rootReference", "reference");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "<Pending>")]
        public async Task<INostrEvent> SendPostAsync(string content, string? rootReference = null, string? reference = null, string? mention = null)
        {
            if (string.IsNullOrEmpty(PublicKey))
            {
                throw new InvalidOperationException($"'{nameof(PublicKey)}' cannot be null or empty.");
            }

            var postEvent = new NostrEvent<string>()
            {
                Content = content,
                CreatedAt = DateTime.Now,
                Kind = NostrKind.TextNote,
                PublicKey = PublicKey,
            };

            if (!string.IsNullOrEmpty(rootReference))
            {
                postEvent.Tags = new() {
                    new()
                    {
                        TagIdentifier = "e",
                        Data = new List<string>()
                        {
                            rootReference,
                            "",
                            "root"
                        }
                    }
                };
                if (!string.IsNullOrEmpty(reference))
                {
                    postEvent.Tags.Add(new()
                    {
                        TagIdentifier = "e",
                        Data = new List<string>()
                        {
                            reference,
                            "",
                            "reply"
                        }
                    });
                }
            }
            var sendTasks = _relayInstances.Values.Select(r => r.SendEvent(GenerateRelayEvent(r, postEvent)));
            await Task.WhenAll(sendTasks);

            return postEvent;
        }

        private INostrEvent GenerateRelayEvent(NostrRelay r, NostrEvent<string> postEvent)
        {
            var relayEvent = postEvent.Clone();
            relayEvent.Id = CalculateId(relayEvent);
            relayEvent.Signature = CalculateSignature(relayEvent);
            return relayEvent;
        }

        private string CalculateSignature(NostrEvent<string> relayEvent)
        {
            throw new NotImplementedException();
        }

        private string CalculateId(NostrEvent<string> relayEvent)
        {
            throw new NotImplementedException();
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
