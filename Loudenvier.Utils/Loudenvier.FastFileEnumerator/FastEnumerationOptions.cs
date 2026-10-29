namespace Loudenvier.Utils.IO;

/// <summary>Backend selection. Auto uses the BCL on .NET 6+,
/// native NT on Windows with net472/netstandard2.0, and the portable fallback otherwise.</summary>
public enum FastEnumerationBackend
{
    Auto,
    Bcl,
    WindowsNt,
    Portable
}

/// <summary>Options for synchronous file and directory enumeration.</summary>
public sealed class FastEnumerationOptions
{
    /// <summary>Simple filename glob (* and ? only; backslash is literal). Default "*".</summary>
    public string SearchPattern { get; set; } = "*";

    /// <summary>Visit subdirectories; directory links are never traversed. Default true.</summary>
    public bool RecurseSubdirectories { get; set; } = true;

    public bool IncludeFiles { get; set; } = true;
    public bool IncludeDirectories { get; set; } = true;

    /// <summary>Skip inaccessible subdirectories (not an inaccessible root).</summary>
    public bool IgnoreInaccessible { get; set; }

    /// <summary>
    /// Null means case-insensitive on Windows, case-sensitive elsewhere.
    /// Matching uses ordinal-like character folding, not locale-sensitive matching.
    /// </summary>
    public bool? IgnoreCase { get; set; }

    /// <summary>Requested native/buffer hint in bytes; defaults to 52 KiB.
    /// Portable fallback does not use this setting.</summary>
    public int BufferSize { get; set; } = 52 * 1024;

    public FastEnumerationBackend Backend { get; set; } = FastEnumerationBackend.Auto;
}
