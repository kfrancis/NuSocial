using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NBitcoin.Secp256k1;
using NostrLib.Models;
using System.Threading.Tasks;
using System.Security;

namespace NostrLib.Nips
{
    public static class NIP04
    {
        private static readonly IAesEncryptor s_encryptor = new AesEncryptor();

        public static async Task DecryptNip04Event(this NostrEvent<string> nostrEvent, ECPrivKey key)
        {
            if (nostrEvent is null || string.IsNullOrEmpty(nostrEvent.Content))
            {
                throw new ArgumentNullException(nameof(nostrEvent));
            }

            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (nostrEvent.Kind != NostrKind.EncryptedDM)
            {
                throw new ArgumentException("event is not of kind 4", nameof(nostrEvent));
            }

            var receiverPubKeyStr = nostrEvent.Tags.FirstOrDefault(tag => tag.TagIdentifier == "p")?.Data?.First();

            if (string.IsNullOrEmpty(receiverPubKeyStr))
            {
                throw new ArgumentException("Could not find receiver pub key");
            }

            var ourPubKey = key.CreateXOnlyPubKey();
            var ourPubKeyHex = ourPubKey.ToBytes().ToHex();
            var areWeSender = false;
            var receiverPubKey = Context.Instance.CreateXOnlyPubKey(receiverPubKeyStr.DecodHexData());

            var receiverPubKeyHex = receiverPubKey.ToBytes().ToHex();
            var senderPubkKey = nostrEvent.GetPublicKey();
            if (nostrEvent.PublicKey == ourPubKeyHex)
            {
                areWeSender = true;
            }
            else if (receiverPubKeyHex == ourPubKeyHex)
            {
                areWeSender = false;
            }
            else
            {
                throw new ArgumentException("key does not match recipients of this event", nameof(key));
            }

            var sharedKey = GetSharedPubkey(areWeSender ? receiverPubKey : senderPubkKey, key)?.ToBytes().Skip(1).ToArray();
            var encrypted = nostrEvent.Content.Split("?iv=");
            var encryptedText = encrypted[0];
            var iv = encrypted[1];

            using var secureIv = iv.ToSecureString();
            using var secureContent = await s_encryptor.Decrypt(encryptedText, secureIv);

            nostrEvent.Content = secureContent.ToPlainString();
        }

        private static readonly byte[] s_posBytes = "02".DecodHexData();

        private static ECPubKey? GetSharedPubkey(this ECXOnlyPubKey ecxOnlyPubKey, ECPrivKey key)
        {
            Context.Instance.TryCreatePubKey(s_posBytes.Concat(ecxOnlyPubKey.ToBytes()).ToArray(), out var mPubKey);
            return mPubKey?.GetSharedPubkey(key);
        }

        public static async Task EncryptNip04Event(this NostrEvent<string> nostrEvent, ECPrivKey key)
        {
            if (nostrEvent is null)
            {
                throw new ArgumentNullException(nameof(nostrEvent));
            }

            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (string.IsNullOrEmpty(nostrEvent.Content))
            {
                throw new ArgumentNullException(nameof(nostrEvent));
            }

            var sharedPubKey = key.CreateXOnlyPubKey().GetSharedPubkey(key);

            if (sharedPubKey == null) { throw new Exception("Couldn't get the shared public key"); }

            using var secureContent = nostrEvent.Content.ToSecureString();
            var (cipherText, iv) = await s_encryptor.Encrypt(secureContent, sharedPubKey.ToBytes().Skip(1).ToArray());

            nostrEvent.Content = $"{cipherText}?iv={iv}";
        }
    }
}
