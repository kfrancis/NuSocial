using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace NuSocial.Models
{
    public enum RelayType
    {
        Regular,
        Ephemeral,
        NostrWalletConnect
    }

    public class Relay
    {
        public Relay():this("wss://")
        {
        }

        public Relay(string address, bool canRead = true, bool canWrite = true)
        {
            Address = address;
            CanRead = canRead;
            CanWrite = canWrite;
        }

        [Ignore]
        public Uri? Uri
        {
            get
            {
                if (!string.IsNullOrEmpty(Address))
                {
                    return new Uri(Address);
                }
                else return null;
            }
        }

        [Ignore]
        public bool IsEphemeral => RelayType == RelayType.Ephemeral;

        [PrimaryKey]
        public string Address { get; set; }

        public RelayType RelayType { get; set; } = RelayType.Regular;

        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
    }

    public class Post
    {
        [Ignore]
        public Contact Contact { get; set; }

        public string ContactId => Contact.PublicKey;

        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }

        [PrimaryKey]
        public string Hash { get; set; }

        public void ComputeHash()
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(Content));
            var builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
            }
            Hash = builder.ToString();
        }
    }

    public class MerkleNode
    {
        public string Hash { get; set; }
        public MerkleNode Left { get; set; }
        public MerkleNode Right { get; set; }
    }
}
