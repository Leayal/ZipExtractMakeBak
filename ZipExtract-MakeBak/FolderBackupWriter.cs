using System;
using System.IO;

namespace ZipExtractMakeBak
{
    internal class FolderBackupWriter : IBackupWriter
    {
        private readonly string DestinationDirectory;

        public FolderBackupWriter(string directory)
        {
            this.DestinationDirectory = directory;
        }

        public void Dispose() { }

        public void WriteBackup(string relativePath, string filename)
        {
            var fullpath = Path.GetFullPath(Path.Combine(this.DestinationDirectory, relativePath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullpath));
            File.Copy(filename, Path.Combine(this.DestinationDirectory, relativePath), true);
        }
    }
}
