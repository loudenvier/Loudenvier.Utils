//using SpookilySharp;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Loudenvier.Utils;

/// <summary>
/// Some helpful file extension methods to avoid common performance and correctness pitfalls when dealing with
/// the file system and file streams.
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
        if (forceDirs)
            EnsureDirectoryExists(Path.GetDirectoryName(path) ?? throw new ArgumentNullException(nameof(path)));
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
            EnsureDirectoryExists(Path.GetDirectoryName(path) ?? throw new ArgumentNullException(nameof(path)));
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
        } while (bytesRead > 0 && pos < buffer.Length);
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
    
    /// <summary>
    /// Returns a string representing a folder structure of <paramref name="depth"/> subfolders
    /// constructed from the given <paramref name="filename"/> by using the first <c>depth</c> 
    /// characters of the <c>filename</c> itself as folder names (or <c>'0'</c> if filename has 
    /// fewer characters than <c>depth</c>).
    /// </summary>
    /// <remarks>This method is useful to segregate files into separate folders given their own
    /// file names, helping to avoid storing all files in a single folder, which is problematic
    /// under Windows. The filenames themselves should be properly randomized and distributed or 
    /// too many files can still end up on the same folder if they begin with the same characaters.</remarks>
    /// <param name="filename">The file name to use as a template for the folder.</param>
    /// <param name="depth">The depth of the resulting folder structure.</param>
    /// <returns>A string representing a file path with the specified <paramref name="depth"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Depth is lower than 1.</exception>
    public static string? GetFolderAtDepth(this string filename, int depth=3) {
        if (filename == null) return null;
        if (depth < 1)
            throw new ArgumentOutOfRangeException("depth", "Depth must be greater than 0.");
        var sb = new StringBuilder();
        for (int i = 0; i < depth; i++) 
            sb.Append(i < filename.Length ? filename[i] : '0').Append('\\');
        return sb.ToString();
    }

    /// <summary>
    /// Returns a string representing a folder structure of <paramref name="depth"/> subfolders
    /// constructed from a random filename by using the first <c>depth</c> characters of the filename
    /// itself as folder names.
    /// </summary>
    /// <remarks>This method is useful to segregate files into separate folders automatically,
    /// helping to avoid storing all files in a single folder, which is problematic
    /// under Windows. The greater the <paramref name="depth"/> the more segregated the files
    /// will be in the folder structure.</remarks>
    /// <param name="depth">The depth of the resulting folder structure.</param>
    /// <returns>A string representing a file path with the specified <paramref name="depth"/>.</returns>
    public static string? GetRandomFolderAtDepth(int depth = 3) 
        => Path.GetRandomFileName().GetFolderAtDepth(depth);

    /*public static string GetHashedFileName(this byte[] fileBytes) 
        => fileBytes.SpookyHash128().ToString();*/

    /// <summary>
    /// Ensures that the directory <paramref name="folderName"/> exists, creating it if needed.
    /// </summary>
    /// <param name="folderName">The directory path.</param>
    public static void EnsureDirectoryExists(this string folderName) {
        if (!Directory.Exists(folderName))
            Directory.CreateDirectory(folderName);
    }

    /// <summary>
    /// Changes the directory part of a full file path (<paramref name="fileName"/>) to a new path (<paramref name="newPath"/>).
    /// </summary>
    /// <param name="fileName">The original full file path and name.</param>
    /// <param name="newPath">The new file path for the given file name.</param>
    /// <returns>The file name with the path part changed to the new path.</returns>
    public static string ChangeFileDir(this string fileName, string newPath) 
        => Path.Combine(newPath, Path.GetFileName(fileName));
}
