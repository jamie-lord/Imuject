using Newtonsoft.Json;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using ZeroFormatter;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace Imuject
{
    public class Database
    {
        public void Create(string jsonObj)
        {
            var obj = JsonConvert.DeserializeObject(jsonObj);
        }

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

        public class Chain : IDisposable
        {
            private FileStream _chainStream;

            private object _chainStreamLock = new object();

            private SortedDictionary<int, long> _index = new SortedDictionary<int, long>();

            // Maps object ids to object indexes
            private SortedDictionary<int, int> _latestVersions = new SortedDictionary<int, int>();

            private SortedDictionary<int, (ImmutableObject, long)> _objs = new SortedDictionary<int, (ImmutableObject, long)>();

            public Chain()
            {
                _chainStream = new FileStream(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "chain.data"), FileMode.OpenOrCreate, FileAccess.ReadWrite);
                if (!Any)
                {
                    var genesis = new ImmutableObject() { Json = string.Empty };
                    genesis.InsertOp(0, string.Empty);
                    WriteObject(genesis);
                }
            }

            private void WriteObject(ImmutableObject obj)
            {
                byte[] data = ZeroFormatterSerializer.Serialize(obj);
                lock (_chainStreamLock)
                {
                    long pos = _chainStream.Length;
                    _chainStream.Position = pos;

                    _index.Add(obj.Index, pos);
                    _latestVersions[obj.Id] = obj.Index;
                    _objs.Add(obj.Index, (obj, data.Length));

                    _chainStream.Write(data, 0, data.Length);
                    _chainStream.Flush();
                }
            }

            private ImmutableObject ReadObject(long beginning, long end)
            {
                ImmutableObject obj = null;
                lock (_chainStreamLock)
                {
                    _chainStream.Position = beginning;
                    byte[] data = new byte[end - beginning];
                    _chainStream.Read(data, 0, data.Length);
                    obj = ZeroFormatterSerializer.Deserialize<ImmutableObject>(data);
                }
                return obj;
            }

            private ImmutableObject ReadObject(int index)
            {
                if (!_index.TryGetValue(index, out long beginning))
                {
                    throw new Exception("Object not in index");
                }

                if (!_index.TryGetValue(index + 1, out long end))
                {
                    lock (_chainStreamLock)
                    {
                        end = _chainStream.Length;
                    }
                }

                return ReadObject(beginning, end);
            }

            public int Insert(ImmutableObject obj)
            {
                ImmutableObject previousObject = LastObject();
                //// If the object is new then we need to set the id to the next unique id available
                if (obj.Version == -1)
                {
                    obj.Id = LastIndex + 1;
                }
                obj.InsertOp(previousObject.Index + 1, previousObject.Hash);

                WriteObject(obj);

                return obj.Id;
            }

            public int UniqueObjectCount
            {
                get
                {
                    return _latestVersions.Count;
                }
            }

            public int LastIndex
            {
                get
                {
                    if (_index.Count > 0)
                    {
                        return _index.Last().Key;
                    }
                    return 0;
                }
            }

            private bool Any
            {
                get
                {
                    return _index.Count > 0 ? true : false;
                }
            }

            public int Count
            {
                get
                {
                    return _index.Count;
                }
            }

            public ImmutableObject LatestVersion(int id)
            {
                if (_latestVersions.TryGetValue(id, out int index))
                {
                    return ReadObject(index);
                }
                return null;
            }

            private ImmutableObject LastObject()
            {
                return ReadObject(_index.Last().Key);
            }

            public bool Validate()
            {
                bool valid = true;
                string previousHash = null;
                foreach (KeyValuePair<int, long> item in _index)
                {
                    ImmutableObject obj = ReadObject(item.Key);
                    if (previousHash != null && obj.PreviousHash != previousHash)
                    {
                        valid = false;
                        break;
                    }
                    previousHash = obj.CalculateHash();
                }
                return valid;
            }

            public void Dispose()
            {
                _chainStream.Flush();
                _chainStream.Dispose();
            }
        }
    }
}
