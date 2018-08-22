using System;
using MarcelloDB.Platform;
using MarcelloDB.Storage;

namespace Imuject
{
    public class Platform : IPlatform
    {
        public IStorageStreamProvider CreateStorageStreamProvider(string rootPath)
        {
            return new FileStorageStreamProvider(rootPath);
        }
    }
}
