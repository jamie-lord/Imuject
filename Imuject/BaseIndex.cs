using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Imuject
{
    public abstract class BaseIndex<T> : IDisposable
        where T : struct
    {
        private FileStream _stream;

        private object _streamLock = new object();

        private readonly int _typeSize;

        public BaseIndex(string path)
        {
            _typeSize = SizeHelper.SizeOf(typeof(T));
            _stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }

        public T? GetAtIndex(int index)
        {
            T? obj = null;
            lock (_streamLock)
            {
                long loc = _typeSize * index;
                if (_stream.Length > loc)
                {
                    _stream.Position = loc;
                    byte[] data = new byte[_typeSize];
                    _stream.Read(data, 0, data.Length);
                    obj = ConverterHelper.FromBytes<T>(data);
                }
            }
            return obj;
        }

        public void SetAtIndex(int index, T obj)
        {
            byte[] data = ConverterHelper.GetBytes(obj);
            lock (_streamLock)
            {
                _stream.Position = _typeSize * index;
                _stream.Write(data, 0, data.Length);
            }
        }

        public int Count()
        {
            int count = 0;
            lock (_streamLock)
            {
                count = (int)(_stream.Length / _typeSize);
            }
            return count;
        }

        public int Last()
        {
            return Count() - 1;
        }

        public void Dispose()
        {
            lock (_streamLock)
            {
                _stream.Flush();
                _stream.Close();
            }
            _stream = null;
            _streamLock = null;
        }
    }

    public static class SizeHelper
    {
        private static readonly Dictionary<Type, int> sizes = new Dictionary<Type, int>();

        public static int SizeOf(Type type)
        {
            if (sizes.TryGetValue(type, out int size))
            {
                return size;
            }

            size = SizeOfType(type);
            sizes.Add(type, size);
            return size;
        }

        private static int SizeOfType(Type type)
        {
            var dm = new DynamicMethod("SizeOfType", typeof(int), new Type[] { });
            ILGenerator il = dm.GetILGenerator();
            il.Emit(OpCodes.Sizeof, type);
            il.Emit(OpCodes.Ret);
            return (int)dm.Invoke(null, null);
        }
    }

    public static class ConverterHelper
    {
        public static byte[] GetBytes(object str)
        {
            int size = SizeHelper.SizeOf(str.GetType());
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        public static T FromBytes<T>(byte[] arr)
            where T : struct
        {
            T str = new T();

            int size = SizeHelper.SizeOf(typeof(T));
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(arr, 0, ptr, size);

            str = (T)Marshal.PtrToStructure(ptr, str.GetType());
            Marshal.FreeHGlobal(ptr);

            return str;
        }
    }
}
