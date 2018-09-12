using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Imuject
{
    public class LinkedIndex<T> : IDisposable
        where T : struct
    {
        private Index<int> _latestObjectIndex;

        private Index<LinkedIndexItem<T>> _objectIndex;

        private object _writeLock = new object();

        public LinkedIndex(string path, string name)
        {
            _latestObjectIndex = new Index<int>(Path.Combine(path, $"{name}.loi"));
            _objectIndex = new Index<LinkedIndexItem<T>>(Path.Combine(path, $"{name}.oi"));
        }

        public void Add(int index, T content)
        {
            lock (_writeLock)
            {
                int? locationOfCurrentVersion = _latestObjectIndex.GetAtIndex(index);

                LinkedIndexItem<T> item = new LinkedIndexItem<T>
                {
                    Content = content
                };

                item.PreviousItem = locationOfCurrentVersion ?? -1;

                int itemIndex = _objectIndex.Last() + 1;
                _objectIndex.SetAtIndex(itemIndex, item.ToArray());

                _latestObjectIndex.SetAtIndex(index, itemIndex);
            }
        }

        public T? GetLatest(int index)
        {
            return Get(index)?.FirstOrDefault();
        }

        public IEnumerable<T> Get(int index)
        {
            int? firstObjectIndex = _latestObjectIndex.GetAtIndex(index);
            if (firstObjectIndex == null)
            {
                yield break;
            }

            int objectIndex = firstObjectIndex.Value;
            while (objectIndex != -1)
            {
                byte[] data = _objectIndex.GetDataAtIndex(objectIndex);

                LinkedIndexItem<T>? item = null;
                if (data != null)
                {
                    item = LinkedIndexItem<T>.FromArray(data);
                }

                if (item != null)
                {
                    objectIndex = item.Value.PreviousItem;
                    yield return item.Value.Content;
                }
                else
                {
                    objectIndex = -1;
                }
            }
        }

        public int Last()
        {
            return _latestObjectIndex.Last();
        }

        public int Count()
        {
            return _latestObjectIndex.Count();
        }

        public void Dispose()
        {
            lock (_writeLock)
            {
                _latestObjectIndex.Dispose();
                _objectIndex.Dispose();
            }
            _latestObjectIndex = null;
            _objectIndex = null;
            _writeLock = null;
        }
    }

    public struct LinkedIndexItem<T>
        where T : struct
    {
        public T Content { get; set; }

        public int PreviousItem { get; set; }

        public byte[] ToArray()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(ConverterHelper.GetBytes(Content));
            writer.Write(PreviousItem);

            return stream.ToArray();
        }

        public static LinkedIndexItem<T> FromArray(byte[] bytes)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(bytes));
            LinkedIndexItem<T> s = default(LinkedIndexItem<T>);

            s.Content = ConverterHelper.FromBytes<T>(reader.ReadBytes(SizeHelper.SizeOf(typeof(T))));
            s.PreviousItem = reader.ReadInt32();

            return s;
        }
    }
}