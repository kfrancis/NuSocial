using NostrLib.Converters;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NostrLib.Models
{
    public interface INostrEvent
    {
        DateTimeOffset? CreatedAt { get; set; }
        bool Deleted { get; set; }
        string Id { get; set; }
        int Kind { get; set; }
        string PublicKey { get; set; }
        string Signature { get; set; }
        List<NostrEventTag> Tags { get; set; }
    }

    public interface INostrEvent<TBody> : INostrEvent
    {
        TBody? Content { get; set; }
    }

    public class NostrEvent<TBody> : IEqualityComparer<NostrEvent<TBody>>, INostrEvent<TBody>
        where TBody : class
    {
        [JsonPropertyName("content")]
        [JsonConverter(typeof(StringEscaperJsonConverter))]
        public TBody? Content { get; set; }

        [JsonPropertyName("created_at")]
        [JsonConverter(typeof(UnixTimestampSecondsJsonConverter))]
        public DateTimeOffset? CreatedAt { get; set; }

        [JsonIgnore] public bool Deleted { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("kind")]
        public int Kind { get; set; }

        [JsonPropertyName("pubkey")]
        public string PublicKey { get; set; }

        [JsonPropertyName("sig")]
        public string Signature { get; set; }

        [JsonPropertyName("tags")]
        public List<NostrEventTag> Tags { get; set; }

        public bool Equals(NostrEvent<TBody>? x, NostrEvent<TBody>? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x?.GetType() != y?.GetType()) return false;
            return x?.Id == y?.Id;
        }

        public int GetHashCode(NostrEvent<TBody> obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}