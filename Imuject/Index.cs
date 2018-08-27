using System;
using System.IO;

namespace Imuject
{
    public class Index : BaseIndex<long>
    {
        public Index()
            : base(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "chain.index"))
        {
        }
    }
}
