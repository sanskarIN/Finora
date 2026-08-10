using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class PersistenceInvariantTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-invariants-{Guid.NewGuid():N}");
    private FinanceStoreTests.TestFactory _factory = null!;
    private Guid _accountId;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        var options = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_root, "finora.db")}")
            .Options;
        _factory = new FinanceStoreTests.TestFactory(options);
        var initializer = new DatabaseInitializer(_factory);
        await initializer.InitializeAsync();

        await using var db = await _factory.CreateDbContextAsync();
        var account = new Account { Name = "Synthetic account", Type = AccountType.Bank, Currency = "inr" };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();
        _accountId = account.Id;
        Assert.Equal("INR", account.Currency);
    }

    public Task DisposeAsync()
    {
        try { Directory.Delete(_root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task DirectEfWrite_RejectsPositiveExpense()
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Transactions.Add(new FinanceTransaction
        {
            Type = TransactionType.Expense,
            AmountMinor = 100,
            Currency = "INR",
            AccountId = _accountId,
            OccurredAtUtc = DateTimeOffset.UtcNow
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DirectEfWrite_RejectsTransferWithoutPairLinkage()
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Transactions.Add(new FinanceTransaction
        {
            Type = TransactionType.Transfer,
            AmountMinor = -100,
            Currency = "INR",
            AccountId = _accountId,
            OccurredAtUtc = DateTimeOffset.UtcNow
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DirectEfWrite_RejectsInvalidCurrency()
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Transactions.Add(new FinanceTransaction
        {
            Type = TransactionType.Adjustment,
            AmountMinor = 100,
            Currency = "INR!",
            AccountId = _accountId,
            OccurredAtUtc = DateTimeOffset.UtcNow
        });

        await Assert.ThrowsAsync<ArgumentException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DirectEfWrite_NormalizesValidCurrency()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var transaction = new FinanceTransaction
        {
            Type = TransactionType.Income,
            AmountMinor = 100,
            Currency = " inr ",
            AccountId = _accountId,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        Assert.Equal("INR", transaction.Currency);
    }

    [Fact]
    public async Task DirectEfWrite_RejectsCategoryBudgetWithoutCategory()
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Budgets.Add(new Budget
        {
            Name = "Food",
            Kind = BudgetKind.Category,
            Cadence = BudgetCadence.Monthly,
            LimitMinor = 10_000,
            Currency = "INR"
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DirectEfWrite_RejectsSavingsGoalStartingAboveTarget()
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.SavingsGoals.Add(new SavingsGoal
        {
            Name = "Emergency",
            TargetMinor = 10_000,
            StartingMinor = 10_001,
            Currency = "INR"
        });

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DirectEfWrite_RejectsRecurringTransferWithoutDestination()
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.RecurrenceRules.Add(new RecurrenceRule
        {
            Name = "Move savings",
            Frequency = RecurrenceFrequency.Monthly,
            Interval = 1,
            StartsOn = new DateOnly(2026, 8, 1),
            TransactionType = TransactionType.Transfer,
            AmountMinor = 1_000,
            Currency = "INR",
            AccountId = _accountId
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DirectEfWrite_NormalizesAggregateCurrencies()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var budget = new Budget
        {
            Name = "Overall",
            Kind = BudgetKind.Overall,
            Cadence = BudgetCadence.Monthly,
            LimitMinor = 5_000,
            Currency = " usd "
        };
        var goal = new SavingsGoal
        {
            Name = "Trip",
            TargetMinor = 20_000,
            StartingMinor = 0,
            Currency = " usd "
        };
        var recurrence = new RecurrenceRule
        {
            Name = "Income",
            Frequency = RecurrenceFrequency.Monthly,
            Interval = 1,
            StartsOn = new DateOnly(2026, 8, 1),
            TransactionType = TransactionType.Income,
            AmountMinor = 1_000,
            Currency = " inr ",
            AccountId = _accountId
        };
        db.AddRange(budget, goal, recurrence);
        await db.SaveChangesAsync();

        Assert.Equal("USD", budget.Currency);
        Assert.Equal("USD", goal.Currency);
        Assert.Equal("INR", recurrence.Currency);
    }
}
