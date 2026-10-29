using System;
using System.IO;
using Loudenvier.Utils.IO;

namespace Loudenvier.FastFileEnumeratorTests.LegacySmoke
{
    internal static class Program
    {
        private static void Main()
        {
            string root = Path.Combine(Path.GetTempPath(), "Loudenvier.FFE.Legacy." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                Directory.CreateDirectory(Path.Combine(root, "child"));
                File.WriteAllText(Path.Combine(root, "one.txt"), "123");
                File.WriteAllText(Path.Combine(root, "child", "two.txt"), "45678");

                foreach (FastEnumerationBackend backend in new[] {
                    FastEnumerationBackend.Auto, FastEnumerationBackend.WindowsNt,
                    FastEnumerationBackend.Portable })
                {
                    long size = 0;
                    int files = 0, dirs = 0;
                    global::Loudenvier.Utils.IO.FastFileEnumerator.Enumerate(root, (in FastFileEntry e) =>
                    {
                        if (e.IsDirectory) dirs++;
                        else { files++; size += e.Length; }
                        if (e.LastWriteTimeUtc.Kind != DateTimeKind.Utc)
                            throw new Exception("Non-UTC date");
                    }, new FastEnumerationOptions { Backend = backend });
                    if (dirs != 1 || files != 2 || size != 8)
                        throw new Exception("Invalid results for " + backend);
                    Console.WriteLine("PASS .NET Framework 4.7.2: " + backend);
                }
            }
            finally { Directory.Delete(root, true); }
        }
    }
}
