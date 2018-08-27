using System;
using System.IO;

namespace Imuject
{
    public class Index : IDisposable
    {
        private FileStream _indexStream;

        private object _indexStreamLock = new object();

        private const int LongSize = sizeof(long);

        public Index()
        {
            _indexStream = new FileStream(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "chain.index"), FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }

        public long? GetPositionForIndex(int index)
        {
            long? location = null;
            lock (_indexStreamLock)
            {
                long loc = LongSize * index;
                if (_indexStream.Length > loc)
                {
                    _indexStream.Position = loc;
                    byte[] data = new byte[LongSize];
                    _indexStream.Read(data, 0, data.Length);
                    location = BitConverter.ToInt64(data, 0);
                }
            }
            return location;
        }

        public void AddPositionToIndex(int index, long location)
        {
            byte[] data = BitConverter.GetBytes(location);
            lock (_indexStreamLock)
            {
                _indexStream.Position = LongSize * index;
                _indexStream.Write(data, 0, data.Length);
            }
        }

        public int Count()
        {
            int count = 0;
            lock (_indexStreamLock)
            {
                count = (int)(_indexStream.Length / LongSize);
            }
            return count;
        }

        public int Last()
        {
            return Count() - 1;
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
