using System;
using System.Collections.Generic;
using System.IO;

namespace Imuject
{
    public class Index : IDisposable
    {
        private FileStream _indexStream;

        private object _indexStreamLock = new object();

        public Index()
        {
            _indexStream = new FileStream(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "chain.index"), FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }

        public long? GetPositionForIndex(int index)
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

        public void AddPositionToIndex(int index, long location)
        {
            lock (_indexStreamLock)
            {
                _indexStream.Position = sizeof(long) * index;
                byte[] data = BitConverter.GetBytes(location);
                _indexStream.Write(data, 0, data.Length);
            }
        }

        public int Count()
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

        public int Last()
        {
            return Count() - 1;
        }

        public IEnumerable<(int, long)> Enumerable()
        {
            int count = Count();
            for (int i = 0; i < count; i++)
            {
                yield return (i, GetPositionForIndex(i).Value);
            }
        }

        public void Dispose()
        {
            lock (_indexStreamLock)
            {
                _indexStream.Flush();
                _indexStream.Close();
            }
        }
    }
}
