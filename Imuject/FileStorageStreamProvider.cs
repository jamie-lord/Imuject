using System;
using System.Collections.Generic;
using System.IO;
using MarcelloDB.Storage;

namespace Imuject
{
    public class FileStorageStreamProvider : IStorageStreamProvider
    {
        string RootPath { get; set; }

        Dictionary<string, IStorageStream> Streams { get; set; }

        public FileStorageStreamProvider(string rootPath)
        {
            RootPath = rootPath;
            Streams = new Dictionary<string, IStorageStream>();
        }

        #region IStorageStreamProvider implementation
        public IStorageStream GetStream(string streamName)
        {
            if (!Streams.ContainsKey(streamName))
            {
                Streams.Add(streamName, new FileStorageStream(Path.Combine(RootPath, streamName)));
            }
            return Streams[streamName];
        }
        #endregion

        public void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var stream in Streams.Values)
                {
                    ((FileStorageStream)stream).Dispose();
                }
            }
            GC.SuppressFinalize(this);
        }

        ~FileStorageStreamProvider()
        {
            Dispose(false);
        }
    }

    internal class FileStorageStream : IStorageStream, IDisposable
    {
        FileStream _backingStream;

        internal FileStorageStream(string filePath)
        {
            _backingStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }

        #region IStorageStream implementation
        public byte[] Read(long address, int length)
        {
            byte[] result = new byte[length];
            _backingStream.Seek(address, SeekOrigin.Begin);
            _backingStream.Read(result, 0, length);
            return result;
        }

        public void Write(long address, byte[] bytes)
        {
            _backingStream.Seek(address, SeekOrigin.Begin);
            _backingStream.Write(bytes, 0, bytes.Length);
            _backingStream.Flush(true);
        }
        #endregion

        public void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                _backingStream.Close();
                _backingStream.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        ~FileStorageStream()
        {
            Dispose(false);
        }
    }
}
