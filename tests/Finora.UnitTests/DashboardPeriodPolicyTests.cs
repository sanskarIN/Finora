using Finora.Domain;

namespace Finora.UnitTests;

public sealed class DashboardPeriodPolicyTests
{
    [Fact]
    public void CurrentFinancialMonth_UsesConfiguredStartAndStopsAtToday()
    {
        var range = DashboardPeriodPolicy.Resolve(DashboardPeriod.CurrentFinancialMonth, new DateOnly(2026, 8, 11), 5);

        Assert.Equal(new DateOnly(2026, 8, 5), range.From);
        Assert.Equal(new DateOnly(2026, 8, 11), range.Through);
        Assert.Equal(7, range.InclusiveDayCount);
    }

    [Fact]
    public void CurrentFinancialMonth_RollsToPreviousCalendarMonthBeforeStartDay()
    {
        var range = DashboardPeriodPolicy.Resolve(DashboardPeriod.CurrentFinancialMonth, new DateOnly(2026, 8, 3), 5);

        Assert.Equal(new DateOnly(2026, 7, 5), range.From);
        Assert.Equal(new DateOnly(2026, 8, 3), range.Through);
    }

    [Fact]
    public void PreviousFinancialMonth_IsCompleteClosedPeriod()
    {
        var range = DashboardPeriodPolicy.Resolve(DashboardPeriod.PreviousFinancialMonth, new DateOnly(2026, 8, 11), 5);

        Assert.Equal(new DateOnly(2026, 7, 5), range.From);
        Assert.Equal(new DateOnly(2026, 8, 4), range.Through);
    }

    [Theory]
    [InlineData(DashboardPeriod.Last30Days, 30)]
    [InlineData(DashboardPeriod.Last90Days, 90)]
    public void TrailingPeriods_IncludeTodayAndExpectedNumberOfDays(DashboardPeriod period, int expectedDays)
    {
        var range = DashboardPeriodPolicy.Resolve(period, new DateOnly(2026, 8, 11), 1);

        Assert.Equal(expectedDays, range.InclusiveDayCount);
        Assert.Equal(new DateOnly(2026, 8, 11), range.Through);
    }

    [Fact]
    public void YearToDate_StartsOnJanuaryFirst()
    {
        var range = DashboardPeriodPolicy.Resolve(DashboardPeriod.YearToDate, new DateOnly(2026, 8, 11), 1);

        Assert.Equal(new DateOnly(2026, 1, 1), range.From);
        Assert.Equal(new DateOnly(2026, 8, 11), range.Through);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(29)]
    public void InvalidFinancialMonthStart_IsRejected(int day)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DashboardPeriodPolicy.Resolve(DashboardPeriod.CurrentFinancialMonth, new DateOnly(2026, 8, 11), day));
    }
}
