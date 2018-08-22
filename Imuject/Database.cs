using Newtonsoft.Json;
using System;
using System.Security.Cryptography;
using System.Text;
using MarcelloDB;
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

            public int Id { get; set; }

            public int Version { get; set; } = -1;

            public void InsertOp(int index, string previousHash)
            {
                Index = index;
                PreviousHash = previousHash;
                Timestamp = DateTime.Now;
                // If this is a new object the version number will be -1, incrementing it to 0 signifies this is the first version of this object
                Version++;
                Hash = CalculateHash();
            }

            public string CalculateHash()
            {
                string stringForHashing = Index + Version + PreviousHash + Timestamp + Json;
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

        public class Chain : IDisposable
        {
            private Collection<ImmutableObject, string, ObjectIndexDefinition> _chain;

            private readonly Session _session;

            private class ObjectIndexDefinition : IndexDefinition<ImmutableObject>
            {
                public IndexedValue<ImmutableObject, int> Index { get; set; }

                public IndexedValue<ImmutableObject, int> Id { get; set; }
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

            public int Insert(ImmutableObject obj)
            {
                ImmutableObject previousObject = LastObject();
                // If the object is new then we need to set the id to the next unique id available
                if (obj.Version == -1)
                {
                    obj.Id = _chain.Indexes.Id.All.Keys.Max() + 1;
                }
                obj.InsertOp(previousObject.Index + 1, previousObject.Hash);
                _chain.Persist(obj);
                return obj.Id;
            }

            public int UniqueObjectCount
            {
                get
                {
                    return _chain.Indexes.Id.All.Keys.Count();
                }
            }

            public int Count
            {
                get
                {
                    return _chain.All.Count();
                }
            }

            public ImmutableObject LatestVersion(int id)
            {
                return _chain.Indexes.Id.All.Where(x => x.Id == id)?.OrderByDescending(x => x.Version).First();
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
