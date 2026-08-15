using Finora.Shared;

namespace Finora.UnitTests;

public sealed class LocalDateRangeTests
{
    [Fact]
    public void FixedOffsetZone_ConvertsLocalMidnightToUtc()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("IST-test", TimeSpan.FromHours(5.5), "IST-test", "IST-test");

        var range = LocalDateRange.ToUtc(new DateOnly(2026, 8, 11), new DateOnly(2026, 8, 11), zone);

        Assert.Equal(new DateTimeOffset(2026, 8, 10, 18, 30, 0, TimeSpan.Zero), range.FromUtc);
        Assert.Equal(new DateTimeOffset(2026, 8, 11, 18, 30, 0, TimeSpan.Zero), range.ToExclusiveUtc);
        Assert.Equal(TimeSpan.FromDays(1), range.Duration);
    }

    [Fact]
    public void NegativeFixedOffsetZone_ConvertsLocalMidnightToUtc()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("UTC-minus-seven-test", TimeSpan.FromHours(-7), "UTC-minus-seven-test", "UTC-minus-seven-test");

        var range = LocalDateRange.ToUtc(new DateOnly(2026, 8, 11), new DateOnly(2026, 8, 11), zone);

        Assert.Equal(new DateTimeOffset(2026, 8, 11, 7, 0, 0, TimeSpan.Zero), range.FromUtc);
        Assert.Equal(new DateTimeOffset(2026, 8, 12, 7, 0, 0, TimeSpan.Zero), range.ToExclusiveUtc);
        Assert.Equal(TimeSpan.FromDays(1), range.Duration);
    }

    [Fact]
    public void MultiDayRange_UsesExclusiveBoundaryAfterThroughDate()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("UTC-plus-two-test", TimeSpan.FromHours(2), "UTC-plus-two-test", "UTC-plus-two-test");

        var range = LocalDateRange.ToUtc(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 3), zone);

        Assert.Equal(new DateTimeOffset(2026, 7, 31, 22, 0, 0, TimeSpan.Zero), range.FromUtc);
        Assert.Equal(new DateTimeOffset(2026, 8, 3, 22, 0, 0, TimeSpan.Zero), range.ToExclusiveUtc);
    }

    [Fact]
    public void ReversedRange_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => LocalDateRange.ToUtc(new DateOnly(2026, 8, 12), new DateOnly(2026, 8, 11), TimeZoneInfo.Utc));
    }
}
