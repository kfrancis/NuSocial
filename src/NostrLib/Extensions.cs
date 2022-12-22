using NBitcoin.Secp256k1;
using NostrLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NostrLib
{
    internal static class NostrEventExtensions
    {
        public static string ToJson(this INostrEvent<string> nostrEvent, bool withoutId)
        {
            return
                $"[{(withoutId ? 0 : $"\"{nostrEvent.Id}\"")},\"{nostrEvent.PublicKey}\",{nostrEvent.CreatedAt?.ToUnixTimeSeconds()},{nostrEvent.Kind},[{string.Join(',', nostrEvent.Tags.Select(tag => tag))}],\"{nostrEvent.Content}\"]";
        }
        public static ECXOnlyPubKey GetPublicKey(this INostrEvent nostrEvent)
        {
            return ParsePubKey(nostrEvent.PublicKey);
        }

        public static ECPrivKey ParseKey(string key)
        {
            return ECPrivKey.Create(key.DecodHexData());
        }

        public static ECXOnlyPubKey ParsePubKey(string key)
        {
            return Context.Instance.CreateXOnlyPubKey(key.DecodHexData());
        }

        public static bool Verify(this INostrEvent<string> nostrEvent)
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

    internal static class StringExtensions
    {
        public static byte[] DecodHexData(this string encoded)
        {
            if (encoded == null)
                throw new ArgumentNullException(nameof(encoded));
            if (encoded.Length % 2 == 1)
                throw new FormatException("Invalid Hex String");

            var result = new byte[encoded.Length / 2];
            for (int i = 0, j = 0; i < encoded.Length; i += 2, j++)
            {
                var a = CharToDec(encoded[i]);
                var b = CharToDec(encoded[i + 1]);
                if (a == -1 || b == -1)
                    throw new FormatException("Invalid Hex String");
                result[j] = (byte)(((uint)a << 4) | (uint)b);
            }

            return result;
        }

        public static int CharToDec(this char c)
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

        private static string ToHex(this byte b)
        {
            return b.ToString("x2");
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
        public static byte[] ComputeSha256Hash(this string rawData)
        {
            // Create a SHA256   
            using var sha256Hash = System.Security.Cryptography.SHA256.Create();
            // ComputeHash - returns byte array  
            return sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        }

    }
}
