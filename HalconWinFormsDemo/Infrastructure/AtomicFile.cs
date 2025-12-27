using System;
using System.IO;
using System.Text;

namespace HalconWinFormsDemo.Infrastructure
{
    /// <summary>
    /// Atomic write helper: write to temp file then replace.
    /// Prevents partially-written JSON when power loss/crash occurs.
    /// </summary>
    public static class AtomicFile
    {
        private static readonly object _lock = new object();

        public static void WriteAllTextAtomic(string path, string content, Encoding encoding = null)
        {
            if (encoding == null) encoding = Encoding.UTF8;
            lock (_lock)
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var tmp = path + ".tmp";
                File.WriteAllText(tmp, content, encoding);

                // Replace if exists; otherwise move
                if (File.Exists(path))
                {
                    // File.Replace works on Windows; fallback to move if needed
                    try
                    {
                        File.Replace(tmp, path, null);
                    }
                    catch
                    {
                        File.Delete(path);
                        File.Move(tmp, path);
                    }
                }
                else
                {
                    File.Move(tmp, path);
                }
            }
        }
    }
}
