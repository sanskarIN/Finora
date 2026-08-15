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
    public void DaylightSavingStartDay_UsesTwentyThreeHourUtcSpan()
    {
        var zone = CreateDstTestZone();

        var range = LocalDateRange.ToUtc(new DateOnly(2026, 3, 8), new DateOnly(2026, 3, 8), zone);

        Assert.Equal(new DateTimeOffset(2026, 3, 8, 5, 0, 0, TimeSpan.Zero), range.FromUtc);
        Assert.Equal(new DateTimeOffset(2026, 3, 9, 4, 0, 0, TimeSpan.Zero), range.ToExclusiveUtc);
        Assert.Equal(TimeSpan.FromHours(23), range.Duration);
    }

    [Fact]
    public void DaylightSavingEndDay_UsesTwentyFiveHourUtcSpan()
    {
        var zone = CreateDstTestZone();

        var range = LocalDateRange.ToUtc(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 1), zone);

        Assert.Equal(new DateTimeOffset(2026, 11, 1, 4, 0, 0, TimeSpan.Zero), range.FromUtc);
        Assert.Equal(new DateTimeOffset(2026, 11, 2, 5, 0, 0, TimeSpan.Zero), range.ToExclusiveUtc);
        Assert.Equal(TimeSpan.FromHours(25), range.Duration);
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

    private static TimeZoneInfo CreateDstTestZone()
    {
        var daylightStart = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
            new DateTime(1, 1, 1, 2, 0, 0),
            3,
            2,
            DayOfWeek.Sunday);
        var daylightEnd = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
            new DateTime(1, 1, 1, 2, 0, 0),
            11,
            1,
            DayOfWeek.Sunday);
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2020, 1, 1),
            new DateTime(2030, 12, 31),
            TimeSpan.FromHours(1),
            daylightStart,
            daylightEnd);

        return TimeZoneInfo.CreateCustomTimeZone(
            "DST-boundary-test",
            TimeSpan.FromHours(-5),
            "DST-boundary-test",
            "DST-standard-test",
            "DST-daylight-test",
            [rule]);
    }
}
