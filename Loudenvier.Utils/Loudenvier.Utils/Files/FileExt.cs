//using SpookilySharp;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Loudenvier.Utils
{
    /// <summary>
    /// Some helpful file extension methods to avoid common perfomance and correctness pitfalls when dealing with
    /// the file system and some 
    /// </summary>
    public static class FileExt
    {
        /// <summary>
        /// Async version of <see cref="File.WriteAllBytes(string, byte[])"/> which allows auto-creation of the directory
        /// structure (<paramref name="forceDirs"/>) and customization of <paramref name="bufferSize"/> (use with care!)
        /// </summary>
        /// <param name="bytes">An array of bytes holding the data to be written to the file</param>
        /// <param name="path">The destination file path</param>
        /// <param name="forceDirs">If true will try to create the directory strucure for the given <paramref name="path"/></param>
        /// <param name="bufferSize">Changes the default buffer size. Use with care as the default of 4096 bytes is well
        /// tested and performs very good accross OS'es and architectures.</param>
        /// <returns></returns>
        public static async Task WriteAllBytesAsync(this byte[] bytes, string path, bool forceDirs = true, int bufferSize = 4096) {
            EnsureDirectoryExists(Path.GetDirectoryName(path));
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize, FileOptions.None);
            await fs.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        }

        /// <summary>
        /// Extension method for <see cref="byte[]"/> which allows auto-creationg of the directory structure. Calls 
        /// <see cref="File.WriteAllBytes(string, byte[])"/> to actually perform the writing.
        /// </summary>
        /// <param name="bytes">An array of bytes holding the data to be written to the file</param>
        /// <param name="path">The destination file path</param>
        /// <param name="forceDirs">If true will try to create the directory strucure for the given <paramref name="path"/></param>
        public static void WriteAllBytes(this byte[] bytes, string path, bool forceDirs = true) {
            if (forceDirs)
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, bytes);
        }

        /// <summary>Reads all bytes from the given <paramref name="stream"/>. No length checking is done. 
        /// Callers must take care to prevent Out of Memory scenarios.</summary>
        /// <remarks>Differently from <see cref="File.ReadAllBytes(string)"/> this method does not check 
        /// for data lengths bigger than <see cref="int.MaxValue"/> and it also does not throw if the end 
        /// of file is reached before length bytes are read</remarks>
        /// <param name="stream">The stream to read data from</param>
        /// <returns>A byte array with all the data on the stream</returns>
        public static async Task<byte[]> ReadAllBytesAsync(this Stream stream) {
            var buffer = new byte[stream.Length];
            int bytesRead, pos = 0;
            do {
                bytesRead = await stream.ReadAsync(buffer, pos, buffer.Length - pos).ConfigureAwait(false);
                pos += bytesRead;
            } while (bytesRead > 0 || pos < buffer.Length);
            return buffer;
        }

        /// <summary>Reads all bytes from the file pointed at by <paramref name="path"/>. No file length checking is done. 
        /// Callers must take care to prevent Out of Memory scenarios.</summary>
        /// <param name="path">The file to read all bytes from</param>
        /// <returns>A byte array with all the data on the file pointed at by <paramref name="path"/></returns>
        /// <exception cref="FileNotFoundException">If the file does not exist</exception>
        public static async Task<byte[]> ReadAllBytesAsync(this string path) {
            if (!File.Exists(path))
                throw new FileNotFoundException($"File not found: {path}", path);
            using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.None);
            return await fs.ReadAllBytesAsync().ConfigureAwait(false);
        }
        
        public static string? GetFolderAtDepth(this string filename, int depth=3) {
            if (filename == null) return null;
            if (depth < 1)
                throw new ArgumentOutOfRangeException("depth", "depth deve ser maior do que 0");
            var sb = new StringBuilder();
            for (int i = 0; i < depth; i++) 
                sb.Append(i < filename.Length ? filename[i] : '0').Append('\\');
            return sb.ToString();
        } 

        /*public static string GetHashedFileName(this byte[] fileBytes) 
            => fileBytes.SpookyHash128().ToString();*/

        /// <summary>
        /// Guarantees that the directory pointed at by <paramref name="folderName"/> exists, creating it if needed.
        /// </summary>
        /// <param name="folderName">The directory path</param>
        public static void EnsureDirectoryExists(this string folderName) {
            if (!Directory.Exists(folderName))
                Directory.CreateDirectory(folderName);
        }

        /// <summary>
        /// Changes the directory part of a full file path (<paramref name="fileName"/>) to a new path (<paramref name="newPath"/>)
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="fileName">The original full file path and name</param>
        /// <param name="newPath">The new file path for the given file name</param>
        /// <returns>The file name with the path part changed to the new path</returns>
        public static string ChangeFileDir(this string fileName, string newPath) 
            => Path.Combine(newPath, Path.GetFileName(fileName));
    }
}
