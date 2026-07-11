
namespace Loudenvier.Utils.Tests;

public class ByteSequenceParserTests
{
    // ==========================================
    // 1. TESTES DE SUCESSO (CASOS FELIZES)
    // ==========================================

    [Fact]
    public void Parse_HexByteSequence_ReturnsExpectedByteArray() {
        var input = "01 02 03 FF a0"; // Mistura de maiúsculas e minúsculas
        byte[] expected = [1, 2, 3, 255, 160];

        var result = ByteSequenceParser.Parse(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parse_DecimalSequence_ReturnsExpectedByteArray() {
        // Nota: 1 dígito ou 3 dígitos são tratados como decimais.
        var input = "0 9 010 128 255";
        byte[] expected = [0, 9, 10, 128, 255];

        var result = ByteSequenceParser.Parse(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parse_TwoDigitNumber_IsAlwaysTreatedAsHex() {
        // De acordo com as regras da classe, exatamente 2 caracteres válidos em hex
        // são tratados como base 16, mesmo que pareçam decimais.
        var input = "10 09"; // 10 hex = 16 dec | 09 hex = 9 dec
        byte[] expected = [16, 9];

        var result = ByteSequenceParser.Parse(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parse_BinarySequence_ReturnsExpectedByteArray() {
        var input = "b0 B1 b1010 B11111111"; // Suporta 'b' e 'B'
        byte[] expected = [0, 1, 10, 255];

        var result = ByteSequenceParser.Parse(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parse_MixedSequence_ParsesAllTokensCorrectly() {
        // O teste de integração final: Hex, Dec, Bin e Strings juntos
        var input = "FF 010 b101 'A' \"B\"";
        byte[] expected = [255, 10, 5, 65, 66];

        var result = ByteSequenceParser.Parse(input);

        Assert.Equal(expected, result);
    }

    // ==========================================
    // 2. TESTES DE STRINGS E ESCAPES
    // ==========================================

    [Theory]
    [InlineData("'HELLO'", new byte[] { 72, 69, 76, 76, 79 })]
    [InlineData("\"WORLD\"", new byte[] { 87, 79, 82, 76, 68 })]
    [InlineData("''", new byte[] { })] // String simples vazia
    [InlineData("\"\"", new byte[] { })] // String dupla vazia
    public void Parse_StringLiterals_ReturnsAsciiBytes(string input, byte[] expected) {
        var result = ByteSequenceParser.Parse(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parse_StringWithEscapes_ReturnsCorrectControlBytes() {
        // Testando: \n (10), \r (13), \t (9), \' (39), \" (34), \\ (92)
        var input = "'\\n\\r\\t\\'\\\\\"'";
        byte[] expected = [10, 13, 9, 39, 92, 34];

        var result = ByteSequenceParser.Parse(input);

        Assert.Equal(expected, result);
    }

    // ==========================================
    // 3. TESTES DE EXTREMOS (EDGE CASES)
    // ==========================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Parse_EmptyOrWhitespaceInput_ReturnsEmptyArray(string input) {
        var result = ByteSequenceParser.Parse(input);
        Assert.Empty(result);
    }

    // ==========================================
    // 4. TESTES DE EXCEÇÃO (FALHAS ESPERADAS)
    // ==========================================

    [Theory]
    [InlineData("'String sem fechar")]
    [InlineData("\"Outra sem fechar")]
    public void Parse_UnclosedString_ThrowsFormatException(string input) {
        var ex = Assert.Throws<FormatException>(() => ByteSequenceParser.Parse(input));
        Assert.Contains("Syntax error: String literal was not closed", ex.Message);
    }

    [Theory]
    [InlineData("256")]   // Acima do limite de um byte (Decimal)
    [InlineData("-1")]    // Abaixo do limite de um byte (Decimal)
    [InlineData("ZZ")]    // Comprimento 2, mas não é hex nem decimal válido
    [InlineData("b102")]  // Prefixo binário, mas contém o dígito '2'
    [InlineData("0xFF")]  // Sintaxe C# não suportada (o correto no nosso parser é só FF)
    public void Parse_InvalidTokens_ThrowsFormatException(string input) {
        Assert.Throws<FormatException>(() => ByteSequenceParser.Parse(input));
    }
}