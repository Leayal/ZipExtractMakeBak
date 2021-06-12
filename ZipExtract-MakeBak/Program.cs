using System;
using SharpCompress.Archives.Zip;
using SharpCompress.Writers.Zip;
using SharpCompress.Readers;
using SharpCompress.Common;
using SharpCompress.IO;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;

namespace ZipExtractMakeBak
{
    class Program
    {
        static void Main(string[] args)
        {
#if NETFRAMEWORK
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
#endif
            try
            {
                var worker = new Worker();
                worker.Main(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
            }
#if NETFRAMEWORK
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            }
#endif
        }

#if NETFRAMEWORK
        private static Dictionary<string, Assembly> cachedDlls = new Dictionary<string, Assembly>(StringComparer.InvariantCultureIgnoreCase);
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var filename = GetAssemblyName(in args);
            if (cachedDlls.TryGetValue(filename, out var lib))
            {
                return lib;
            }
            else
            {
                var entry = Assembly.GetEntryAssembly();
                using (var contentStream = entry.GetManifestResourceStream($"ZipExtractMakeBak.{filename}.dll"))
                {
                    if (contentStream == null)
                    {
                        return null;
                    }
                    var bytes = new byte[contentStream.Length];
                    if (contentStream.Read(bytes, 0, bytes.Length) == bytes.Length)
                    {
                        var assembly = Assembly.Load(bytes);

                        cachedDlls.Add(filename, assembly);

                        return assembly;
                    }
                }
            }

            return null;
        }

        private static string GetAssemblyName(in ResolveEventArgs args)
        {
            String name;
            if (args.Name.IndexOf(",") > -1)
            {
                name = args.Name.Substring(0, args.Name.IndexOf(","));
            }
            else
            {
                name = args.Name;
            }
            return name;
        }
#endif
    }
}
