using System;
using System.Collections.Generic;
using System.IO;
using ZeroFormatter;

namespace Imuject
{
    public class Chain : IDisposable
    {
        private FileStream _chainStream;

        private object _chainStreamLock = new object();

        private FileStream _indexStream;

        private object _indexStreamLock = new object();

        // Maps object ids to object indexes
        private SortedDictionary<int, int> _latestVersions = new SortedDictionary<int, int>();

        public Chain()
        {
            _chainStream = new FileStream(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "chain.data"), FileMode.OpenOrCreate, FileAccess.ReadWrite);
            _indexStream = new FileStream(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "chain.index"), FileMode.OpenOrCreate, FileAccess.ReadWrite);

            if (!Any)
            {
                var genesis = new ImmutableObject() { Json = string.Empty };
                genesis.InsertOp(0, string.Empty);
                WriteObject(genesis);
            }
        }

        private long? GetLocationForIndex(int index)
        {
            long? location = null;
            lock (_indexStreamLock)
            {
                long loc = sizeof(long) * index;
                if (_indexStream.Length > loc)
                {
                    _indexStream.Position = loc;
                    byte[] data = new byte[sizeof(long)];
                    _indexStream.Read(data, 0, data.Length);
                    location = BitConverter.ToInt64(data, 0);
                }
            }
            return location;
        }

        private void AddLocationToIndex(int index, long location)
        {
            lock (_indexStreamLock)
            {
                _indexStream.Position = sizeof(long) * index;
                byte[] data = BitConverter.GetBytes(location);
                _indexStream.Write(data, 0, data.Length);
            }
        }

        private int IndexCount()
        {
            int count = 0;
            lock (_indexStreamLock)
            {
                long length = _indexStream.Length;
                if (length > 0)
                {
                    count = (int)(length / sizeof(long));
                }
            }
            return count;
        }

        private int LastIndex()
        {
            return IndexCount() - 1;
        }

        private IEnumerable<(int, long)> Index()
        {
            int count = IndexCount();
            for (int i = 0; i < count; i++)
            {
                yield return (i, GetLocationForIndex(i).Value);
            }
        }

        private void WriteObject(ImmutableObject obj)
        {
            byte[] data = ZeroFormatterSerializer.Serialize(obj);
            lock (_chainStreamLock)
            {
                long pos = _chainStream.Length;
                _chainStream.Position = pos;

                AddLocationToIndex(obj.Index, pos);
                _latestVersions[obj.Id] = obj.Index;

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
            long? beginning = GetLocationForIndex(index);
            if (!beginning.HasValue)
            {
                throw new Exception("Object not in index");
            }

            long? end = GetLocationForIndex(index + 1);
            if (!end.HasValue)
            {
                lock (_chainStreamLock)
                {
                    end = _chainStream.Length;
                }
            }

            return ReadObject(beginning.Value, end.Value);
        }

        public int Insert(ImmutableObject obj)
        {
            ImmutableObject previousObject = LastObject();
            //// If the object is new then we need to set the id to the next unique id available
            if (obj.Version == -1)
            {
                obj.Id = LastIndex() + 1;
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

        private bool Any
        {
            get
            {
                return IndexCount() > 0 ? true : false;
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
            return ReadObject(LastIndex());
        }

        public bool Validate()
        {
            bool valid = true;
            string previousHash = null;
            foreach ((int, long) item in Index())
            {
                ImmutableObject obj = ReadObject(item.Item1);
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
