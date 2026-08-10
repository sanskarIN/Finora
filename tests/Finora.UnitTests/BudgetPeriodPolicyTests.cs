using Finora.Domain;

namespace Finora.UnitTests;

public sealed class BudgetPeriodPolicyTests
{
    [Fact]
    public void ValidateBudget_RejectsOverlappingExplicitPeriods()
    {
        var budget = NewBudget(BudgetCadence.Custom);
        budget.Periods.Add(NewPeriod(budget.Id, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 15), 1_000));
        budget.Periods.Add(NewPeriod(budget.Id, new DateOnly(2026, 8, 15), new DateOnly(2026, 8, 31), 1_000));

        Assert.Throws<InvalidOperationException>(() => DomainRules.ValidateBudget(budget));
    }

    [Fact]
    public void CustomBudget_IsInactiveOutsideExplicitPeriod()
    {
        var budget = NewBudget(BudgetCadence.Custom);
        budget.Periods.Add(NewPeriod(budget.Id, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), 1_000));

        Assert.False(BudgetPeriodPolicy.TryResolve(budget, new DateOnly(2026, 9, 1), out _));
    }

    [Fact]
    public void CustomBudget_UsesExplicitPeriodInsideWindow()
    {
        var budget = NewBudget(BudgetCadence.Custom);
        budget.Periods.Add(NewPeriod(budget.Id, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), 1_000));

        Assert.True(BudgetPeriodPolicy.TryResolve(budget, new DateOnly(2026, 8, 10), out var window));
        Assert.Equal(new DateOnly(2026, 8, 1), window.StartsOn);
        Assert.Equal(new DateOnly(2026, 8, 31), window.EndsOn);
        Assert.Equal(1_000, window.PlannedMinor);
    }

    [Fact]
    public void ExplicitRollover_IsAppliedOnlyWhenEnabled()
    {
        var disabled = NewBudget(BudgetCadence.Monthly);
        disabled.RolloverEnabled = false;
        disabled.Periods.Add(NewPeriod(disabled.Id, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), 1_000, 250));
        var enabled = NewBudget(BudgetCadence.Monthly);
        enabled.RolloverEnabled = true;
        enabled.Periods.Add(NewPeriod(enabled.Id, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), 1_000, 250));

        Assert.True(BudgetPeriodPolicy.TryResolve(disabled, new DateOnly(2026, 8, 10), out var disabledWindow));
        Assert.True(BudgetPeriodPolicy.TryResolve(enabled, new DateOnly(2026, 8, 10), out var enabledWindow));
        Assert.Equal(1_000, disabledWindow.PlannedMinor);
        Assert.Equal(1_250, enabledWindow.PlannedMinor);
    }

    [Fact]
    public void WeeklyBudget_UsesMondayThroughSunday()
    {
        var budget = NewBudget(BudgetCadence.Weekly);

        Assert.True(BudgetPeriodPolicy.TryResolve(budget, new DateOnly(2026, 8, 10), out var window));
        Assert.Equal(new DateOnly(2026, 8, 10), window.StartsOn);
        Assert.Equal(new DateOnly(2026, 8, 16), window.EndsOn);
    }

    private static Budget NewBudget(BudgetCadence cadence) => new()
    {
        Name = "Plan",
        Kind = BudgetKind.Overall,
        Cadence = cadence,
        LimitMinor = 1_000,
        Currency = "INR"
    };

    private static BudgetPeriod NewPeriod(Guid budgetId, DateOnly start, DateOnly end, long planned, long rollover = 0) => new()
    {
        BudgetId = budgetId,
        StartsOn = start,
        EndsOn = end,
        PlannedMinor = planned,
        RolloverMinor = rollover
    };
}
