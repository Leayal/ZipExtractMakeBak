using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZipExtractMakeBak
{
    interface IBackupWriter : IDisposable
    {
        void WriteBackup(string relativePath, string filename);
    }
}
