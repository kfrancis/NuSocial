using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace NostrLib
{
    public interface IAesEncryptor
    {
        Task<SecureString> Decrypt(byte[] key, string iv, SecureString content);

        Task<(string cipherText, string iv)> Encrypt(SecureString plainText, byte[] key);
    }

    public class AesEncryptor : IAesEncryptor
    {
        public async Task<SecureString> Decrypt(byte[] key, string iv, SecureString content)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            //
            // decrypt the data
            //
            //
            // prepare the crypto stuff
            //
            using var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.IV = Convert.FromBase64String(iv);
            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var encryptedStream = new MemoryStream(Convert.FromBase64String(content.ToPlainString()));
            using var targetStream = new MemoryStream();

            //
            // decrypt the data and return as SecureString
            //
            using (var sourceStream = new CryptoStream(encryptedStream, decryptor, CryptoStreamMode.Read))
            {
                await sourceStream.CopyToAsync(targetStream);
            }

            var decryptedData = targetStream.ToArray();
            try
            {
                return New(decryptedData);
            }
            finally
            {
                Array.Clear(decryptedData, 0, decryptedData.Length);
            }
        }

        public async Task<(string cipherText, string iv)> Encrypt(SecureString plainText, byte[] key)
        {
            if (plainText is null)
            {
                throw new ArgumentNullException(nameof(plainText));
            }

            //
            // get clear text data from the input SecureString
            //
            var cipherData = GetData(plainText);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;

            try
            {
                using var encryptor = aes.CreateEncryptor();
                using var sourceStream = new MemoryStream(cipherData);
                using var encryptedStream = new MemoryStream();

                //
                // encrypt it
                //
                using (var cryptoStream = new CryptoStream(encryptedStream, encryptor, CryptoStreamMode.Write))
                {
                    await sourceStream.CopyToAsync(cryptoStream);
                }

                //
                // return encrypted data
                //
                var encryptedData = encryptedStream.ToArray();
                return (Convert.ToBase64String(encryptedData), Convert.ToBase64String(aes.IV));
            }
            finally
            {
                Array.Clear(cipherData, 0, cipherData.Length);
            }
        }

        internal static byte[] GetData(SecureString s)
        {
            //
            // each unicode char is 2 bytes.
            //
            var data = new byte[s.Length * 2];

            if (s.Length > 0)
            {
                var ptr = Marshal.SecureStringToCoTaskMemUnicode(s);

                try
                {
                    Marshal.Copy(ptr, data, 0, data.Length);
                }
                finally
                {
                    Marshal.ZeroFreeCoTaskMemUnicode(ptr);
                }
            }

            return data;
        }

        /// <summary>
        /// <para>Create a new SecureString based on the specified binary data.</para>
        /// <para>
        /// The binary data must be byte[] version of unicode char[],
        /// otherwise the results are unpredictable.
        /// </para>
        /// </summary>
        /// <param name="data">Input data.</param>
        /// <returns>A SecureString .</returns>
        private static SecureString New(byte[] data)
        {
            if ((data.Length % 2) != 0)
            {
                // If the data is not an even length, they supplied an invalid key
                throw new InvalidDataException("Invalid key");
            }

            char ch;
            var ss = new SecureString();

            //
            // each unicode char is 2 bytes.
            //
            var len = data.Length / 2;

            for (var i = 0; i < len; i++)
            {
                ch = (char)((data[(2 * i) + 1] * 256) + data[2 * i]);
                ss.AppendChar(ch);

                //
                // zero out the data slots as soon as we use them
                //
                data[2 * i] = 0;
                data[(2 * i) + 1] = 0;
            }

            return ss;
        }
    }
}
