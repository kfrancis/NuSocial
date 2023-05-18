using CommunityToolkit.Mvvm.Messaging.Messages;
using Nostr.Client.Keys;
using Nostr.Client.Messages;
using Nostr.Client.Messages.Metadata;

namespace NuSocial.Messages
{
    public class NostrUserChangedMessage : ValueChangedMessage<(string pubKey, string privKey)>
    {
        public NostrUserChangedMessage((string pubKey, string privKey) value) : base(value)
        {
        }
    }

    public class NostrPostMessage : ValueChangedMessage<NostrEvent?>
    {
        public NostrPostMessage(NostrEvent? value = null) : base(value)
        {
        }
    }

    public class NostrMetadataMessage : ValueChangedMessage<NostrMetadataEvent?>
    {
        public NostrMetadataMessage(NostrMetadataEvent? value = null) : base(value)
        {
        }
    }
}

