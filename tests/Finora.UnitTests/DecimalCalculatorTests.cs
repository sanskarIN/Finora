using Finora.Application;

namespace Finora.UnitTests;

public sealed class DecimalCalculatorTests
{
    [Theory]
    [InlineData("2+3*4", "14")]
    [InlineData("(2+3)*4", "20")]
    [InlineData("10/4", "2.5")]
    [InlineData("1.25+2.75", "4.00")]
    [InlineData("-2+5", "3")]
    public void Evaluate_UsesDecimalPrecedence(string expression, string expected)
        => Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), DecimalCalculator.Evaluate(expression));

    [Fact]
    public void Evaluate_DivideByZero_IsRejected()
        => Assert.Throws<DivideByZeroException>(() => DecimalCalculator.Evaluate("1/0"));
}
