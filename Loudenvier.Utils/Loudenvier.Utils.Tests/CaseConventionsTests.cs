using Loudenvier.Utils;

namespace Loudenvier.Utils.Tests;

public class CaseConventionsTests
{
    [Fact]
    public void ToCamelCase_WorksWithBasicCase() =>
        Assert.Equal("camelCase", CaseConvention.Camel.ApplyCase("Camel-case"));

    [Fact]
    public void ToCamelCaseWorks_WithComplexCase() =>
        Assert.Equal("camelCaseTextCommand", CaseConvention.Camel.ApplyCase("Camel-case---Text-----command"));

    [Fact]
    public void ToPascalCase_WorksWithBasicCase() =>
        Assert.Equal("PascalCase", CaseConvention.Pascal.ApplyCase("pascal-Case"));

    [Fact]
    public void ToPascalCaseWorks_WithComplexCase() =>
        Assert.Equal("PascalCaseTextCommand", CaseConvention.Pascal.ApplyCase("pascal-Case---text-----command"));
    [Fact]
    public void ToPascalCaseWorks_DoesNotSeparateRepeatedCapitals() =>
        Assert.Equal("PascalCaseTextCOMMAND", CaseConvention.Pascal.ApplyCase("pascal-Case---text-----COMMAND"));


    [Fact]
    public void ToKebabCase_WorksWithBasicCase() =>
        Assert.Equal("kebab-case", CaseConvention.Kebab.ApplyCase("KebabCase"));
    [Fact]
    public void ToKebabCase_DontRemoveDashesInOriginalString() =>
        Assert.Equal("--kebab---case", CaseConvention.Kebab.ApplyCase("--Kebab---Case"));
    [Fact]
    public void ToKebabCase_DoNotSeparateRepeatedCapitals() =>
        Assert.Equal("kebab-case-all", CaseConvention.Kebab.ApplyCase("KebabCaseALL"));
    [Fact]
    public void ToSnakeCase_WorksWithBasicCase() =>
        Assert.Equal("snake_case", CaseConvention.Snake.ApplyCase("SnakeCase"));
    [Fact]
    public void ToSnakeCase_DoNotSeparateRepeatedCapitals() =>
        Assert.Equal("snake_case_all", CaseConvention.Snake.ApplyCase("SnakeCaseALL"));
    [Fact]
    public void ToSnakeCase_DontRemoveDashesInOriginalString() =>
        Assert.Equal("__snake___case", CaseConvention.Snake.ApplyCase("__Snake___Case"));
    [Fact]
    public void ToConstantCase_WorksWithBasicCase() =>
        Assert.Equal("CONSTANT_CASE", CaseConvention.Constant.ApplyCase("ConstantCase"));
    [Fact]
    public void ToConstantCase_DoNotSeparateRepeatedCapitals() =>
        Assert.Equal("CONSTANT_CASE_ALL", CaseConvention.Constant.ApplyCase("ConstantCaseAll"));
    [Fact]
    public void ToConstantCase_DontRemoveDashesInOriginalString() =>
        Assert.Equal("__CONSTANT___CASE", CaseConvention.Constant.ApplyCase("__Constant___Case"));
    [Fact]
    public void ToCobalCase_WorksWithBasicCase() =>
        Assert.Equal("COBOL-CASE", CaseConvention.Cobol.ApplyCase("CobolCase"));
    [Fact]
    public void ToCobolCase_DoNotSeparateRepeatedCapitals() =>
        Assert.Equal("COBOL-CASE-ALL", CaseConvention.Cobol.ApplyCase("CobolCaseAll"));
    [Fact]
    public void ToCobolCase_DontRemoveDashesInOriginalString() =>
        Assert.Equal("--COBOL---CASE", CaseConvention.Cobol.ApplyCase("--Cobol---Case"));
    [Fact]
    public void ApplyUnknownCaseConventionThrowsArgumentOutOfRangeException() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ((CaseConvention)1000).ApplyCase("ThrowsException"));

    [Fact]
    public void ToCamelSnakeCase_WorksWithBasicCase() =>
        Assert.Equal("camel_Case", CaseConvention.CamelSnake.ApplyCase("CamelCase"));
    [Fact]
    public void ToCamelSnakeCase_DoNotSeparateRepeatedCapitals() =>
        Assert.Equal("camel_Case_ALL", CaseConvention.CamelSnake.ApplyCase("CamelCaseALL"));
    [Fact]
    public void ToCamelSnakeCase_DontRemoveDashesInOriginalString() =>
        Assert.Equal("__Camel___Case", CaseConvention.CamelSnake.ApplyCase("__Camel___Case"));

    [Fact]
    public void ToPascalSnakeCase_WorksWithBasicCase() =>
        Assert.Equal("Pascal_Case", CaseConvention.PascalSnake.ApplyCase("PascalCase"));
    [Fact]
    public void ToPascalSnakeCase_DoNotSeparateRepeatedCapitals() =>
        Assert.Equal("Pascal_Case_ALL", CaseConvention.PascalSnake.ApplyCase("pascalCaseALL"));
    [Fact]
    public void ToPascalSnakeCase_DontRemoveDashesInOriginalString() =>
        Assert.Equal("__Pascal___Case", CaseConvention.PascalSnake.ApplyCase("__pascal___Case"));

    [Fact]
    public void ToHTTPHeaderCase_WorksWithBasicCase() =>
        Assert.Equal("HTTP-Header-Case", CaseConvention.HTTPHeader.ApplyCase("HTTP-HeaderCase"));
    [Fact]
    public void ToHTTPHeaderCase_DoNotSeparateRepeatedCapitals() =>
        Assert.Equal("HTTP-Header-Case-ALL", CaseConvention.HTTPHeader.ApplyCase("HTTP-header-CaseALL"));
    [Fact]
    public void ToHTTPHeaderCase_DontRemoveDashesInOriginalString() =>
        Assert.Equal("--HTTP-Header---Case", CaseConvention.HTTPHeader.ApplyCase("--HTTP-Header---Case"));

}
