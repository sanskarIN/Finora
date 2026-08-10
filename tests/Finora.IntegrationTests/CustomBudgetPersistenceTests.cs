using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class CustomBudgetPersistenceTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-custom-budget-{Guid.NewGuid():N}");
    private FinanceStoreTests.TestFactory _factory = null!;
    private FinanceStore _store = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        var options = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_root, "finora.db")}")
            .Options;
        _factory = new FinanceStoreTests.TestFactory(options);
        _store = new FinanceStore(_factory, new DatabaseInitializer(_factory));
        await _store.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        try { Directory.Delete(_root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CustomBudget_RequiresExplicitPeriod()
    {
        var budget = NewCustomBudget();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _store.SaveBudgetAsync(budget));
        await using var db = await _factory.CreateDbContextAsync();
        Assert.Empty(await db.Budgets.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task CustomBudget_RejectsOverlappingPeriodsBeforeCommit()
    {
        var budget = NewCustomBudget();
        budget.Periods.Add(Period(budget.Id, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 20)));
        budget.Periods.Add(Period(budget.Id, new DateOnly(2026, 8, 20), new DateOnly(2026, 8, 31)));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _store.SaveBudgetAsync(budget));
        await using var db = await _factory.CreateDbContextAsync();
        Assert.Empty(await db.Budgets.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task CustomBudget_IsAbsentOutsideConfiguredWindow()
    {
        var budget = NewCustomBudget();
        budget.Periods.Add(Period(budget.Id, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31)));
        await _store.SaveBudgetAsync(budget);

        Assert.Empty(await _store.GetBudgetsAsync(new DateOnly(2026, 9, 1)));
        Assert.Empty(await new AdvancedReportService(_factory).GetBudgetPerformanceAsync(new DateOnly(2026, 9, 1)));
    }

    [Fact]
    public async Task UpdatingBudget_ReplacesExplicitPeriodsAtomically()
    {
        var budget = NewCustomBudget();
        budget.Periods.Add(Period(budget.Id, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), 1_000));
        await _store.SaveBudgetAsync(budget);

        budget.Periods.Clear();
        budget.Periods.Add(Period(budget.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), 2_000));
        await _store.SaveBudgetAsync(budget);

        Assert.Empty(await _store.GetBudgetsAsync(new DateOnly(2026, 8, 10)));
        var september = Assert.Single(await _store.GetBudgetsAsync(new DateOnly(2026, 9, 10)));
        Assert.Equal(2_000, september.PlannedMinor);
        await using var db = await _factory.CreateDbContextAsync();
        var periods = await db.BudgetPeriods.AsNoTracking().Where(x => x.BudgetId == budget.Id).ToListAsync();
        Assert.Single(periods);
        Assert.Equal(new DateOnly(2026, 9, 1), periods[0].StartsOn);
    }

    private static Budget NewCustomBudget() => new()
    {
        Name = "Project period",
        Kind = BudgetKind.Overall,
        Cadence = BudgetCadence.Custom,
        LimitMinor = 1_000,
        Currency = "INR"
    };

    private static BudgetPeriod Period(Guid budgetId, DateOnly start, DateOnly end, long planned = 1_000) => new()
    {
        BudgetId = budgetId,
        StartsOn = start,
        EndsOn = end,
        PlannedMinor = planned
    };
}
