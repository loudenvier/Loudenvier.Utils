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

        [Fact]
        public void SplitOnUpperCaseWorksAsExpectedWithDefaultOption() {
            var parts = "FelipeRochaMachado".SplitOnUpperOrSeparator();
            Assert.Equal(["Felipe", "Rocha", "Machado"], parts);
        }
        [Fact]
        public void SplitOnSeparatorWorksAsExpectedWithDefaultOption() {
            var parts = "felipe-rocha-machado".SplitOnUpperOrSeparator('-');
            Assert.Equal(["felipe", "rocha", "machado"], parts);
        }
        [Fact]
        public void SplitOnUpperCaseKeepsSequencesOfUpperCaseTogether() {
            var parts = "FelipeRochaMACHADO".SplitOnUpperOrSeparator('-');
            Assert.Equal(["Felipe", "Rocha", "MACHADO"], parts);
        }
        [Fact]
        public void SplitOnSeparatorsRemoveRepeatingSeparators() {
            var parts = "Felipe--Rocha---MACHADO".SplitOnUpperOrSeparator('-');
            Assert.Equal(["Felipe", "Rocha", "MACHADO"], parts);
        }
        [Fact]
        public void SplitOnUpperCaseKeepsSequencesWithFollowingLowerCasesTogether() {
            var parts = "Felipe---RochaMACHado".SplitOnUpperOrSeparator('-');
            Assert.Equal(["Felipe", "Rocha", "MACHado"], parts);
        }
        [Fact]
        public void SplitOnSeparatorsAlsoRemovesStartingSeparator() {
            var parts = "-FelipeMAChado".SplitOnUpperOrSeparator('-');
            Assert.Equal(["Felipe", "MAChado"], parts);
        }
        [Fact]
        public void SplitOnSeparatorsAlsoRemovesStartingSeparators() {
            var parts = "---FelipeMAChado".SplitOnUpperOrSeparator('-');
            Assert.Equal(["Felipe", "MAChado"], parts);
        }
        [Fact]
        public void SplitOnSeparatorsAlsoRemovesTrailingSeparators() {
            var parts = "FelipeMAChado---".SplitOnUpperOrSeparator('-');
            Assert.Equal(["Felipe", "MAChado"], parts);
        }
        [Fact]
        public void SplitOnSeparatorsKeepsSeparatorsIfToldTo() {
            var parts = "felipe--rocha-machado".SplitOnUpperOrSeparator(keepSeparators: true, '-');
            Assert.Equal(["felipe", "--", "rocha", "-", "machado"], parts);
        }
        [Fact]
        public void SplitOnSeparatorsKeepsSeparatorsIfToldTo2() {
            var parts = "---felipe--rocha----machado".SplitOnUpperOrSeparator(keepSeparators: true, '-');
            Assert.Equal(["---","felipe", "--", "rocha", "----", "machado"], parts);
        }
        [Fact]
        public void SplitOnSeparatorsKeepsTrailingSeparators() {
            var parts = "felipe-rocha-machado---".SplitOnUpperOrSeparator(keepSeparators: true, '-');
            Assert.Equal(["felipe", "-", "rocha", "-", "machado", "---"], parts);
        }
        [Fact]
        public void SplitOnSeparatorsKeepsSingleTrailingSeparator() {
            var parts = "felipe-rocha-machado-".SplitOnUpperOrSeparator(keepSeparators: true, '-');
            Assert.Equal(["felipe", "-", "rocha", "-", "machado", "-"], parts);
        }

    }
}
