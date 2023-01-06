using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NBitcoin.Secp256k1;
using NostrLib.Models;
using System.Threading.Tasks;

namespace NostrLib.Nips
{
    public static class NIP04
    {
        //private static readonly IAesEncryptor s_encryptor = new AesEncryptor();

        public static Task<string> DecryptNip04Event(this NostrEvent<string> nostrEvent, ECPrivKey key)
        {
            if (nostrEvent is null)
            {
                throw new ArgumentNullException(nameof(nostrEvent));
            }

            if (nostrEvent.Kind != NostrKind.EncryptedDM)
            {
                throw new ArgumentException("event is not of kind 4", nameof(nostrEvent));
            }

            return Task.FromResult("");
        }

        private static readonly byte[] s_posBytes = "02".DecodHexData();

        private static ECPubKey? GetSharedPubkey(this ECXOnlyPubKey ecxOnlyPubKey, ECPrivKey key)
        {
            Context.Instance.TryCreatePubKey(s_posBytes.Concat(ecxOnlyPubKey.ToBytes()).ToArray(), out var mPubKey);
            return mPubKey?.GetSharedPubkey(key);
        }

        public static Task EncryptNip04Event(this NostrEvent<string> nostrEvent, ECPrivKey key)
        {
            return Task.CompletedTask;
        }
    }
}
