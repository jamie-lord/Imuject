using System;
using System.IO;

namespace Imuject
{
    public class VersionIndex : BaseIndex<int>
    {
        public VersionIndex(string dbName)
            : base(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{dbName}.version.index"))
        {
        }
    }
}
