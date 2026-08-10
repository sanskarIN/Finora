namespace Finora.Domain;

public readonly record struct BudgetWindow(DateOnly StartsOn, DateOnly EndsOn, long PlannedMinor);

public static class BudgetPeriodPolicy
{
    public static bool TryResolve(Budget budget, DateOnly date, out BudgetWindow window)
    {
        ArgumentNullException.ThrowIfNull(budget);
        if (date == default) throw new ArgumentException("Budget date is required.", nameof(date));
        DomainRules.ValidateBudget(budget);

        var explicitPeriod = budget.Periods.SingleOrDefault(period => period.StartsOn <= date && period.EndsOn >= date);
        if (explicitPeriod is not null)
        {
            var rollover = budget.RolloverEnabled ? explicitPeriod.RolloverMinor : 0L;
            window = new BudgetWindow(explicitPeriod.StartsOn, explicitPeriod.EndsOn, checked(explicitPeriod.PlannedMinor + rollover));
            return true;
        }

        switch (budget.Cadence)
        {
            case BudgetCadence.Weekly:
            {
                var offset = ((int)date.DayOfWeek + 6) % 7;
                var start = date.AddDays(-offset);
                window = new BudgetWindow(start, start.AddDays(6), budget.LimitMinor);
                return true;
            }
            case BudgetCadence.Monthly:
            {
                var start = new DateOnly(date.Year, date.Month, 1);
                window = new BudgetWindow(start, new DateOnly(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month)), budget.LimitMinor);
                return true;
            }
            case BudgetCadence.Custom:
                window = default;
                return false;
            default:
                throw new InvalidDataException("Budget cadence is unsupported.");
        }
    }
}
