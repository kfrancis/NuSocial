using CommunityToolkit.Mvvm.Messaging.Messages;
using Nostr.Client.Keys;

namespace NuSocial.Messages
{
    public class NostrUserChangedMessage : ValueChangedMessage<(string pubKey, string privKey)>
    {
        public NostrUserChangedMessage((string pubKey, string privKey) value) : base(value)
        {
        }
    }

    public class NostrPostMessage : ValueChangedMessage<string?>
    {
        public NostrPostMessage(string? value = null) : base(value)
        {
        }
    }
}

