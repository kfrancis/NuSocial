using System.Threading.Channels;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using NBitcoin.Secp256k1;
using NNostr.Client;
using NuSocial.Core.Data;
using NuSocial.Messages;

namespace NuSocial.Services
{
    public enum RelayStatus
    {
        Connecting,
        Connected,
        Disconnected
    }

    public class NostrService : IDisposable
    {
        public ECPrivKey? Key { get; set; }
        public ECXOnlyPubKey? PubKey => Key?.CreateXOnlyPubKey();
        public string PubKeyHex => PubKey?.ToBytes()?.ToHex();

        public HashSet<string> NIP4Authors { get; set; } = new();
        public Dictionary<string, string> DecryptedNIP4Content { get; set; } = new();


        public Dictionary<string, NostrEvent> Events { get; set; } = new();
        public Dictionary<string, NostrEvent> UnseenEvents { get; set; } = new();
        public MultiValueDictionary<string, string> AuthorToEvent { get; set; } = new();
        public MultiValueDictionary<Uri, string> RelayNotices { get; set; } = new();
        public MultiValueDictionary<string, string> ReplyToEvent { get; set; } = new();
        public Dictionary<string, string> ReferencedUserToEvent { get; set; } = new();

        private readonly Channel<NostrEvent> PendingIncomingEvents = Channel.CreateUnbounded<NostrEvent>();
        public Dictionary<Uri, NostrRelayListener> ActiveRelays { get; set; } = new();

        public HashSet<Uri> KnownRelays { get; set; } = new()
        {
            new Uri("wss://relay.damus.io"),
            //new Uri("wss://nostr-pub.semisol.dev")
        };

        public enum RelayStatus
        {
            Connecting,
            Connected,
            Disconnected
        }

        public NostrService()
        {
        }


        async Task<bool> ProcessEvent(NostrEvent e, CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine($"Processing event {e.Id}");
                if (!Events.TryAdd(e.Id, e)) return true;
                if (UnseenEvents.Remove(e.Id))
                {
                    EventNoLongerPending?.Invoke(this, e.Id);
                }

                AuthorToEvent.Add(e.PublicKey, e.Id);
                var taggedEvent = e.Tags.FirstOrDefault(tag => tag.TagIdentifier == "e" && tag.Data.Any())?.Data?.First();
                if (taggedEvent is not null)
                {
                    ReplyToEvent.Add(taggedEvent, e.Id);
                }

                var taggedPubKey = e.Tags.FirstOrDefault(tag => tag.TagIdentifier == "p" && tag.Data.Any())?.Data?.First();
                if (e.Kind == 4)
                {
                    if (e.PublicKey == PubKeyHex)
                    {
                        NIP4Authors.Add(e.PublicKey);
                        DecryptedNIP4Content.TryAdd(e.Id, await e.DecryptNip04Event(Key));
                    }
                    else if (taggedPubKey == PubKeyHex)
                    {
                        NIP4Authors.Add(taggedPubKey);
                        DecryptedNIP4Content.TryAdd(e.Id, await e.DecryptNip04Event(Key));
                    }
                }

                if (taggedPubKey is not null)
                    ReferencedUserToEvent.TryAdd(taggedPubKey, e.Id);

                EventProcessed.Invoke(this, e);
                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return false;
            }
        }


        public async Task Unsubscribe(string directDm)
        {
            Subscriptions.Remove(directDm);

            foreach (var activeRelay in ActiveRelays.Values)
            {
                await activeRelay.Unsubscribe(directDm);
            }
        }

        public async Task Subscribe(string directDm, NostrSubscriptionFilter[] getMessageThreadFilters)
        {
            await Unsubscribe(directDm);
            Subscriptions.TryAdd(directDm, getMessageThreadFilters);
            foreach (var activeRelay in ActiveRelays.Values)
            {
                await activeRelay.Subscribe(directDm, getMessageThreadFilters);
            }
        }

        public EventHandler<(string subscriptionId, NostrEvent[] events, Uri known)> EventsReceived;
        public EventHandler<(string tuple, Uri known)> NoticeReceived;
        public EventHandler<string> EventNoLongerPending;
        public EventHandler<NostrEvent> EventProcessed;
        public EventHandler RelayStateChanged;
        private bool disposedValue;

        public Dictionary<string, NostrSubscriptionFilter[]> Subscriptions { get; set; } = new();
        public Dictionary<string, string> Threads { get; set; } = new();

        public async Task ToggleRelay(Uri known)
        {
            void StatusChangedCore(object? sender, EventArgs e)
            {
                RelayStateChanged.Invoke(sender, e);
            }

            void NoticeReceivedCore(object? sender, string e)
            {
                RelayNotices.Add(known, e);
                NoticeReceived.Invoke(sender, (e, known));
            }

            void EventsReceivedCore(object? sender, (string subscriptionId, NostrEvent[] events) e)
            {
                foreach (var nostrEvent in e.events)
                {
                    PendingIncomingEvents.Writer.TryWrite(nostrEvent);
                }

                EventsReceived.Invoke(sender, (e.subscriptionId, e.events, known));
            }

            if (ActiveRelays.TryGetValue(known, out var activeRelay))
            {
                await activeRelay.StopAsync(CancellationToken.None);

                if (ActiveRelays.Remove(known) && activeRelay != null)
                {
                    activeRelay.EventsReceived -= EventsReceivedCore;
                    activeRelay.NoticeReceived -= NoticeReceivedCore;
                    activeRelay.StatusChanged -= StatusChangedCore;
                    
                }
                activeRelay?.Dispose();
            }
            else
            {
                KnownRelays.Add(known);
                var relay = new NostrRelayListener(known);
                relay.EventsReceived += EventsReceivedCore;
                relay.NoticeReceived += NoticeReceivedCore;
                relay.StatusChanged += StatusChangedCore;

                if (ActiveRelays.TryAdd(known, relay))
                {
                    relay.Subscriptions = Subscriptions;
                    await relay.StartAsync(CancellationToken.None);
                }
            }
        }

        public NostrEvent? GetEvent(string id, out bool pending)
        {
            pending = false;
            if (!Events.TryGetValue(id, out var nostrEvent))
            {
                if (UnseenEvents.TryGetValue(id, out nostrEvent))
                {
                    pending = true;
                }
            }

            return nostrEvent;
        }

        public Task StartAsync(CancellationToken token)
        {
            _ = ProcessChannel(PendingIncomingEvents, ProcessEvent, token);

            return Task.CompletedTask;

        }

        public Task StopAsync(CancellationToken token)
        {
            return Task.CompletedTask;
        }


        private async Task ProcessChannel<T>(Channel<T> channel, Func<T, CancellationToken, Task<bool>> processor,
            CancellationToken cancellationToken = default)
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

        public async Task SendEvent(NostrEvent evt)
        {
            UnseenEvents.Add(evt.Id, evt);
            foreach (var keyValuePair in ActiveRelays)
            {
                await keyValuePair.Value.SendEvent(evt);
            }
        }


        public string GetContent(string eventId, out NostrEvent nostrEvent)
        {
            if (Events.TryGetValue(eventId, out nostrEvent) || UnseenEvents.TryGetValue(eventId, out nostrEvent))
            {
                return GetContent(nostrEvent);
            }

            return "Event unavailable";
        }

        public string GetContent(NostrEvent nostrEvent)
        {
            if (nostrEvent.Kind == 4 && DecryptedNIP4Content.TryGetValue(nostrEvent.Id, out var decrypted))
            {
                return decrypted;
            }
            else if (nostrEvent.Kind == 4)
            {
            }

            return nostrEvent.Content;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~NostrService()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
