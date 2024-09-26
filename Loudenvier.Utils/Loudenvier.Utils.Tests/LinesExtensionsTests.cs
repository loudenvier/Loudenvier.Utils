using System.Text;

namespace Loudenvier.Utils.Tests;

public class LinesExtensionsTests {
    [Fact]
    public void GetLinesReturnsExpectedLines() {
        string source = """
            first
            second
            third
            """;
        var lines = new List<string>(source.GetLines());
        Assert.Equal(["first", "second", "third"], lines);
    }
    [Fact]
    public void GetLinesReturnsExpectedLinesWithLineFeedsOnly() {
        string source = "first\nsecond\nthird";
        var lines = new List<string>(source.GetLines());
        Assert.Equal(["first", "second", "third"], lines);
    }
    [Fact]
    public void GetLinesReturnTheStringAsFirstLineEvenWithoutNewLineTerminator() {
        string source = "first";
        var lines = new List<string>(source.GetLines());
        Assert.Equal(["first"], lines);
    }

    [Fact]
    public void GetLinesDontRemoveEmptyLines() {
        string source = """
            first

            third


            sixth
            """;
        var lines = new List<string>(source.GetLines());
        Assert.Equal(["first", "", "third", "", "", "sixth"], lines);
    }

    [Fact]
    public void GetLinesDontRemoveTrailingEmptyLine() {
        string source = "first\r\nsecond\r\nthird\r\n\r\n";
        var lines = new List<string>(source.GetLines());
        Assert.Equal(["first", "second", "third", ""], lines);
    }
    [Fact]
    public void GetLinesDontRemoveTrailingEmptyLines() {
        string source = "first\r\nsecond\r\nthird\r\n\r\n\r\n";
        var lines = new List<string>(source.GetLines());
        Assert.Equal(["first", "second", "third", "", ""], lines);
    }
    [Fact]
    public void GetLinesReturnsEmptyCollectionOnEmptyString() {
        string source = "";
        var lines = new List<string>(source.GetLines());
        Assert.Empty(lines);
    }
    [Fact]
    public void GetLinesReturnsSingleBlankLineOnSigleWhiteSpaceString() {
        string source = " ";
        var lines = new List<string>(source.GetLines());
        Assert.Equal([" "], lines);
    }
    [Fact]
    public void GetLinesWorksWithStreamSources() {
        string source = """
            first
            second
            third
            """;
        using var stm = new MemoryStream(Encoding.ASCII.GetBytes(source.ToCharArray()));
        var lines = new List<string>(stm.GetLines());
        Assert.Equal(["first", "second", "third"], lines);
    }
    [Fact]
    public void GetLinesAsSpansReturnsExpectedLines() {
        string source = """
            first
            second
            third
            """;
        var lines = new List<string>();
        foreach (var line in source.GetLinesAsSpans())
            lines.Add(line.ToString());
        Assert.Equal(["first", "second", "third"], lines);
    }
    [Fact]
    public void GetLinesAsSpansReturnsExpectedLinesWithLineFeedsOnly() {
        string source = "first\nsecond\nthird";
        var lines = new List<string>();
        foreach (var line in source.GetLinesAsSpans())
            lines.Add(line.ToString());
        Assert.Equal(["first", "second", "third"], lines);
    }
    [Fact]
    public void GetLinesAsSpansReturnsEmptyCollectionOnEmptyString() {
        string source = "";
        var lines = new List<string>();
        foreach (var line in source.GetLinesAsSpans())
            lines.Add(line.ToString());
        Assert.Empty(lines);
    }
    [Fact]
    public void GetLinesAsSpansReturnTheStringAsFirstLineEvenWithoutNewLineTerminator() {
        string source = "first";
        var lines = new List<string>();
        foreach (var line in source.GetLinesAsSpans())
            lines.Add(line.ToString());
        Assert.Equal(["first"], lines);
    }
    [Fact]
    public void GetLinesAsSpansDontRemoveEmptyLines() {
        string source = """
            first

            third


            sixth
            """;
        var lines = new List<string>();
        foreach (var line in source.GetLinesAsSpans())
            lines.Add(line.ToString());
        Assert.Equal(["first", "", "third", "", "", "sixth"], lines);
    }
    [Fact]
    public void GetLinesAsSpansDontRemoveTrailingEmptyLine() {
        string source = "first\r\nsecond\r\nthird\n\n";
        var lines = new List<string>();
        foreach (var line in source.GetLinesAsSpans())
            lines.Add(line.ToString());
        Assert.Equal(["first", "second", "third", ""], lines);
    }

}
