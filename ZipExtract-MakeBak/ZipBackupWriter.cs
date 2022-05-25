using SharpCompress.Writers.Zip;
using System;
using System.IO;

namespace ZipExtractMakeBak
{
    class ZipBackupWriter : ZipWriter, IBackupWriter
    {
        public ZipBackupWriter(FileStream destination, ZipWriterOptions zipWriterOptions) : base(destination, zipWriterOptions) { }

        public void WriteBackup(string relativePath, string filename)
        {
            using (var fs_orig = File.OpenRead(filename))
            {
                this.Write(relativePath, fs_orig, File.GetLastWriteTime(fs_orig.Name));
            }
        }
    }
}
