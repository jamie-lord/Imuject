using System.Collections.Generic;
using System.IO;

namespace Imuject
{
    public class LinkedIndex<T>
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
                // Get location for previous linked item if it exists

                int? locationOfCurrentVersion = _latestObjectIndex.GetAtIndex(index);

                LinkedIndexItem<T> item = new LinkedIndexItem<T>
                {
                    Content = content
                };

                item.PreviousItem = locationOfCurrentVersion ?? -1;

                // Add the new item to the end of object index

                int itemIndex = _objectIndex.Last() + 1;
                _objectIndex.SetAtIndex(itemIndex, item);

                // Update the latest object index with new pointer to new object

                _latestObjectIndex.SetAtIndex(index, itemIndex);
            }
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
                LinkedIndexItem<T>? item = _objectIndex.GetAtIndex(objectIndex);

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
    }

    public struct LinkedIndexItem<T>
        where T : struct
    {
        public T Content { get; set; }

        public int PreviousItem { get; set; }
    }
}