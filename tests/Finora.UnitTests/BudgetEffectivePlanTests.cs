using Finora.Domain;

namespace Finora.UnitTests;

public sealed class BudgetEffectivePlanTests
{
    [Fact]
    public void EnabledRollover_CannotReduceEffectivePlanToZero()
    {
        var budget = CreateBudget(1_000, true);
        budget.Periods.Add(CreatePeriod(budget.Id, 1_000, -1_000));

        Assert.Throws<InvalidDataException>(() =>
            BudgetPeriodPolicy.TryResolve(budget, new DateOnly(2026, 8, 10), out _));
    }

    [Fact]
    public void EnabledRollover_CannotReduceEffectivePlanBelowZero()
    {
        var budget = CreateBudget(1_000, true);
        budget.Periods.Add(CreatePeriod(budget.Id, 1_000, -1_001));

        Assert.Throws<InvalidDataException>(() =>
            BudgetPeriodPolicy.TryResolve(budget, new DateOnly(2026, 8, 10), out _));
    }

    [Fact]
    public void EnabledRollover_UsesCheckedAddition()
    {
        var budget = CreateBudget(long.MaxValue, true);
        budget.Periods.Add(CreatePeriod(budget.Id, long.MaxValue, 1));

        Assert.Throws<OverflowException>(() =>
            BudgetPeriodPolicy.TryResolve(budget, new DateOnly(2026, 8, 10), out _));
    }

    [Fact]
    public void DisabledRollover_IgnoresNegativeHistoricalRollover()
    {
        var budget = CreateBudget(1_000, false);
        budget.Periods.Add(CreatePeriod(budget.Id, 1_000, -5_000));

        Assert.True(BudgetPeriodPolicy.TryResolve(budget, new DateOnly(2026, 8, 10), out var window));
        Assert.Equal(1_000, window.PlannedMinor);
    }

    private static Budget CreateBudget(long limitMinor, bool rolloverEnabled) => new()
    {
        Name = "Budget",
        Kind = BudgetKind.Overall,
        Cadence = BudgetCadence.Monthly,
        LimitMinor = limitMinor,
        Currency = "INR",
        RolloverEnabled = rolloverEnabled
    };

    private static BudgetPeriod CreatePeriod(Guid budgetId, long plannedMinor, long rolloverMinor) => new()
    {
        BudgetId = budgetId,
        StartsOn = new DateOnly(2026, 8, 1),
        EndsOn = new DateOnly(2026, 8, 31),
        PlannedMinor = plannedMinor,
        RolloverMinor = rolloverMinor
    };
}
