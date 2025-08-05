using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net.Mime;

namespace Loudenvier.Utils;

/// <summary>
/// Implements methods to parse and encode data urls (https://developer.mozilla.org/en-US/docs/Web/HTTP/Basics_of_HTTP/Data_URLs) 
/// </summary>
public class DataUrl {
    public const string Base64Encoding = "base64";
    public const string DefaultMediaType = "text/plain";
    
    /// <summary>
    /// Creates a new DataUrl. You should not url encode the <paramref name="data"/> if it's
    /// 'text/plain'. Use the <see cref="PercentEncodedData"/> property.
    /// </summary>
    /// <param name="data">The non-url-encoded contents of this dataUrl.</param>
    /// <param name="contentType">The optional mime media type with optional parameters</param>
    /// <param name="encoding">The optional encoding ('base64' is the only useful encoding currently)</param>
    public DataUrl(string data, string? contentType = null, string? encoding = null) {
        Data = data;
        if (!string.IsNullOrEmpty(contentType)) {
            ContentMediaType = new ContentType(contentType);
            MediaType = ContentMediaType.MediaType;
            MediaTypeParameters = [];
            foreach (string key in ContentMediaType.Parameters.Keys)
                MediaTypeParameters.Add(key, ContentMediaType!.Parameters![key]!);
        }
        Encoding = encoding;
        Base64Encoded = encoding == Base64Encoding;
    }
    /// <summary>The media type of this dataUrl (if null 'text/plain' should be inferred -> <see cref="MediaTypeOrDefault"/>)</summary>
    public string? MediaType { get; private set; }
    public Dictionary<string, string> MediaTypeParameters { get; private set; } = [];
    public ContentType? ContentMediaType { get; private set; }
    /// <summary>Returns the encoding (currently only 'base64' is of any use)</summary>
    public string? Encoding { get; private set; }
    /// <summary>Returns if the Data is Base64 encoded</summary>
    public bool Base64Encoded { get; private set; }
    /// <summary>The contents of this data URL. It may be a base64 string or a plain text string which is not URL Encoded.</summary>
    public string Data { get; private set; }


    /// <summary>Returns the MediaType or "text/plain" if the Media Type is not defined.</summary>
    public string MediaTypeOrDefault => MediaType ?? DefaultMediaType;
    /// <summary>Returns the URL Encoded string data, but only if it's not Base64 encoded, in which case it return null.</summary>
    public string? PercentEncodedData => Base64Encoded ? null : Uri.EscapeDataString(Data);
    /// <summary>If there's a <see cref="MediaType"/> defined returns a string in the form {mediaType/Encoding}</summary>
    public string MediaTypeAndEncoding {
        get {
            var s = "";
            if (ContentMediaType != null) 
                s = ContentMediaType.ToString();
            if (Encoding != null)
                s += $";{Encoding}";
            return s;
        }
    }
    /// <summary>The default extension associated with the mime media type, or an empty string if no extension is found.</summary>
    public string MediaFileExtension => MimeTypeMap.GetExtension(MediaTypeOrDefault, throwErrorIfNotFound: false);

    static readonly Encoding UTF8NoBOM = new UTF8Encoding(false, true);
    /// <summary>
    /// Get a byte[] representation of the <see cref="Data"/> property. It will decode it from Base 64 or as a byte array in the
    /// provided character <paramref name="encoding"/> (or UTF8 by default)
    /// </summary>
    /// <param name="encoding">The character encoding used if the data is plain text (if ommited will default to UTF8)</param>
    /// <returns>A byte array representation of the Data in this object</returns>
    public byte[] GetDataBytes(Encoding? encoding = null) {
        if (Base64Encoded)
            return Convert.FromBase64String(Data);
        return (encoding ?? UTF8NoBOM).GetBytes(Data);
    }

    /// <summary>
    /// Saves the <see cref="Data"/> possibly decoded if it's in Base 64 to <paramref name="fileName"/>. It appends the default
    /// file extension to the filename if <paramref name="appendMediaExtension"/> is set. 
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="appendMediaExtension"></param>
    /// <returns>The resulting filename used, which may include the detault mime media type extension.</returns>
    public string SaveData(string fileName, bool appendMediaExtension = true) {
        if (appendMediaExtension) {
            var ext = MediaFileExtension;
            if (!string.IsNullOrWhiteSpace(ext))
                fileName += ext;
        }
        var dir = Path.GetDirectoryName(fileName);
        if (dir is not null)
            Directory.CreateDirectory(dir);
        var stm = new FileStream(fileName, FileMode.Create);
        SaveData(stm);
        return fileName;
    }

    /// <summary>
    /// Saves the <see cref="Data"/> possibly decoded if it's in Base 64 to the stream <paramref name="stm"/>. 
    /// </summary>
    /// <param name="stm">The destination stream</param>
    /// <param name="encoding">The character encoding used if the data is plain text (if ommited will default to UTF8)</param>
    public void SaveData(Stream stm, Encoding? encoding=null) {
        var bytes = GetDataBytes(encoding);
        stm.Write(bytes, 0, bytes.Length);
    }
    /// <summary>
    /// Returns a string representation of this Data Url, properly encoding plain text data with percent encoding.
    /// </summary>
    /// <returns>This dataUrl as a string</returns>
    public override string ToString() => $"data:{MediaTypeAndEncoding},{PercentEncodedData ?? Data}";
    
}

/// <summary>
/// Helper class to parse Data Urls (https://developer.mozilla.org/en-US/docs/Web/HTTP/Basics_of_HTTP/Data_URLs)
/// </summary>
public static class DataUrlParser
{
    const string prefix = "data:";
    static readonly int minLength = prefix.Length + 1;

    /// <summary>
    /// Parses a Data Url in the form data:[<mediatype>][;base64],<data>
    /// </summary>
    /// <param name="dataUrl">The contents of the data url</param>
    /// <returns>An instance of <see cref="DataUrl"/> parsed from <paramref name="dataUrl"/></returns>
    public static DataUrl Parse(string dataUrl, bool percentEncoded=true) {
        if (dataUrl == null) 
            throw new ArgumentNullException(nameof(dataUrl));
        if (dataUrl.Length < minLength) 
            throw new FormatException($"The DataUrl is smaller than the minimun length of {minLength}. DataUrl: {dataUrl}");
        var readPrefix = dataUrl[..prefix.Length];
        if (readPrefix != prefix)
            throw new FormatException($"The DataUrl has a wrong prefix. Expected: {prefix} - Found: {readPrefix}");
        var dataSepIdx = dataUrl.IndexOf(',', prefix.Length);
        if (dataSepIdx <= 0)
            throw new FormatException($"The DataUrl has no data separator (,): DataUrl: {dataUrl}");
        // optional mime and encoding lies from data: to the first ','  
        var mimeAndEncoding = dataUrl[prefix.Length..dataSepIdx];
        // content lies just after the position of the aforementioned ',' 
        var content = dataUrl[(dataSepIdx + 1)..];
        // mime type and encoding are optional, when present are separated by ';'
        // but mime type may have parameters also separated by ';'
        string? mime = null;
        string? enc = null;
        if (!string.IsNullOrWhiteSpace(mimeAndEncoding)) {
            var mimePartLength = mimeAndEncoding.Length;
            // let's check if it has an encoding
            var lastIdx = mimeAndEncoding.LastIndexOf(';');
            if (lastIdx >= 0) {
                // it may have an encoding... (currently only base64 makes sense)
                var possibleEnc = mimeAndEncoding[(lastIdx + 1)..];
                // ...if it's not a attr=value pair (I won't consider starting '=' as valid)
                if (possibleEnc.IndexOf('=') < 1) {
                    enc = possibleEnc;
                    // the mime part should not include the encoding!
                    mimePartLength -= possibleEnc.Length + 1;
                }
            }
            mime = mimeAndEncoding[..mimePartLength];
        }
        // if the caller is telling us that the string is percent encoded, lets decode it as DataUrl requires
        if (percentEncoded)
            content = Uri.UnescapeDataString(content);
        return new DataUrl(content, mime, enc);
    }
    
}
