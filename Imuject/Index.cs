using System;
using System.IO;

namespace Imuject
{
    public class Index : BaseIndex<long>
    {
        public Index(string dbName)
            : base(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{dbName}.index"))
        {
        }
    }
}
