using System;
using System.Security.Cryptography;
using System.Text;
using ZeroFormatter;

namespace Imuject
{
    [ZeroFormattable]
    public class ImmutableObject
    {
        [Index(0)]
        public virtual int Index { get; set; }

        [Index(1)]
        public virtual int Id { get; set; }

        [Index(2)]
        public virtual int Version { get; set; } = -1;

        [Index(3)]
        public virtual string PreviousHash { get; set; }

        [Index(4)]
        public virtual DateTime Timestamp { get; set; }

        [Index(5)]
        public virtual string Hash { get; set; }

        [Index(6)]
        public virtual string Json { get; set; }

        public void InsertOp(int index, string previousHash)
        {
            Index = index;
            PreviousHash = previousHash;
            Timestamp = DateTime.Now.ToUniversalTime();
            // If this is a new object the version number will be -1, incrementing it to 0 signifies this is the first version of this object
            Version++;
            Hash = CalculateHash();
        }

        public string CalculateHash()
        {
            string stringForHashing = Index + Id + Version + PreviousHash + Timestamp + Json;
            SHA256Managed crypt = new SHA256Managed();
            string hash = string.Empty;
            byte[] crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(stringForHashing), 0, Encoding.ASCII.GetByteCount(stringForHashing));
            for (int i = 0; i < crypto.Length; i++)
            {
                hash += crypto[i].ToString("x2");
            }
            return hash;
        }
    }
}
