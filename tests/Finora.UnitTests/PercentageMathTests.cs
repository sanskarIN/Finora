using Finora.Application;

namespace Finora.UnitTests;

public sealed class PercentageMathTests
{
    [Theory]
    [InlineData(100, 80, 80)]
    [InlineData(101, 80, 81)]
    [InlineData(1, 1, 1)]
    [InlineData(1, 100, 1)]
    [InlineData(0, 80, 0)]
    [InlineData(999, 0, 0)]
    public void CeilingPercentOf_DoesNotTriggerBeforeFractionalThreshold(long amount, int percent, long expected)
        => Assert.Equal(expected, PercentageMath.CeilingPercentOf(amount, percent));

    [Fact]
    public void CeilingPercentOf_HandlesLargestSupportedAmountAtOneHundredPercent()
        => Assert.Equal(long.MaxValue, PercentageMath.CeilingPercentOf(long.MaxValue, 100));

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void CeilingPercentOf_RejectsInvalidPercent(int percent)
        => Assert.Throws<ArgumentOutOfRangeException>(() => PercentageMath.CeilingPercentOf(100, percent));

    [Fact]
    public void CeilingPercentOf_RejectsNegativeAmounts()
        => Assert.Throws<ArgumentOutOfRangeException>(() => PercentageMath.CeilingPercentOf(-1, 50));
}
