using System;
using System.Collections.ObjectModel;

namespace NostrLib.Models
{
    public class NostrPost
    {
        public NostrPost(INostrEvent ev)
        {
            RawEvent = ev;
        }

        public string Author => RawEvent.PublicKey;
        public string Content => (RawEvent as INostrEvent<string>)?.Content ?? string.Empty;
        public DateTime? CreatedAt => RawEvent.CreatedAt?.DateTime;
        public string Id => RawEvent.Id;
        public string MentionTo { get; set; }
        public INostrEvent RawEvent { get; set; }

        public string Reference { get; set; }
        public string RootReference { get; set; }
    }

    public class NostrProfile
    {
        public string? About { get; set; }
        public Collection<(string publicKey, string name)> Followers { get; } = new();
        public Collection<(string publicKey, string name)> Following { get; } = new();
        public string? Name { get; set; }
        public string? Picture { get; set; }
        public string? DisplayName { get; set; }
        public string? Nip05 { get; set; }
        public string? Website { get; set; }
        public Collection<(string url, bool read, bool write)> Relays { get; } = new();
    }
}
