using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace NostrLib
{
    public class AesEncryptor : IAesEncryptor
    {
        public Task<string> Decrypt(string cipherText, string iv, byte[] key)
        {
            string plainText;
            Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = Convert.FromBase64String(iv);
            aes.Mode = CipherMode.CBC;
            ICryptoTransform decipher = aes.CreateDecryptor(aes.Key, aes.IV);

            using MemoryStream ms = new(Convert.FromBase64String(cipherText));
            using (CryptoStream cs = new(ms, decipher, CryptoStreamMode.Read))
            {
                using StreamReader sr = new(cs);
                plainText = sr.ReadToEnd();
            }

            return Task.FromResult(plainText);
        }

        public Task<(string cipherText, string iv)> Encrypt(string plainText, byte[] key)
        {
            byte[] cipherData;
            Aes aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            ICryptoTransform cipher = aes.CreateEncryptor(aes.Key, aes.IV);

            using (MemoryStream ms = new())
            {
                using (CryptoStream cs = new(ms, cipher, CryptoStreamMode.Write))
                {
                    using StreamWriter sw = new(cs);
                    sw.Write(plainText);
                }

                cipherData = ms.ToArray();
            }

            return Task.FromResult((Convert.ToBase64String(cipherData), Convert.ToBase64String(aes.IV)));
        }
    }
}