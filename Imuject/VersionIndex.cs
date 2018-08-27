using System;
using System.IO;

namespace Imuject
{
    public class VersionIndex : BaseIndex<int>
    {
        public VersionIndex()
            : base(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "chain.version.index"))
        {
        }
    }
}
