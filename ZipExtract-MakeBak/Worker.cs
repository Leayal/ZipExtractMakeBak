using System;
using SharpCompress.Writers.Zip;
using SharpCompress.Readers;
using SharpCompress.Common;
using System.IO;
using System.Collections.Generic;
using SharpCompress.Writers;
using System.Text;

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
            Console.WriteLine(" -i [input archive (7z, zip or RAR) or a directory path] (Required)");
            Console.WriteLine(" -o [output path or directory] (Required)");
            Console.WriteLine(" -b [backup destination path] (Optional)");
#if !NETFRAMEWORK
            Console.WriteLine(" -d [The maximum recursive depth to enumerate for files in the input directory and its subdirectories] (Optional. Default 30)");
#endif
            Console.WriteLine(" -nozip [The backup files should NOT be zipped. The backup files will be files (directory structure preserved) in directory path given by the \"backup destination\" param] (Optional)");
            Console.WriteLine("(The input can be specified multiple time for a bulk operation. E.g: -i first.zip -i \"second one.zip\" -i \"my new folder\")");
        }

        public void Main(string[] args)
        {
            var filenameComparer =
#if NETFRAMEWORK
                StringComparer.OrdinalIgnoreCase
#else
                OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal
#endif
                ;
            var inputSrc = new Dictionary<string, bool>(filenameComparer); // filename + bool of "Is Directory Or File" (false for file, true for directory)
            string outputDirectory = null, backupDst = null;
            bool isOutputZip = true;
            int intputDirectoryRecursiveDepth = 30;

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
                    var filename = args[++i];
                    if (File.Exists(filename))
                    {
                        inputSrc.Add(filename, false);
                    }
                    else if (Directory.Exists(filename))
                    {
                        inputSrc.Add(filename, true);
                    }
                }
                else if (string.Equals(arg, "-o", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "--output-directory", StringComparison.OrdinalIgnoreCase))
                {
                    outputDirectory = args[++i];
                }
                else if (string.Equals(arg, "-b", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "-bak", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "--backup", StringComparison.OrdinalIgnoreCase))
                {
                    backupDst = args[++i];
                    isOutputZip = !Directory.Exists(backupDst);
                }
                else if (string.Equals(arg, "-d", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "-depth", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(args[++i], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var _parsedDepth) && _parsedDepth > 0)
                    {
                        intputDirectoryRecursiveDepth = _parsedDepth;
                    }
                }
                else if (string.Equals(arg, "-nozip", StringComparison.OrdinalIgnoreCase))
                {
                    isOutputZip = false;
                }
            }

            if (inputSrc.Count == 0 || string.IsNullOrWhiteSpace(outputDirectory))
            {
                ShowHelp();
                return;
            }

            var sb = new StringBuilder();
            bool firstItemInAppendedString = true;
            foreach (var item in inputSrc.Keys)
            {
                if (firstItemInAppendedString)
                {
                    firstItemInAppendedString = false;
                }
                else
                {
                    sb.Append(", ");
                }
#if NETFRAMEWORK
                sb.Append(Path.GetFileName(item));
#else
                sb.Append(Path.GetFileName(item.AsSpan()));
#endif
            }

            if (sb.Length != 0)
            {
                Console.WriteLine(inputSrc.Count == 1 ? "Input archive file: " : "Input archive files: ");
                Console.WriteLine(sb.ToString());
            }
            Console.WriteLine($"Destination directory: {outputDirectory}");
#if NETFRAMEWORK
            Console.WriteLine("Input Directory Enumberate Maximum Depth: Infinite (May cause app hang if directory has corrupted record or the structure is very wrong)");
#else
            Console.WriteLine($"Input Directory Enumberate Maximum Depth: {intputDirectoryRecursiveDepth}");
#endif

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
                    if (isOutputZip)
                    {
                        fs_bak = File.Create(backupDst);
                        writer = new ZipBackupWriter(fs_bak, new ZipWriterOptions(CompressionType.Deflate) { DeflateCompressionLevel = SharpCompress.Compressors.Deflate.CompressionLevel.BestCompression, LeaveStreamOpen = false, ArchiveComment = "Backup" });
                    }
                    else
                    {
                        writer = new FolderBackupWriter(backupDst);
                    }
                }

                foreach (var item in inputSrc)
                {
                    if (item.Value)
                    {
                        // True = Is Directory
                        var directoryName = item.Key;
                        var directoryNameLengthWithTrailingSlash = directoryName.Length + 1;
#if NETFRAMEWORK
                        foreach (var src_fullFilename in Directory.EnumerateFiles(directoryName, "*",  SearchOption.AllDirectories))
#else
                        foreach (var src_fullFilename in Directory.EnumerateFiles(directoryName, "*", new EnumerationOptions() { IgnoreInaccessible = true, MaxRecursionDepth = intputDirectoryRecursiveDepth, RecurseSubdirectories = true, ReturnSpecialDirectories = false }))
#endif
                        {
                            if (cancel)
                            {
                                break;
                            }

                            string relativePath = src_fullFilename.Remove(0, directoryNameLengthWithTrailingSlash), 
                                fullpath_ofFile = Path.GetFullPath(Path.Combine(outputDirectory, relativePath));
                            if (File.Exists(fullpath_ofFile))
                            {
                                Console.WriteLine($"Backup file: {relativePath}");
                                writer.WriteBackup(relativePath, fullpath_ofFile);
                            }
                            if (cancel)
                            {
                                break;
                            }
                            Console.WriteLine($"Copying patching file: {relativePath}");

                            Directory.CreateDirectory(Path.GetDirectoryName(fullpath_ofFile));
                            var tmpDstFile = fullpath_ofFile + ".patch";
                            RemoveReadOnlyFlagIfNeeded(tmpDstFile);
#if NETFRAMEWORK
                            File.Delete(tmpDstFile);
                            File.Copy(fullpath_ofFile, tmpDstFile);
#else
                            File.Copy(src_fullFilename, tmpDstFile, true);
#endif
                            extracted.Add(fullpath_ofFile, tmpDstFile);
                        }
                    }
                    else
                    {
                        // True = Is a file, so assume it's an archive
                        var inputZip = item.Key;
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
                    RemoveReadOnlyFlagIfNeeded(item.Key);
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
                if (isOutputZip)
                {
                    File.Delete(backupDst);
                }
                foreach (var item in extracted)
                {
                    File.Delete(item.Value);
                }
            }
        }

        private static void RemoveReadOnlyFlagIfNeeded(string path)
        {
            if (File.Exists(path))
            {
                var attr = File.GetAttributes(path);
                if ((attr & FileAttributes.ReadOnly) != 0)
                {
                    File.SetAttributes(path, attr &= ~FileAttributes.ReadOnly);
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
