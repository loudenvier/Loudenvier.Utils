namespace Loudenvier.Utils.Tests
{
    public class StringExtensionsTests
    {
        readonly char[] special = ['-', '_', '&', '*', '!', '@', '#', '$', '`'];
        [Fact]
        public void StripChars_RemovesAllCharsFromString() {
            const string s = "Remo_v-e Th&&**e Sp&e&cia!l C#h@a`racter$s";
            const string expected = "Remove The Special Characters";
            var stripped = s.StripChars('-', '_', '&', '*', '!', '@', '#', '$', '`');
            Assert.Equal(expected, stripped);
        }
        [Fact]
        public void StripChars_RemovesFirstChar() {
            const string s = "`Test";
            const string expected = "Test";
            var stripped = s.StripChars('-', '_', '&', '*', '!', '@', '#', '$', '`');
            Assert.Equal(expected, stripped);
        }
        [Fact]
        public void StripChars_ReturnsEmptyStringForNull() {
            const string? s = null;
            const string expected = "";
            var stripped = s.StripChars('-', '_', '&', '*', '!', '@', '#', '$', '`');
            Assert.Equal(expected, stripped);
        }
        [Fact]
        public void StripChars_ReturnsEmptyStringForEmptyString() {
            const string s = "";
            const string expected = "";
            var stripped = s.StripChars('-', '_', '&', '*', '!', '@', '#', '$', '`');
            Assert.Equal(expected, stripped);
        }
        [Fact]
        public void StripChars_ReturnsEmptyStringForStringWithOnlyStrippedChars() {
            const string s = "`@$_-";
            const string expected = "";
            var stripped = s.StripChars('-', '_', '&', '*', '!', '@', '#', '$', '`');
            Assert.Equal(expected, stripped);
        }
        [Fact]
        public void StripChars_ReturnsOriginalStringIfNoCharsPassed() {
            const string s = "original";
            var stripped = s.StripChars();
            Assert.Same(s, stripped);
        }
    }
}
