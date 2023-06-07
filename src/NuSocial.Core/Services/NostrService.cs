using Bogus;
using CommunityToolkit.Mvvm.Messaging;
using DynamicData;
using Microsoft.Extensions.Logging.Abstractions;
using Nostr.Client.Client;
using Nostr.Client.Communicator;
using Nostr.Client.Keys;
using Nostr.Client.Messages;
using Nostr.Client.Messages.Contacts;
using Nostr.Client.Messages.Metadata;
using Nostr.Client.Requests;
using Nostr.Client.Responses;
using NostrClient.Helpers;
using NuSocial.Messages;
using NuSocial.Models;
using Serilog;
using System.Net.WebSockets;
using Websocket.Client.Models;
using Contact = NuSocial.Models.Contact;

namespace NuSocial.Services
{
    public interface INostrService
    {
        NostrClientStreams? Streams { get; }

        void Dispose();

        Task<Profile> GetProfileAsync(string publicKey, bool getExtra = false, CancellationToken ct = default);

        void RegisterFilter(string subscription, NostrFilter filter);
        void Send(string v, NostrFilter nostrFilter);
        void StartNostr();
        Task StartNostrAsync(CancellationToken ct = default);

        void StopNostr();
        Task StopNostrAsync(CancellationToken ct = default);
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
                    var keyPair = new NostrKeyPair(NostrPrivateKey.GenerateNew(), NostrPublicKey.FromHex(pubKey));
                    await Setup(keyPair);
                }
                else
                {
                    // nothing usable
                }
            }
        }

        public void Send(string v, NostrFilter nostrFilter)
        {
            _client?.Send(new NostrRequest(v, nostrFilter));
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
                        NostrKind.Metadata,
                        NostrKind.ShortTextNote,
                        NostrKind.EncryptedDm,
                        NostrKind.Reaction,
                        NostrKind.Contacts,
                        NostrKind.RecommendRelay,
                        NostrKind.EventDeletion,
                        NostrKind.Reporting,
                        NostrKind.ClientAuthentication
                    },
                    Limit = 0,
                    Since = DateTime.UtcNow.AddHours(-12),
                    Until = DateTime.UtcNow.AddHours(4)
                });

                await StartNostrAsync();

                WeakReferenceMessenger.Default.Send<NostrReadyMessage>(new(true));
                
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

        public async Task StartNostrAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (_communicators != null)
            {
                var startTasks = _communicators.Select(x => x.Start());
                await Task.WhenAll(startTasks);
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

        public async Task StopNostrAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (_communicators != null)
            {
                var stopTasks = _communicators.Select(x => x.Stop(WebSocketCloseStatus.NormalClosure, string.Empty));
                await Task.WhenAll(stopTasks);
            }
        }

        private async Task<INostrCommunicator[]> CreateCommunicatorsAsync()
        {
            var relays = await _db.GetRelaysAsync();

            if (!relays.Any())
            {
                // make sure there's at least one
                relays.Add(new Relay("wss://relayable.org"));
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
            Log.Debug("{kind}: {content}", ev?.Kind, ev?.Content);

            if (response.Event != null && response.Event.IsSignatureValid())
            {
                switch (response.Event.Kind)
                {
                    case NostrKind.Metadata:
                        if (ev is NostrMetadataEvent evm)
                        {
                            Log.Debug("Name: {name}, about: {about}", evm.Metadata?.Name, evm.Metadata?.About);
                            WeakReferenceMessenger.Default.Send<NostrMetadataMessage>(new(evm));
                        }
                        break;

                    case NostrKind.ShortTextNote:
                        WeakReferenceMessenger.Default.Send<NostrPostMessage>(new(ev));
                        break;

                    case NostrKind.RecommendRelay:
                        break;

                    case NostrKind.Contacts:
                        if (ev is NostrContactEvent evc)
                        {
                            WeakReferenceMessenger.Default.Send<NostrContactMessage>(new(evc));
                        }
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

        public async Task<Profile> GetProfileAsync(string publicKey, bool getExtra = false, CancellationToken ct = default)
        {
            if (_client == null) throw new InvalidOperationException();

            var profileEvents = new List<NostrMetadataEvent>();
            var contactEvents = new List<NostrContactEvent>();

            WeakReferenceMessenger.Default.Unregister<NostrMetadataMessage>(this);
            WeakReferenceMessenger.Default.Register<NostrMetadataMessage>(this, (r, m) =>
            {
                if (m.Value != null)
                    profileEvents.Add(m.Value);
            });
            WeakReferenceMessenger.Default.Unregister<NostrContactMessage>(this);
            WeakReferenceMessenger.Default.Register<NostrContactMessage>(this, (r, m) =>
            {
                if (m.Value != null)
                    contactEvents.Add(m.Value);
            });
            var subscription = _client.SendProfileRequest(publicKey, NostrKind.Metadata, NostrKind.Contacts);
            await Task.Delay(TimeSpan.FromSeconds(_profileThreshold), ct);
            WeakReferenceMessenger.Default.Unregister<NostrMetadataMessage>(this);
            WeakReferenceMessenger.Default.Unregister<NostrContactMessage>(this);
            var profileInfo = new Profile();
            var latestMetadataEvent = profileEvents.Where(x => x.Kind == NostrKind.Metadata).OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            if (latestMetadataEvent != null && !string.IsNullOrEmpty(latestMetadataEvent.Content))
            {
                using var doc = JsonDocument.Parse(latestMetadataEvent.Content.Replace("\\", "", StringComparison.OrdinalIgnoreCase));
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

            if (getExtra)
            {
                var latestContactEvent = contactEvents.OrderByDescending(x => x.CreatedAt).FirstOrDefault();

                if (latestContactEvent != null)
                {
                    foreach (var relay in latestContactEvent.Relays)
                    {
                        profileInfo.Relays.Add(new Relay(relay.Key, relay.Value.Read, relay.Value.Write));
                    }
                    if (latestContactEvent.Tags != null)
                    {
                        foreach (var tag in latestContactEvent.Tags.Where(t => !string.IsNullOrEmpty(t.TagIdentifier) && t.TagIdentifier.Equals("p", StringComparison.OrdinalIgnoreCase) && t.AdditionalData != null))
                        {
                            var followData = tag.AdditionalData.ToList();
                            var followsHex = followData[0].ToString();
                            if (!string.IsNullOrEmpty(followsHex))
                            {
                                var followContact = new Contact() { PublicKey = followsHex };
                                if (followData.Count > 1)
                                {
                                    followContact.Relay = followData[1].ToString();
                                }
                                if (followData.Count > 2 && !string.IsNullOrEmpty(followData[2].ToString()))
                                {
                                    followContact.PetName = followData[2].ToString()!;
                                }
                                profileInfo.Follows.Add(followContact);
                            }
                        }
                    }
                }
            }

            return profileInfo;
        }

        private Task<NostrMetadataEvent> GetFollowingInfoAsync(string publicKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    public class TestNostrService : INostrService, IDisposable
    {
        private bool _isDisposed;
        private readonly Faker<NostrEvent> _nostrFaker;
        private readonly Faker<Profile> _profileFaker;
        private Timer? _timer;

        public TestNostrService()
        {
            var faker = new Faker();
            List<NostrKeyPair> authorPool = new List<NostrKeyPair>();
            for (int i = 0; i < faker.Random.Int(10, 100); i++)
            {
                var keyPair = NostrKeyPair.GenerateNew();
                authorPool.Add(keyPair);
            }
            _nostrFaker = new Faker<NostrEvent>()
                .RuleFor(n => n.Pubkey, f => authorPool[f.UniqueIndex % authorPool.Count].PublicKey.Bech32)
                .RuleFor(n => n.CreatedAt, f => f.Date.Recent(f.Random.Int(1, 7)))
                .RuleFor(n => n.Kind, f => NostrKind.ShortTextNote)
                .RuleFor(n => n.Tags, f => default)
                .RuleFor(n => n.Content, f => TestNostrService.GenerateRandomMarkdownContent(f))
                .RuleFor(n => n.Id, (f, n) => n.ComputeId())
                .RuleFor(n => n.Sig, (f, n) =>
                {
                    var index = f.UniqueIndex % authorPool.Count;
                    var keyPair = authorPool[index];
                    var sig = n.ComputeSignature(keyPair.PrivateKey);
                    return sig;
                })
                .FinishWith((f, e) =>
                {
                    Console.WriteLine("Content event generated! Id={0}", e.Id);
                });

            _profileFaker = new Faker<Profile>()
                .RuleFor(p => p.Name, f => f.Internet.UserName())
                .RuleFor(p => p.DisplayName, f => f.Random.Bool(0.2f) ? f.Internet.UserName() : null)
                .RuleFor(p => p.Picture, f => f.Random.Bool(0.8f) ? f.Internet.Avatar() : null)
                .RuleFor(p => p.Relays, f => default);
        }

        private static string GenerateRandomMarkdownContent(Faker faker)
        {
            var markdownContent = $"{string.Join(" ", faker.Lorem.Words(faker.Random.Int(1, 3)))}\n\n";
            var hasSomethingInteresting = false;
            if (!hasSomethingInteresting && faker.Random.Bool(0.05f))
            {
                for (var i = 0; i < faker.Random.Int(1, 3); i++)
                {
                    markdownContent += $"{faker.Lorem.Paragraph()}\n\n";
                }
                hasSomethingInteresting = true;
            }

            // Add a list
            if (!hasSomethingInteresting && faker.Random.Bool(0.05f))
            {
                markdownContent += "## List\n\n";
                for (var i = 0; i < faker.Random.Int(2, 5); i++)
                {
                    markdownContent += $"* {faker.Lorem.Sentence()}\n";
                }
                hasSomethingInteresting = true;
                if (faker.Random.Bool(0.05f))
                {
                    hasSomethingInteresting = false;
                }
            }

            // Add a quote
            if (!hasSomethingInteresting && faker.Random.Bool(0.05f))
            {
                markdownContent += "\n> " + faker.Lorem.Sentence() + "\n\n";
                hasSomethingInteresting = true;
                if (faker.Random.Bool(0.05f))
                {
                    hasSomethingInteresting = false;
                }
            }

            // Add a code block
            if (!hasSomethingInteresting && faker.Random.Bool(0.05f))
            {
                markdownContent += "```\n" + faker.Lorem.Sentence() + "\n```\n\n";
                hasSomethingInteresting = true;
                if (faker.Random.Bool(0.05f))
                {
                    hasSomethingInteresting = false;
                }
            }

            // Add a table
            if (!hasSomethingInteresting && faker.Random.Bool(0.05f))
            {
                markdownContent += "| Column 1 | Column 2 |\n| -------- | -------- |\n";
                for (var i = 0; i < faker.Random.Int(2, 5); i++)
                {
                    markdownContent += $"| {faker.Lorem.Word()} | {faker.Lorem.Word()} |\n";
                }
                hasSomethingInteresting = true;
                if (faker.Random.Bool(0.05f))
                {
                    hasSomethingInteresting = false;
                }
            }

            // Add an emoji
            if (!hasSomethingInteresting && faker.Random.Bool(0.30f))
            {
                var emojis = new[] { "😀", "😃", "😄", "😁", "😆", "😅", "😂", "🤣", "😊", "😇" };
                markdownContent += "\n" + faker.PickRandom(emojis) + "\n";
                hasSomethingInteresting = true;
                if (faker.Random.Bool(0.05f))
                {
                    hasSomethingInteresting = false;
                }
            }

            return markdownContent;
        }

        public NostrClientStreams? Streams => throw new NotImplementedException();


        public Task<Profile> GetProfileAsync(string publicKey, bool getExtra = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_profileFaker.Generate());
        }

        public void RegisterFilter(string subscription, NostrFilter filter)
        {
            // Do nothing
        }

        public void StartNostr()
        {
            // var faker = new Faker();
            //_timer = new Timer(SendNostrPostMessage, null, 0, faker.Random.Int(1000, 5000));
        }

        private void SendNostrPostMessage(object? state)
        {
            var nostrEvent = _nostrFaker.Generate();
            var message = new NostrPostMessage(nostrEvent);
            WeakReferenceMessenger.Default.Send(message);
        }

        public void StopNostr()
        {
            _timer?.Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _timer?.Dispose();
                }

                _isDisposed = true;
            }
        }

        ~TestNostrService()
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

        public void Send(string v, NostrFilter nostrFilter)
        {
            throw new NotImplementedException();
        }

        public Task StartNostrAsync(CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task StopNostrAsync(CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}