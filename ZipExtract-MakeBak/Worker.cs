using System;
using SharpCompress.Writers.Zip;
using SharpCompress.Readers;
using SharpCompress.Common;
using System.IO;
using System.Collections.Generic;
using SharpCompress.Writers;

namespace ZipExtractMakeBak
{
    class Worker
    {
        private bool cancel;

        public Worker()
        {
            this.cancel = false;
        }

        void ShowHelp()
        {
            Console.WriteLine("Usage switches:");
            Console.WriteLine("-i [input archive (7z, zip or RAR)]");
            Console.WriteLine("-o [output path or directory]");
            Console.WriteLine("-b [backup destination path]");
            Console.WriteLine("-nozip");
        }

        public void Main(string[] args)
        {
            string inputZip = null, outputDirectory = null, backupDst = null;
            bool isZip = true;

            if (args == null || args.Length == 0)
            {
                ShowHelp();
                return;
            }

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (string.Equals(arg, "-i", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "--input", StringComparison.OrdinalIgnoreCase))
                {
                    inputZip = args[++i];
                }
                else if (string.Equals(arg, "-o", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "--output-directory", StringComparison.OrdinalIgnoreCase))
                {
                    outputDirectory = args[++i];
                }
                else if (string.Equals(arg, "-b", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "-bak", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "--backup", StringComparison.OrdinalIgnoreCase))
                {
                    backupDst = args[++i];
                    isZip = !Directory.Exists(backupDst);
                }
                else if (string.Equals(arg, "-nozip", StringComparison.OrdinalIgnoreCase))
                {
                    isZip = false;
                }
            }

            if (string.IsNullOrWhiteSpace(inputZip) || string.IsNullOrWhiteSpace(outputDirectory))
            {
                ShowHelp();
                return;
            }

            Console.WriteLine($"Input archive file: {inputZip}");
            Console.WriteLine($"Destination directory: {outputDirectory}");

            if (string.IsNullOrWhiteSpace(backupDst))
            {
                Console.Write("Extract without making backup file(s)? [y/N]");
                if (Console.ReadKey().Key != ConsoleKey.Y)
                {
                    return;
                }
            }

            Console.CancelKeyPress += Console_CancelKeyPress;
            Dictionary<string, string> extracted = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            IBackupWriter writer = null;
            FileStream fs_bak = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(backupDst))
                {
                    backupDst = Path.GetFullPath(backupDst);
                    if (isZip)
                    {
                        fs_bak = File.Create(backupDst);
                        writer = new ZipBackupWriter(fs_bak, new ZipWriterOptions(CompressionType.Deflate) { DeflateCompressionLevel = SharpCompress.Compressors.Deflate.CompressionLevel.BestCompression, LeaveStreamOpen = false, ArchiveComment = "Backup" });
                    }
                    else
                    {
                        writer = new FolderBackupWriter(backupDst);
                    }
                }

                using (var archive = OpenArchive(inputZip))
                using (var reader = archive.ExtractAllEntries())
                {
                    while (reader.MoveToNextEntry())
                    {
                        if (reader.Entry.IsDirectory)
                        {
                            continue;
                        }
                        if (cancel)
                        {
                            break;
                        }

                        var fullpath_ofFile = Path.GetFullPath(Path.Combine(outputDirectory, reader.Entry.Key));
                        if (writer != null && File.Exists(fullpath_ofFile))
                        {
                            Console.WriteLine($"Backup file: {reader.Entry.Key}");
                            writer.WriteBackup(reader.Entry.Key, fullpath_ofFile);
                        }
                        if (cancel)
                        {
                            break;
                        }
                        Console.WriteLine($"Extract patching file: {reader.Entry.Key}");

                        Directory.CreateDirectory(Path.GetDirectoryName(fullpath_ofFile));
                        reader.WriteEntryTo(fullpath_ofFile + ".patch");
                        extracted.Add(fullpath_ofFile, fullpath_ofFile + ".patch");
                    }
                }
            }
            finally
            {
                writer?.Dispose();
                fs_bak?.Dispose();
            }

            if (!cancel)
            {
                Console.WriteLine("Applying all patch files.");
                foreach (var item in extracted)
                {
#if NETFRAMEWORK
                    File.Delete(item.Key);
                    File.Move(item.Value, item.Key);
#else
                        File.Move(item.Value, item.Key, true);
#endif
                }

                if (writer == null)
                {
                    Console.WriteLine("All patch files applied.");
                }
                else
                {
                    Console.WriteLine($"All patch files applied. Backup saved to: {backupDst}");
                }
            }
            else
            {
                Console.WriteLine($"Cancelled. Delete all extracted patch files and unused backup files.");
                if (isZip)
                {
                    File.Delete(backupDst);
                }
                foreach (var item in extracted)
                {
                    File.Delete(item.Value);
                }
            }
        }

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            cancel = true;
        }

        private static SharpCompress.Archives.IArchive OpenArchive(in string inputZip)
        {
            if (SharpCompress.Archives.SevenZip.SevenZipArchive.IsSevenZipFile(inputZip))
            {
                return SharpCompress.Archives.SevenZip.SevenZipArchive.Open(inputZip);
            }
            else if (SharpCompress.Archives.Rar.RarArchive.IsRarFile(inputZip))
            {
                return SharpCompress.Archives.Rar.RarArchive.Open(inputZip);
            }
            else
            {
                return SharpCompress.Archives.ArchiveFactory.Open(inputZip);
            }
        }
    }
}
