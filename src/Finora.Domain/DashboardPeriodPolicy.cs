namespace Finora.Domain;

public enum DashboardPeriod
{
    CurrentFinancialMonth,
    PreviousFinancialMonth,
    Last30Days,
    Last90Days,
    YearToDate
}

public readonly record struct DashboardDateRange(DateOnly From, DateOnly Through)
{
    public int InclusiveDayCount => Through.DayNumber - From.DayNumber + 1;
}

public static class DashboardPeriodPolicy
{
    public static DashboardDateRange Resolve(DashboardPeriod period, DateOnly today, int financialMonthStartDay)
    {
        if (today == default) throw new ArgumentException("Dashboard date requires a valid current date.", nameof(today));
        if (financialMonthStartDay is < 1 or > 28)
            throw new ArgumentOutOfRangeException(nameof(financialMonthStartDay), "Financial month start day must be between 1 and 28.");

        var currentFinancialStart = ResolveFinancialMonthStart(today, financialMonthStartDay);
        return period switch
        {
            DashboardPeriod.CurrentFinancialMonth => new DashboardDateRange(currentFinancialStart, today),
            DashboardPeriod.PreviousFinancialMonth => new DashboardDateRange(currentFinancialStart.AddMonths(-1), currentFinancialStart.AddDays(-1)),
            DashboardPeriod.Last30Days => new DashboardDateRange(today.AddDays(-29), today),
            DashboardPeriod.Last90Days => new DashboardDateRange(today.AddDays(-89), today),
            DashboardPeriod.YearToDate => new DashboardDateRange(new DateOnly(today.Year, 1, 1), today),
            _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Dashboard period is unsupported.")
        };
    }

    public static string GetLabel(DashboardPeriod period) => period switch
    {
        DashboardPeriod.CurrentFinancialMonth => "Current financial month",
        DashboardPeriod.PreviousFinancialMonth => "Previous financial month",
        DashboardPeriod.Last30Days => "Last 30 days",
        DashboardPeriod.Last90Days => "Last 90 days",
        DashboardPeriod.YearToDate => "Year to date",
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Dashboard period is unsupported.")
    };

    private static DateOnly ResolveFinancialMonthStart(DateOnly today, int startDay)
    {
        if (today.Day >= startDay) return new DateOnly(today.Year, today.Month, startDay);
        var previousMonth = today.AddMonths(-1);
        return new DateOnly(previousMonth.Year, previousMonth.Month, startDay);
    }
}
