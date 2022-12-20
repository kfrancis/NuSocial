using LinqKit;
using NBitcoin.Secp256k1;
using NostrLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NostrLib
{
    public interface IAesEncryptor
    {
        Task<string> Decrypt(string cipherText, string iv, byte[] key);

        Task<(string cipherText, string iv)> Encrypt(string plainText, byte[] key);
    }

    public static class EnumeratorExtensions
    {
        public static IEnumerable<T> ToEnumerable<T>(this IEnumerator<T> enumerator)
        {
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }
    }

    public static class NIP04
    {
        public static IAesEncryptor Encryptor = new AesEncryptor();

        private static readonly byte[] posBytes = "02".DecodHexData();

        public static Task<string> DecryptNip04Event(this NostrEvent nostrEvent, ECPrivKey key)
        {
            if (nostrEvent.Kind != 4)
            {
                throw new ArgumentException("event is not of kind 4", nameof(nostrEvent));
            }

            var receiverPubKeyStr = nostrEvent.Tags.Find(tag => tag.TagIdentifier == "p")?.Data?.First();
            if (receiverPubKeyStr is null)
            {
                throw new ArgumentException("event did not specify a receiver pub key", nameof(nostrEvent));
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

            var sharedKey = GetSharedPubkey(areWeSender ? receiverPubKey : senderPubkKey, key).ToBytes().Skip(1).ToArray();
            var encrypted = nostrEvent.Content.Split("?iv=");
            var encryptedText = encrypted[0];
            var iv = encrypted[1];
            return Encryptor.Decrypt(encryptedText, iv, sharedKey);
        }

        public static async Task EncryptNip04Event(this NostrEvent nostrEvent, ECPrivKey key)
        {
            if (nostrEvent.Kind != 4)
            {
                throw new ArgumentException("event is not of kind 4", nameof(nostrEvent));
            }

            var receiverPubKeyStr = nostrEvent.Tags.Find(tag => tag.TagIdentifier == "p")?.Data?.First();
            if (receiverPubKeyStr is null)
            {
                throw new ArgumentException("event did not specify a receiver pub key", nameof(nostrEvent));
            }

            var ourPubKey = key.CreateXOnlyPubKey();
            if (nostrEvent.PublicKey == null)
            {
                nostrEvent.PublicKey = ourPubKey.ToBytes().ToHex();
            }
            else if (nostrEvent.PublicKey != ourPubKey.ToBytes().ToHex())
            {
                throw new ArgumentException("key does not match sender of this event", nameof(key));
            }
            var receiverPubKey = Context.Instance.CreateXOnlyPubKey(receiverPubKeyStr.DecodHexData());
            var sharedKey = GetSharedPubkey(receiverPubKey, key).ToBytes().Skip(1).ToArray();

            var (cipherText, iv) = await Encryptor.Encrypt(nostrEvent.Content, sharedKey);
            nostrEvent.Content = $"{cipherText}?iv={iv}";
        }

        private static ECPubKey? GetSharedPubkey(this ECXOnlyPubKey ecxOnlyPubKey, ECPrivKey key)
        {
            Context.Instance.TryCreatePubKey(posBytes.Concat(ecxOnlyPubKey.ToBytes()).ToArray(), out var mPubKey);
            return mPubKey?.GetSharedPubkey(key);
        }
    }

    public static class NostrExtensions
    {
        public static string ComputeId(this NostrEvent nostrEvent)
        {
            return nostrEvent.ToJson(true).ComputeSha256Hash().ToHex();
        }

        public static async Task ComputeIdAndSign(this NostrEvent nostrEvent, ECPrivKey priv, bool handlenip4 = true)
        {
            if (handlenip4 && nostrEvent.Kind == 4)
            {
                await nostrEvent.EncryptNip04Event(priv);
            }
            nostrEvent.Id = nostrEvent.ComputeId();
            nostrEvent.Signature = nostrEvent.ComputeSignature(priv);
        }

        public static string ComputeSignature(this NostrEvent nostrEvent, ECPrivKey priv)
        {
            return nostrEvent.ToJson(true).ComputeSignature(priv);
        }

        public static IQueryable<NostrEvent> Filter(this IQueryable<NostrEvent> events, bool includeDeleted,
            NostrSubscriptionFilter filter)
        {
            var filterQuery = events;
            if (!includeDeleted)
            {
                filterQuery = filterQuery.Where(e => !e.Deleted);
            }

            if (filter.Ids?.Any() is true)
            {
                filterQuery = filterQuery.Where(filter.Ids.Aggregate(PredicateBuilder.New<NostrEvent>(),
                    (current, temp) => current.Or(p => p.Id.StartsWith(temp))));
            }

            if (filter.Kinds?.Any() is true)
            {
                filterQuery = filterQuery.Where(e => filter.Kinds.Contains(e.Kind));
            }

            if (filter.Since != null)
            {
                filterQuery = filterQuery.Where(e => e.CreatedAt > filter.Since);
            }

            if (filter.Until != null)
            {
                filterQuery = filterQuery.Where(e => e.CreatedAt < filter.Until);
            }

            var authors = filter.Authors?.Where(s => !string.IsNullOrEmpty(s))?.ToArray();
            if (authors?.Any() is true)
            {
                filterQuery = filterQuery.Where(authors.Aggregate(PredicateBuilder.New<NostrEvent>(),
                    (current, temp) => current.Or(p => p.PublicKey.StartsWith(temp))));
            }

            if (filter.EventId?.Any() is true)
            {
                filterQuery = filterQuery.Where(e =>
                    e.Tags.Any(tag => tag.TagIdentifier == "e" && filter.EventId.Contains(tag.Data[0])));
            }

            if (filter.PublicKey?.Any() is true)
            {
                filterQuery = filterQuery.Where(e =>
                    e.Tags.Any(tag => tag.TagIdentifier == "p" && filter.PublicKey.Contains(tag.Data[0])));
            }

            var tagFilters = filter.GetAdditionalTagFilters();
            filterQuery = tagFilters.Where(tagFilter => tagFilter.Value.Any()).Aggregate(filterQuery,
                (current, tagFilter) => current.Where(e =>
                    e.Tags.Any(tag => tag.TagIdentifier == tagFilter.Key && tagFilter.Value.Equals(tag.Data[1]))));

            if (filter.Limit is not null)
            {
                filterQuery = filterQuery.OrderBy(e => e.CreatedAt).TakeLast(filter.Limit.Value);
            }
            return filterQuery;
        }

        public static IEnumerable<NostrEvent> Filter(this IEnumerable<NostrEvent> events, bool includeDeleted,
            NostrSubscriptionFilter filter)
        {
            var filterQuery = events;
            if (!includeDeleted)
            {
                filterQuery = filterQuery.Where(e => !e.Deleted);
            }

            if (filter.Ids?.Any() is true)
            {
                filterQuery = filterQuery.Where(e => filter.Ids.Any(s => e.Id.StartsWith(s)));
            }

            if (filter.Kinds?.Any() is true)
            {
                filterQuery = filterQuery.Where(e => filter.Kinds.Contains(e.Kind));
            }

            if (filter.Since != null)
            {
                filterQuery = filterQuery.Where(e => e.CreatedAt > filter.Since);
            }

            if (filter.Until != null)
            {
                filterQuery = filterQuery.Where(e => e.CreatedAt < filter.Until);
            }

            var authors = filter.Authors?.Where(s => !string.IsNullOrEmpty(s))?.ToArray();
            if (authors?.Any() is true)
            {
                filterQuery = filterQuery.Where(e => authors.Any(s => e.PublicKey.StartsWith(s)));
            }

            if (filter.EventId?.Any() is true)
            {
                filterQuery = filterQuery.Where(e =>
                    e.Tags.Any(tag => tag.TagIdentifier == "e" && filter.EventId.Contains(tag.Data[0])));
            }

            if (filter.PublicKey?.Any() is true)
            {
                filterQuery = filterQuery.Where(e =>
                    e.Tags.Any(tag => tag.TagIdentifier == "p" && filter.PublicKey.Contains(tag.Data[0])));
            }

            var tagFilters = filter.GetAdditionalTagFilters();
            return tagFilters.Where(tagFilter => tagFilter.Value.Any()).Aggregate(filterQuery,
                (current, tagFilter) => current.Where(e =>
                    e.Tags.Any(tag =>
                        tag.TagIdentifier == tagFilter.Key && tagFilter.Value.Contains(tag.Data[0]))));
        }

        public static IEnumerable<NostrEvent> FilterByLimit(this IEnumerable<NostrEvent> events, int? limitFilter)
        {
            return limitFilter is not null ? events.OrderBy(e => e.CreatedAt).TakeLast(limitFilter.Value) : events;
        }

        public static ECXOnlyPubKey GetPublicKey(this NostrEvent nostrEvent)
        {
            return ParsePubKey(nostrEvent.PublicKey);
        }

        public static string[] GetTaggedData(this NostrEvent e, string identifier)
        {
            return e.Tags.Where(tag => tag.TagIdentifier == identifier).Select(tag => tag.Data[0]).ToArray();
        }

        public static string[] GetTaggedEvents(this NostrEvent e)
        {
            return e.GetTaggedData("e");
        }

        public static ECPrivKey ParseKey(this string key)
        {
            return ECPrivKey.Create(key.DecodHexData());
        }

        public static ECXOnlyPubKey ParsePubKey(this string key)
        {
            return Context.Instance.CreateXOnlyPubKey(key.DecodHexData());
        }

        public static string ToHex(this ECPrivKey key)
        {
            Span<byte> output = new(new byte[32]);
            key.WriteToSpan(output);
            return output.ToHex();
        }

        public static string ToHex(this ECXOnlyPubKey key)
        {
            return key.ToBytes().ToHex();
        }

        public static string ToJson(this NostrEvent nostrEvent, bool withoutId)
        {
            return
                $"[{(withoutId ? 0 : $"\"{nostrEvent.Id}\"")},\"{nostrEvent.PublicKey}\",{nostrEvent.CreatedAt?.ToUnixTimeSeconds()},{nostrEvent.Kind},[{string.Join(',', nostrEvent.Tags.Select(tag => tag))}],\"{nostrEvent.Content}\"]";
        }

        public static bool Verify(this NostrEvent nostrEvent)
        {
            var hash = nostrEvent.ToJson(true).ComputeSha256Hash();
            if (hash.ToHex() != nostrEvent.Id)
            {
                return false;
            }

            var pub = nostrEvent.GetPublicKey();
            if (!SecpSchnorrSignature.TryCreate(nostrEvent.Signature.DecodHexData(), out var sig))
            {
                return false;
            }

            return pub.SigVerifyBIP340(sig, hash);
        }
    }

    public static class StringExtensions
    {
        public static byte[] ComputeSha256Hash(this string rawData)
        {
            // Create a SHA256
            using var sha256Hash = System.Security.Cryptography.SHA256.Create();
            // ComputeHash - returns byte array
            return sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        }

        public static string ComputeSignature(this string rawData, ECPrivKey privKey)
        {
            var bytes = rawData.ComputeSha256Hash();
            var buf = new byte[64];
            privKey.SignBIP340(bytes).WriteToSpan(buf);
            return buf.ToHex();
        }

        public static byte[] DecodHexData(this string encoded)
        {
            if (encoded == null)
                throw new ArgumentNullException(nameof(encoded));
            if (encoded.Length % 2 == 1)
                throw new FormatException("Invalid Hex String");

            var result = new byte[encoded.Length / 2];
            for (int i = 0, j = 0; i < encoded.Length; i += 2, j++)
            {
                var a = IsDigit(encoded[i]);
                var b = IsDigit(encoded[i + 1]);
                if (a == -1 || b == -1)
                    throw new FormatException("Invalid Hex String");
                result[j] = (byte)(((uint)a << 4) | (uint)b);
            }

            return result;
        }

        public static int IsDigit(this char c)
        {
            if ('0' <= c && c <= '9')
            {
                return c - '0';
            }
            else if ('a' <= c && c <= 'f')
            {
                return c - 'a' + 10;
            }
            else if ('A' <= c && c <= 'F')
            {
                return c - 'A' + 10;
            }
            else
            {
                return -1;
            }
        }

        public static string ToHex(this byte[] bytes)
        {
            var builder = new StringBuilder();
            foreach (var t in bytes)
            {
                builder.Append(t.ToHex());
            }

            return builder.ToString();
        }

        public static string ToHex(this Span<byte> bytes)
        {
            var builder = new StringBuilder();
            foreach (var t in bytes)
            {
                builder.Append(t.ToHex());
            }

            return builder.ToString();
        }

        private static string ToHex(this byte b)
        {
            return b.ToString("x2");
        }
    }

    public static class WebSocketExtensions
    {
        public static async Task SendMessageAsync(this WebSocket socket, string message, CancellationToken cancellationToken)
        {
            if (socket.State != WebSocketState.Open)
                return;
            var buffer = Encoding.UTF8.GetBytes(message);
            await socket.SendAsync(new Memory<byte>(buffer), WebSocketMessageType.Text, true, cancellationToken);
        }
    }
}