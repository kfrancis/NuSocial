using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Nostr.Client.Client;
using Nostr.Client.Communicator;
using Nostr.Client.Keys;
using Nostr.Client.Messages;
using Nostr.Client.Messages.Metadata;
using Nostr.Client.Requests;
using Nostr.Client.Responses;
using NuSocial.Messages;
using Serilog;
using System.Net.WebSockets;
using Websocket.Client.Models;

namespace NuSocial.Services
{
    public interface INostrService
    {
        NostrClientStreams? Streams { get; }

        void Dispose();

        Task<Profile> GetProfileAsync(string publicKey, CancellationToken cancellationToken);

        void RegisterFilter(string subscription, NostrFilter filter);

        void StartNostr();

        void StopNostr();
    }

    public class NostrService : INostrService, IDisposable
    {
        private readonly IDatabase _db;
        private NostrMultiWebsocketClient? _client;
        private INostrCommunicator[]? _communicators;
        private bool _isDisposed;
        private double _profileThreshold = 5;
        private readonly Dictionary<string, NostrFilter> _subscriptionToFilter = new();

        public NostrService(IDatabase db)
        {
            _db = db;
            WeakReferenceMessenger.Default.Register<NostrUserChangedMessage>(this, (r, m) =>
            {
                Receive(m);
            });
        }

        public async void Receive(NostrUserChangedMessage message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (message.Value is (string pubKey, string privKey))
            {
                if (!string.IsNullOrEmpty(privKey))
                {
                    var keyPair = NostrKeyPair.From(NostrPrivateKey.FromHex(privKey));
                    await Setup(keyPair);
                }
                else if (!string.IsNullOrEmpty(pubKey))
                {
                }
                else
                {
                    // nothing usable
                }
            }
        }

        public NostrClientStreams? Streams => _client?.Streams;

        private async Task Setup(NostrKeyPair keyPair)
        {
            try
            {
                _communicators = await CreateCommunicatorsAsync();

                _client = new NostrMultiWebsocketClient(NullLogger<NostrWebsocketClient>.Instance, _communicators);

                _client.Streams.EventStream.Subscribe(HandleEvent);

                RegisterFilter(keyPair.PublicKey.Hex, new NostrFilter()
                {
                    Kinds = new[]
                    {
                        NostrKind.ShortTextNote
                    },
                    Limit = 0
                });

                StartNostr();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void RegisterFilter(string subscription, NostrFilter filter)
        {
            _subscriptionToFilter[subscription] = filter;
        }

        public void StartNostr()
        {
            if (_communicators != null)
            {
                foreach (var comm in _communicators)
                {
                    // fire and forget
                    _ = comm.Start();
                }
            }
        }

        public void StopNostr()
        {
            if (_communicators != null)
            {
                foreach (var comm in _communicators)
                {
                    // fire and forget
                    _ = comm.Stop(WebSocketCloseStatus.NormalClosure, string.Empty);
                }
            }
        }

        private async Task<INostrCommunicator[]> CreateCommunicatorsAsync()
        {
            var relays = await _db.GetRelaysAsync();

            if (!relays.Any())
            {
                // make sure there's at least one
                relays.Add(new Relay("wss://relay.damus.io"));
                await _db.UpdateRelaysAsync(relays);
            }

            return relays.Where(x => x.Uri != null).Select(x => CreateCommunicator(x.Uri!)).ToArray();
        }

        private INostrCommunicator CreateCommunicator(Uri uri)
        {
            var comm = new NostrWebsocketCommunicator(uri, () =>
            {
                var client = new ClientWebSocket();
                client.Options.SetRequestHeader("Origin", "http://localhost");
                return client;
            });

            comm.Name = uri.Host;
            comm.ReconnectTimeout = null; //TimeSpan.FromSeconds(30);
            comm.ErrorReconnectTimeout = TimeSpan.FromSeconds(60);

            comm.ReconnectionHappened.Subscribe(info => OnCommunicatorReconnection(info, comm.Name));
            comm.DisconnectionHappened.Subscribe(info =>
                Log.Information("[{relay}] Disconnected, type: {type}, reason: {reason}", comm.Name, info.Type, info.CloseStatus));
            return comm;
        }

        private void OnCommunicatorReconnection(ReconnectionInfo info, string communicatorName)
        {
            try
            {
                if (_client == null) return;

                Log.Information("[{relay}] Reconnected, sending Nostr filters ({filterCount})", communicatorName, _subscriptionToFilter.Count);

                var client = _client.FindClient(communicatorName);
                if (client == null)
                {
                    Log.Warning("[{relay}] Cannot find client", communicatorName);
                    return;
                }

                foreach (var (sub, filter) in _subscriptionToFilter)
                {
                    client.Send(new NostrRequest(sub, filter));
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "[{relay}] Failed to process reconnection, error: {error}", communicatorName, e.Message);
            }
        }

        private void HandleEvent(NostrEventResponse response)
        {
            if (response is null)
            {
                throw new ArgumentNullException(nameof(response));
            }
            var ev = response.Event;
            Log.Information("{kind}: {content}", ev?.Kind, ev?.Content);

            if (ev is NostrMetadataEvent evm)
            {
                WeakReferenceMessenger.Default.Send<NostrMetadataMessage>(new(evm));
                Log.Information("Name: {name}, about: {about}", evm.Metadata?.Name, evm.Metadata?.About);
            }

            if (response.Event != null && response.Event.IsSignatureValid())
            {
                switch (response.Event.Kind)
                {
                    case NostrKind.Metadata:
                        break;

                    case NostrKind.ShortTextNote:
                        WeakReferenceMessenger.Default.Send<NostrPostMessage>(new(ev));
                        break;

                    case NostrKind.RecommendRelay:
                        break;

                    case NostrKind.Contacts:
                        break;

                    case NostrKind.EncryptedDm:
                        break;

                    case NostrKind.EventDeletion:
                        break;

                    case NostrKind.Reserved:
                        break;

                    case NostrKind.Reaction:
                        break;

                    case NostrKind.BadgeAward:
                        break;

                    case NostrKind.ChannelCreation:
                        break;

                    case NostrKind.ChannelMetadata:
                        break;

                    case NostrKind.ChannelMessage:
                        break;

                    case NostrKind.ChannelHideMessage:
                        break;

                    case NostrKind.ChanelMuteUser:
                        break;

                    case NostrKind.Reporting:
                        break;

                    case NostrKind.ZapRequest:
                        break;

                    case NostrKind.Zap:
                        break;

                    case NostrKind.RelayListMetadata:
                        break;

                    case NostrKind.ClientAuthentication:
                        break;

                    case NostrKind.NostrConnect:
                        break;

                    case NostrKind.ProfileBadges:
                        break;

                    case NostrKind.BadgeDefinition:
                        break;

                    case NostrKind.LongFormContent:
                        break;

                    case NostrKind.ApplicationSpecificData:
                        break;

                    default:
                        break;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _client?.Dispose();
                    if (_communicators != null)
                    {
                        foreach (var comm in _communicators)
                        {
                            comm.Dispose();
                        }
                    }
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

        public async Task<Profile> GetProfileAsync(string publicKey, CancellationToken cancellationToken)
        {
            RegisterFilter(publicKey, new NostrFilter()
            {
                Kinds = new[]
                {
                    NostrKind.Metadata
                },
                Authors = new[]
                {
                    publicKey
                },
                Limit = 0
            });

            var relays = await _db.GetRelaysAsync();
            var relay = relays.FirstOrDefault(r => r.Uri != null);
            var profileEvents = new List<NostrMetadataEvent>();
            if (relay != null)
            {
                WeakReferenceMessenger.Default.Unregister<NostrMetadataMessage>(this);
                WeakReferenceMessenger.Default.Register<NostrMetadataMessage>(this, (r, m) =>
                {
                    if (m.Value != null)
                        profileEvents.Add(m.Value);
                });
                var ev = new NostrRequest(publicKey, new NostrFilter() { Authors = new[] { publicKey }, Kinds = new[] { NostrKind.Metadata } });
                _client.Send<NostrRequest>(ev);
                await Task.Delay(TimeSpan.FromSeconds(_profileThreshold), cancellationToken);
                WeakReferenceMessenger.Default.Unregister<NostrMetadataMessage>(this);
            }

            var profileInfo = new Profile();
            var latestProfileEvent = profileEvents.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            if (latestProfileEvent != null && !string.IsNullOrEmpty(latestProfileEvent.Content))
            {
                using var doc = JsonDocument.Parse(latestProfileEvent.Content.Replace("\\", "", StringComparison.OrdinalIgnoreCase));
                var json = doc.RootElement;
                if (json.TryGetProperty("name", out var nameJson))
                {
                    profileInfo.Name = nameJson.GetString() ?? string.Empty;
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

            //var followingInfo = await GetFollowingInfoAsync(publicKey, cancellationToken);
            //if (followingInfo != null &&
            //    !string.IsNullOrEmpty(followingInfo.Content))
            //{
            //    using var doc = JsonDocument.Parse(followingInfo.Content.Replace("\\", "", StringComparison.OrdinalIgnoreCase));
            //    var json = doc.RootElement;
            //    foreach (var key in json.EnumerateObject())
            //    {
            //        profileInfo.Relays.Add(new(key.Name, key.Value.GetProperty("read").GetBoolean(), key.Value.GetProperty("write").GetBoolean()));
            //    }
            //}

            return profileInfo;
        }

        private Task<NostrMetadataEvent> GetFollowingInfoAsync(string publicKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}