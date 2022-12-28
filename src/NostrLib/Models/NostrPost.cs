using System;
using System.Collections;
using System.Collections.Generic;

namespace NostrLib.Models
{
    public class NostrProfile
    {
        public string? Name { get; set; }
        public string? Picture { get; set; }
        public string? About { get; set; }
        public List<(string url, bool read, bool write)> Relays { get; set; } = new();
        public List<(string publicKey, string name)> Following { get; set; } = new();
        public List<(string publicKey, string name)> Followers { get; set; } = new();
    }

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
        public INostrEvent RawEvent { get; set; }

        public string Reference { get; set; }
        public string RootReference { get; set; }
        public string MentionTo { get; set; }
    }
}