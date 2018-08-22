using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using MarcelloDB;
using MarcelloDB.Platform;
using MarcelloDB.Index;
using MarcelloDB.Collections;
using System.Linq;

namespace Imuject
{
    public class Database
    {
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

            public string Json { get; set; }

            public string Hash { get; set; }

            public DateTime Timestamp { get; set; }

            public string PreviousHash { get; set; }

            public int Index { get; set; }

            public void InsertOp(int index, string previousHash)
            {
                Index = index;
                PreviousHash = previousHash;
                Timestamp = DateTime.Now;
                Hash = CalculateHash();
            }

            public string CalculateHash()
            {
                string stringForHashing = Index + PreviousHash + Timestamp + Json;
                SHA256Managed crypt = new SHA256Managed();
                string hash = String.Empty;
                byte[] crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(stringForHashing), 0, Encoding.ASCII.GetByteCount(stringForHashing));
                foreach (byte theByte in crypto)
                {
                    hash += theByte.ToString("x2");
                }
                return hash;
            }
        }

        public class Chain : IDisposable
        {
            private MarcelloDB.Collections.Collection<ImmutableObject, string, ObjectIndexDefinition> _chain;

            private readonly Session _session;

            private class ObjectIndexDefinition : IndexDefinition<ImmutableObject>
            {
                public IndexedValue<ImmutableObject, int> Index { get; set; }
            }

            public Chain()
            {
                var platform = new Platform();
                _session = new Session(platform, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

                var chainFile = _session["chain.data"];

                _chain = chainFile.Collection<ImmutableObject, string, ObjectIndexDefinition>("objects", x => x.Hash);

                if (!_chain.All.Any())
                {
                    var genesis = new ImmutableObject(string.Empty);
                    genesis.InsertOp(0, string.Empty);

                    _chain.Persist(genesis);
                }
            }

            public void Add(ImmutableObject obj)
            {
                var previousObject = LastObject();
                obj.InsertOp(previousObject.Index + 1, previousObject.Hash);
                _chain.Persist(obj);
            }

            public int Count
            {
                get
                {
                    return _chain.All.Count();
                }
            }

            private ImmutableObject LastObject()
            {
                return _chain.Indexes.Index.All.Descending.First();
            }

            public bool Validate()
            {
                string previousHash = null;
                foreach (var item in _chain.Indexes.Index.All)
                {
                    if (previousHash != null && item.PreviousHash != previousHash)
                    {
                        return false;
                    }
                    previousHash = item.CalculateHash();
                }
                return true;
            }

            public void Dispose()
            {
                _session.Dispose();
            }
        }
    }
}
