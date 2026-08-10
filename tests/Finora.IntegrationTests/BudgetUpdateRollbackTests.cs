using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class BudgetUpdateRollbackTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-budget-rollback-{Guid.NewGuid():N}");
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
    public async Task DuplicateReplacementPeriodIds_RollBackDeletedExistingPeriod()
    {
        var budget = new Budget
        {
            Name = "Project",
            Kind = BudgetKind.Overall,
            Cadence = BudgetCadence.Custom,
            LimitMinor = 1_000,
            Currency = "INR"
        };
        budget.Periods.Add(new BudgetPeriod
        {
            BudgetId = budget.Id,
            StartsOn = new DateOnly(2026, 8, 1),
            EndsOn = new DateOnly(2026, 8, 31),
            PlannedMinor = 1_000
        });
        await _store.SaveBudgetAsync(budget);

        var duplicateId = Guid.NewGuid();
        budget.Periods.Clear();
        budget.Periods.Add(new BudgetPeriod
        {
            Id = duplicateId,
            BudgetId = budget.Id,
            StartsOn = new DateOnly(2026, 9, 1),
            EndsOn = new DateOnly(2026, 9, 15),
            PlannedMinor = 1_000
        });
        budget.Periods.Add(new BudgetPeriod
        {
            Id = duplicateId,
            BudgetId = budget.Id,
            StartsOn = new DateOnly(2026, 9, 16),
            EndsOn = new DateOnly(2026, 9, 30),
            PlannedMinor = 1_000
        });

        await Assert.ThrowsAnyAsync<Exception>(() => _store.SaveBudgetAsync(budget));

        await using var verify = await _factory.CreateDbContextAsync();
        var persisted = await verify.BudgetPeriods.AsNoTracking().Where(x => x.BudgetId == budget.Id).ToListAsync();
        var original = Assert.Single(persisted);
        Assert.Equal(new DateOnly(2026, 8, 1), original.StartsOn);
        Assert.Equal(new DateOnly(2026, 8, 31), original.EndsOn);
        Assert.Equal(1_000, original.PlannedMinor);
    }
}
