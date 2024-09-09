using Loudenvier.Utils;

namespace Loudenvier.Utils.Tests
{
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
    }
}
