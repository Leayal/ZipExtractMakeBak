using System;
using SharpCompress.Writers.Zip;
using SharpCompress.Readers;
using SharpCompress.Common;
using System.IO;
using System.Collections.Generic;

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
            Console.WriteLine("-b [backup zip]");
        }

        public void Main(string[] args)
        {
            string inputZip = null, outputDirectory = null, backupZip = null;

            if (args == null || args.Length == 0)
            {
                ShowHelp();
                return;
            }

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (string.Equals(arg, "-i", StringComparison.InvariantCultureIgnoreCase) || string.Equals(arg, "--input", StringComparison.InvariantCultureIgnoreCase))
                {
                    inputZip = args[++i];
                }
                else if (string.Equals(arg, "-o", StringComparison.InvariantCultureIgnoreCase) || string.Equals(arg, "--output-directory", StringComparison.InvariantCultureIgnoreCase))
                {
                    outputDirectory = args[++i];
                }
                else if (string.Equals(arg, "-b", StringComparison.InvariantCultureIgnoreCase) || string.Equals(arg, "-bak", StringComparison.InvariantCultureIgnoreCase) || string.Equals(arg, "--backup", StringComparison.InvariantCultureIgnoreCase))
                {
                    backupZip = args[++i];
                }
            }

            if (string.IsNullOrWhiteSpace(inputZip) || string.IsNullOrWhiteSpace(outputDirectory))
            {
                ShowHelp();
                return;
            }

            Console.WriteLine($"Input archive file: {inputZip}");
            Console.WriteLine($"Destination directory: {outputDirectory}");

            if (string.IsNullOrWhiteSpace(backupZip))
            {
                Console.Write("Extract without making backup file(s)? [y/N]");
                if (Console.ReadKey().Key != ConsoleKey.Y)
                {
                    return;
                }
            }

            Console.CancelKeyPress += Console_CancelKeyPress;
            Dictionary<string, string> extracted = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            ZipWriter writer = null;
            FileStream fs_bak = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(backupZip))
                {
                    backupZip = Path.GetFullPath(backupZip);
                    fs_bak = File.Create(backupZip);
                    writer = new ZipWriter(fs_bak, new ZipWriterOptions(CompressionType.Deflate) { DeflateCompressionLevel = SharpCompress.Compressors.Deflate.CompressionLevel.BestCompression, LeaveStreamOpen = false, ArchiveComment = "Backup" });
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
                            using (var fs_orig = File.OpenRead(fullpath_ofFile))
                            {
                                writer.Write(reader.Entry.Key, fs_orig, File.GetLastWriteTime(fs_orig.Name));
                            }
                        }
                        if (cancel)
                        {
                            break;
                        }
                        Console.WriteLine($"Extract patching file: {reader.Entry.Key}");

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
                    Console.WriteLine($"All patch files applied. Backup saved to: {backupZip}");
                }
            }
            else
            {
                Console.WriteLine($"Cancelled. Delete all extracted patch files and unused backup files.");
                File.Delete(backupZip);
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
