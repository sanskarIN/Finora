using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class AggregateIntegrityRegressionTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-integrity-aggregate-{Guid.NewGuid():N}");
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
    public async Task IntegrityCheck_DetectsCustomBudgetWithoutPeriod()
    {
        var budget = new Budget
        {
            Name = "Custom",
            Kind = BudgetKind.Overall,
            Cadence = BudgetCadence.Custom,
            LimitMinor = 1_000,
            Currency = "INR"
        };
        await using (var db = await _factory.CreateDbContextAsync())
        {
            db.Budgets.Add(budget);
            await db.SaveChangesAsync();
        }

        var report = await new DataIntegrityService(_factory, _root).CheckAsync();

        Assert.Contains(report.Issues, issue => issue.Code == "BUDGET_INVALID");
    }

    [Fact]
    public async Task IntegrityCheck_DetectsLinkedGoalTransactionCurrencyDrift()
    {
        var account = await CreateAccountAsync("INR");
        var transaction = new FinanceTransaction
        {
            Type = TransactionType.Income,
            AmountMinor = 500,
            Currency = "INR",
            AccountId = account.Id,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
        await _store.SaveTransactionAsync(transaction);
        var goal = new SavingsGoal { Name = "Goal", TargetMinor = 5_000, Currency = "INR" };
        await _store.SaveSavingsGoalAsync(goal);
        await _store.AddGoalContributionAsync(new GoalContribution
        {
            SavingsGoalId = goal.Id,
            AmountMinor = 500,
            OccurredAtUtc = DateTimeOffset.UtcNow,
            TransactionId = transaction.Id
        });
        await using (var db = await _factory.CreateDbContextAsync())
        {
            await db.Transactions.Where(x => x.Id == transaction.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Currency, "USD"));
        }

        var report = await new DataIntegrityService(_factory, _root).CheckAsync();

        Assert.Contains(report.Issues, issue => issue.Code == "GOAL_CONTRIBUTION_INVALID");
    }

    [Fact]
    public async Task IntegrityCheck_DetectsActiveRecurrenceOnArchivedAccount()
    {
        var account = await CreateAccountAsync("INR");
        var rule = new RecurrenceRule
        {
            Name = "Rent",
            Frequency = RecurrenceFrequency.Monthly,
            Interval = 1,
            StartsOn = new DateOnly(2026, 8, 1),
            TransactionType = TransactionType.Expense,
            AmountMinor = 1_000,
            Currency = "INR",
            AccountId = account.Id
        };
        await _store.SaveRecurrenceRuleAsync(rule);
        await using (var db = await _factory.CreateDbContextAsync())
        {
            await db.Accounts.Where(x => x.Id == account.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.State, AccountState.Archived));
        }

        var report = await new DataIntegrityService(_factory, _root).CheckAsync();

        Assert.Contains(report.Issues, issue => issue.Code == "RECURRENCE_RELATION_INVALID");
    }

    [Fact]
    public async Task IntegrityCheck_DetectsReconciliationDifferenceDrift()
    {
        var account = await CreateAccountAsync("INR");
        var reconciliation = await new ReconciliationService(_factory)
            .CompleteAsync(account.Id, 0, DateTimeOffset.UtcNow, false, null);
        Assert.True(reconciliation.IsSuccess);
        await using (var db = await _factory.CreateDbContextAsync())
        {
            await db.AccountReconciliations
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.DifferenceMinor, 1L));
        }

        var report = await new DataIntegrityService(_factory, _root).CheckAsync();

        Assert.Contains(report.Issues, issue => issue.Code == "RECONCILIATION_INVALID");
    }

    [Fact]
    public async Task IntegrityCheck_DetectsInvalidOccurrencePaymentState()
    {
        var account = await CreateAccountAsync("INR");
        var today = DateOnly.FromDateTime(DateTime.Today);
        var rule = new RecurrenceRule
        {
            Name = "Rent",
            Frequency = RecurrenceFrequency.Monthly,
            Interval = 1,
            StartsOn = today,
            NextDueOn = today,
            TransactionType = TransactionType.Expense,
            AmountMinor = 1_000,
            Currency = "INR",
            AccountId = account.Id
        };
        await _store.SaveRecurrenceRuleAsync(rule);
        Assert.Equal(1, await _store.ProcessDueRecurrencesAsync(today));
        await using (var db = await _factory.CreateDbContextAsync())
        {
            await db.RecurrenceOccurrences
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, OccurrenceStatus.Paid)
                    .SetProperty(x => x.PaidAmountMinor, 1_000L));
        }

        var report = await new DataIntegrityService(_factory, _root).CheckAsync();

        Assert.Contains(report.Issues, issue => issue.Code == "RECURRENCE_STATE_INVALID");
    }

    private async Task<Account> CreateAccountAsync(string currency)
    {
        var account = new Account { Name = $"{currency} account", Type = AccountType.Bank, Currency = currency };
        await _store.SaveAccountAsync(account);
        return account;
    }
}
