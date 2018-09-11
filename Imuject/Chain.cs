using System;
using System.IO;
using ZeroFormatter;

namespace Imuject
{
    public class Chain : IDisposable
    {
        private FileStream _chainStream;

        private object _chainStreamLock = new object();

        private Index<long> _index;

        private Index<int> _latestVersionIndex;

        public Chain(string dbName)
        {
            _index = new Index<long>(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{dbName}.index"));
            _latestVersionIndex = new Index<int>(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{dbName}.latestVersion.index"));

            _chainStream = new FileStream(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{dbName}.data"), FileMode.OpenOrCreate, FileAccess.ReadWrite);

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

                _index.SetAtIndex(obj.Index, pos);
                _latestVersionIndex.SetAtIndex(obj.Id, obj.Index);

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
            long? beginning = _index.GetAtIndex(index);
            if (!beginning.HasValue)
            {
                throw new Exception("Object not in index");
            }

            long? end = _index.GetAtIndex(index + 1);
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
            // If the object is new then we need to set the id to the next unique id available
            if (obj.Version == -1)
            {
                obj.Id = _latestVersionIndex.Last() + 1;
            }
            obj.InsertOp(previousObject.Index + 1, previousObject.Hash);

            WriteObject(obj);

            return obj.Id;
        }

        /// <summary>
        /// Count of unique object Ids.
        /// Does not include the genesis object.
        /// </summary>
        public int UniqueObjectCount
        {
            get
            {
                return _latestVersionIndex.Count() - 1;
            }
        }

        private bool Any
        {
            get
            {
                return _index.Count() > 0 ? true : false;
            }
        }

        public ImmutableObject LatestVersion(int id)
        {
            int? index = _latestVersionIndex.GetAtIndex(id);
            if (index.HasValue)
            {
                return ReadObject(index.Value);
            }
            return null;
        }

        private ImmutableObject LastObject()
        {
            return ReadObject(_index.Last());
        }

        public bool Validate()
        {
            bool valid = true;
            string previousHash = null;
            int count = _index.Count();
            for (int i = 0; i < count; i++)
            {
                ImmutableObject obj = ReadObject(i);
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
            _index.Dispose();
            _latestVersionIndex.Dispose();
        }
    }
}
