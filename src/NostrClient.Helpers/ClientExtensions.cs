using Nostr.Client.Client;
using Nostr.Client.Keys;
using Nostr.Client.Messages;
using Nostr.Client.Requests;
using System.Reflection;

namespace NostrClient.Helpers
{
    /// <summary>
    /// Extensions for INostrClient
    /// </summary>
    public static class ClientExtensions
    {
        /// <summary>
        /// Send a note
        /// </summary>
        /// <param name="client"></param>
        /// <param name="senderPrivBech"></param>
        /// <param name="message"></param>
        public static void Send(this INostrClient client, string senderPrivBech, string message)
        {
            var key = NostrPrivateKey.FromBech32(senderPrivBech);
            client.Send(key, message);
        }

        /// <summary>
        /// Send a note
        /// </summary>
        /// <param name="client"></param>
        /// <param name="senderPriv"></param>
        /// <param name="message"></param>
        public static void Send(this INostrClient client, NostrPrivateKey senderPriv, string message)
        {
            var ev = new NostrEvent
            {
                Kind = NostrKind.ShortTextNote,
                CreatedAt = DateTime.UtcNow,
                Content = message
            };

            var signed = ev.Sign(senderPriv);

            client.Send(new NostrEventRequest(signed));
        }

        /// <summary>
        /// Send an encrypted message
        /// </summary>
        /// <param name="client"></param>
        /// <param name="senderPrivBech"></param>
        /// <param name="receiverPubBech"></param>
        /// <param name="message"></param>
        public static void SendEncrypted(this INostrClient client, string senderPrivBech, string receiverPubBech, string message)
        {
            var sender = NostrPrivateKey.FromBech32(senderPrivBech);
            var receiver = NostrPublicKey.FromBech32(receiverPubBech);

            client.SendEncrypted(sender, receiver, message);
        }

        /// <summary>
        /// Send an encrypted message
        /// </summary>
        /// <param name="client"></param>
        /// <param name="sender"></param>
        /// <param name="receiver"></param>
        /// <param name="message"></param>
        public static void SendEncrypted(this INostrClient client, NostrPrivateKey sender, NostrPublicKey receiver, string message)
        {
            var ev = new NostrEvent
            {
                CreatedAt = DateTime.UtcNow,
                Content = message
            };

            var encrypted = ev.EncryptDirect(sender, receiver);
            var signed = encrypted.Sign(sender);

            client.Send(new NostrEventRequest(signed));
        }

        public static void SendZap(this INostrClient client)
        {
            // send event of kind 23194 to The content is a pay_invoice
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="privateKey"></param>
        public static string SendProfileRequest(this INostrClient client, NostrPrivateKey privateKey, params NostrKind[] kinds)
        {
            return SendProfileRequest(client, privateKey.DerivePublicKey().Hex, kinds);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="publicKey"></param>
        public static string SendProfileRequest(this INostrClient client, NostrPublicKey publicKey, params NostrKind[] kinds)
        {
            return SendProfileRequest(client, publicKey.Hex, kinds);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="publicKey"></param>
        /// <param name="kinds"></param>
        public static string SendProfileRequest(this INostrClient client, string publicKey, params NostrKind[] kinds)
        {
            var subscription = $"profile:pubkey:{Guid.NewGuid():N}";
            if (kinds.Length > 0)
            {
                client.Send(new NostrRequest(subscription, new NostrFilter() { Kinds = kinds, Authors = new[] { publicKey } }));
            } else
            {
                client.Send(new NostrRequest(subscription, new NostrFilter() { Kinds = new[] { NostrKind.Metadata }, Authors = new[] { publicKey } }));
            }
            
            return subscription;
        }
    }
}