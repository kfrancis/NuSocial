using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using NostrLib.Converters;

namespace NostrLib.Models
{
    public enum NostrKind
    {
        SetMetadata = 0,
        TextNote = 1,
        RecommendServer = 2,
        Contacts = 3,
        EncryptedDM = 4,
        Deletion = 5,
        Reaction = 7,

        /// <summary>
        /// nip-28
        /// </summary>
        ChannelCreate = 40,

        /// <summary>
        /// nip-28
        /// </summary>
        ChannelMetadata = 41,

        /// <summary>
        /// nip-28
        /// </summary>
        ChannelMessage = 42,

        /// <summary>
        /// nip-28
        /// </summary>
        HideMessage = 43,

        /// <summary>
        /// nip-28
        /// </summary>
        MuteUser = 44,

        //Reserved1 = 45,
        //Reserved2 = 46,
        //Reserved3 = 47,
        //Reserved4 = 48,
        //Reserved5 = 49,
    }

    public interface INostrEvent
    {
        DateTimeOffset? CreatedAt { get; set; }
        bool Deleted { get; set; }
        string Id { get; set; }
        NostrKind Kind { get; set; }
        string PublicKey { get; set; }
        string Signature { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "<Pending>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "<Pending>")]
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
        public NostrKind Kind { get; set; }

        [JsonPropertyName("pubkey")]
        public string PublicKey { get; set; }

        [JsonPropertyName("sig")]
        public string Signature { get; set; }

        [JsonPropertyName("tags")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "<Pending>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "<Pending>")]
        public List<NostrEventTag> Tags { get; set; }

        public bool Equals(NostrEvent<TBody>? x, NostrEvent<TBody>? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x?.GetType() != y?.GetType()) return false;
            return x?.Id == y?.Id;
        }

        public int GetHashCode(NostrEvent<TBody> obj)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            return obj.Id.GetHashCode(StringComparison.InvariantCultureIgnoreCase);
        }

        public NostrEvent<TBody> Clone()
        {
            return new NostrEvent<TBody>()
            {
                Content = this.Content,
                Kind = this.Kind,
                Tags = this.Tags,
                Signature = this.Signature,
                CreatedAt = this.CreatedAt,
                PublicKey = this.PublicKey,
                Deleted = this.Deleted,
                Id = this.Id
            };
        }
    }
}
