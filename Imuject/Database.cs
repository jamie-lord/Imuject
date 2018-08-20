using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;

namespace Imuject
{
    public class Database
    {
        public Database()
        {

        }

        public void Create(string jsonObj)
        {
            var obj = JsonConvert.DeserializeObject(jsonObj);

        }

        public class ImmutableObject
        {
            public ImmutableObject(string json)
            {
                Json = json;
            }

            public string Json { get; }

            public string Hash { get; private set; }

            public DateTime Timestamp { get; private set; }

            public string PreviousHash { get; private set; }

            public int Index { get; private set; }

            public void InsertOp(int index, string previousHash)
            {
                Index = index;
                PreviousHash = previousHash;
                Timestamp = DateTime.Now;
                CalculateHash();
            }

            private void CalculateHash()
            {
                string stringForHashing = Index + PreviousHash + Timestamp + Json;
                SHA256Managed crypt = new SHA256Managed();
                string hash = String.Empty;
                byte[] crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(stringForHashing), 0, Encoding.ASCII.GetByteCount(stringForHashing));
                foreach (byte theByte in crypto)
                {
                    hash += theByte.ToString("x2");
                }
                Hash = hash;
            }
        }

        public class Chain
        {
            private IList<ImmutableObject> _chain = new List<ImmutableObject>();

            public Chain()
            {
                var genesis = new ImmutableObject(string.Empty);
                genesis.InsertOp(0, string.Empty);
                _chain.Add(genesis);
            }

            public void Add(ImmutableObject obj)
            {
                string previousHash = _chain[_chain.Count - 1].Hash;
                obj.InsertOp(_chain.Count, previousHash);
                _chain.Add(obj);
            }
        }
    }
}
