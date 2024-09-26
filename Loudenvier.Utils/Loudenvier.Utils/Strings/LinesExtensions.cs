using System;
using System.Collections.Generic;
using System.IO;

namespace Loudenvier.Utils;

public static class LinesExtensions
{
    public static IEnumerable<string> GetLines(this string line) => GetLines(new StringReader(line));
    public static IEnumerable<string> GetLines(this Stream stm) => GetLines(new StreamReader(stm));
    public static IEnumerable<string> GetLines(this TextReader reader) {
        string? line;
        while ((line = reader.ReadLine()) != null)
            yield return line;
        reader.Dispose();
        yield break;
    }

    public static LineEnumerator GetLinesAsSpans(this string text) => new(text.AsSpan());

    // based on Dennis answer: https://stackoverflow.com/a/65969222/285678
    public ref struct LineEnumerator(ReadOnlySpan<char> text)
    {
        private ReadOnlySpan<char> Text { get; set; } = text;
        public ReadOnlySpan<char> Current { get; private set; } = default;
        public readonly LineEnumerator GetEnumerator() => this;

        public bool MoveNext() {
            if (Text.IsEmpty) return false;

            var index = Text.IndexOf('\n'); // \r\n or \n
            if (index != -1) {
                // removes \r\n or \n from resulting line as most ReadLine methods do
                var shift = index > 0 && Text[index - 1] == '\r' ? 1 : 0;
                Current = Text[..(index - shift)];
                Text = Text[(index + 1)..];
                return true;
            } else {
                Current = Text;
                Text = [];
                return true;
            }
        }
    }
}
