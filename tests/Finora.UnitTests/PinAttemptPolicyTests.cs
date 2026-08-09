using Finora.Shared;

namespace Finora.UnitTests;

public sealed class PinAttemptPolicyTests
{
    [Theory]
    [InlineData(-100, 1)]
    [InlineData(0, 1)]
    [InlineData(4, 5)]
    [InlineData(999, 1000)]
    [InlineData(int.MaxValue, 1000)]
    public void NextFailureCount_IsBounded(int current, int expected)
    {
        Assert.Equal(expected, PinAttemptPolicy.NextFailureCount(current));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(4, 0)]
    [InlineData(5, 1)]
    [InlineData(6, 2)]
    [InlineData(7, 4)]
    [InlineData(8, 8)]
    [InlineData(9, 16)]
    [InlineData(10, 30)]
    [InlineData(1000, 30)]
    [InlineData(int.MaxValue, 30)]
    public void LockoutDuration_EscalatesAndCaps(int failures, int expectedMinutes)
    {
        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), PinAttemptPolicy.GetLockoutDuration(failures));
    }
}
