using System.Globalization;
using Finora.Application;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class CurrencyPrecisionWorkflowTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-currency-workflow-{Guid.NewGuid():N}");
    private FinanceStoreTests.TestFactory _factory = null!;
    private FinanceStore _store = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        var options = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_root, "finora.db")}")
            .Options;
        _factory = new FinanceStoreTests.TestFactory(options);
        _store = new FinanceStore(_factory, new DatabaseInitializer(_factory), TimeZoneInfo.Utc);
        await _store.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        try { Directory.Delete(_root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        return Task.CompletedTask;
    }

    [Theory]
    [InlineData("JPY", "1234.6", 1235L)]
    [InlineData("INR", "12.345", 1235L)]
    [InlineData("KWD", "12.3456", 12346L)]
    [InlineData("CLF", "1.23456", 12346L)]
    public async Task AccountAndBudgetWorkflow_PreservesCurrencySpecificMinorUnits(string currency, string majorText, long expectedMinor)
    {
        Assert.Equal(expectedMinor, ParseMoney(majorText, currency).MinorUnits);
        var account = new Account
        {
            Name = $"{currency} budget account",
            Type = AccountType.Bank,
            Currency = currency,
            OpeningBalanceMinor = checked(expectedMinor * 10)
        };
        await _store.SaveAccountAsync(account);

        var date = new DateOnly(2026, 8, 16);
        await _store.SaveTransactionAsync(TransactionFactory.Create(
            TransactionType.Expense,
            expectedMinor,
            currency,
            account.Id,
            new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero)));
        await _store.SaveBudgetAsync(new Budget
        {
            Name = $"{currency} monthly budget",
            Kind = BudgetKind.Overall,
            Cadence = BudgetCadence.Monthly,
            LimitMinor = checked(expectedMinor * 5),
            Currency = currency
        });

        var accountSnapshot = Assert.Single(await _store.GetAccountsAsync());
        Assert.Equal(currency, accountSnapshot.Currency);
        Assert.Equal(checked(expectedMinor * 9), accountSnapshot.BalanceMinor);

        var budgetSnapshot = Assert.Single(await _store.GetBudgetsAsync(date));
        Assert.Equal(currency, budgetSnapshot.Currency);
        Assert.Equal(checked(expectedMinor * 5), budgetSnapshot.PlannedMinor);
        Assert.Equal(expectedMinor, budgetSnapshot.ActualMinor);
    }

    [Theory]
    [InlineData("JPY", "1234.6", 1235L)]
    [InlineData("INR", "12.345", 1235L)]
    [InlineData("KWD", "12.3456", 12346L)]
    [InlineData("CLF", "1.23456", 12346L)]
    public async Task SavingsWorkflow_PreservesCurrencySpecificMinorUnits(string currency, string majorText, long expectedMinor)
    {
        Assert.Equal(expectedMinor, ParseMoney(majorText, currency).MinorUnits);
        var goal = new SavingsGoal
        {
            Name = $"{currency} savings goal",
            TargetMinor = checked(expectedMinor * 10),
            StartingMinor = expectedMinor,
            Currency = currency
        };
        await _store.SaveSavingsGoalAsync(goal);
        await _store.AddGoalContributionAsync(new GoalContribution
        {
            SavingsGoalId = goal.Id,
            AmountMinor = checked(expectedMinor * 2),
            OccurredAtUtc = new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero)
        });

        var snapshot = Assert.Single(await _store.GetSavingsGoalsAsync());
        Assert.Equal(currency, snapshot.Currency);
        Assert.Equal(checked(expectedMinor * 10), snapshot.TargetMinor);
        Assert.Equal(checked(expectedMinor * 3), snapshot.CurrentMinor);
        Assert.Equal(0.3d, snapshot.Progress, 10);
    }

    [Theory]
    [InlineData("JPY", "1234.6", 1235L)]
    [InlineData("INR", "12.345", 1235L)]
    [InlineData("KWD", "12.3456", 12346L)]
    [InlineData("CLF", "1.23456", 12346L)]
    public async Task RecurringWorkflow_PreservesCurrencySpecificMinorUnits(string currency, string majorText, long expectedMinor)
    {
        Assert.Equal(expectedMinor, ParseMoney(majorText, currency).MinorUnits);
        var account = new Account { Name = $"{currency} recurring account", Type = AccountType.Bank, Currency = currency };
        await _store.SaveAccountAsync(account);
        var due = new DateOnly(2026, 8, 16);
        var rule = new RecurrenceRule
        {
            Name = $"{currency} recurring expense",
            Frequency = RecurrenceFrequency.Monthly,
            StartsOn = due,
            NextDueOn = due,
            DayOfMonth = due.Day,
            TransactionType = TransactionType.Expense,
            AmountMinor = expectedMinor,
            Currency = currency,
            AccountId = account.Id
        };
        await _store.SaveRecurrenceRuleAsync(rule);
        Assert.Equal(1, await _store.ProcessDueRecurrencesAsync(due));

        var workflow = new RecurringWorkflowService(_factory);
        var occurrence = Assert.Single(await workflow.GetOccurrencesAsync(due, due, false));
        Assert.Equal(currency, occurrence.Currency);
        Assert.Equal(expectedMinor, occurrence.AmountMinor);

        var paid = await workflow.MarkPaidAsync(occurrence.Id);
        Assert.True(paid.IsSuccess, paid.Error);
        var transaction = Assert.Single(await _store.SearchTransactionsAsync());
        Assert.Equal(currency, transaction.Currency);
        Assert.Equal(-expectedMinor, transaction.AmountMinor);
    }

    [Theory]
    [InlineData("JPY", "1234.6", 1235L)]
    [InlineData("INR", "12.345", 1235L)]
    [InlineData("KWD", "12.3456", 12346L)]
    [InlineData("CLF", "1.23456", 12346L)]
    public async Task ReconciliationWorkflow_PreservesCurrencySpecificMinorUnits(string currency, string majorText, long expectedMinor)
    {
        Assert.Equal(expectedMinor, ParseMoney(majorText, currency).MinorUnits);
        var account = new Account
        {
            Name = $"{currency} reconciliation account",
            Type = AccountType.Bank,
            Currency = currency,
            OpeningBalanceMinor = checked(expectedMinor * 10)
        };
        await _store.SaveAccountAsync(account);
        var statementDate = new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
        var statementBalance = checked(expectedMinor * 9);
        var service = new ReconciliationService(_factory);

        var preview = await service.PreviewAsync(account.Id, statementBalance, statementDate);
        Assert.True(preview.IsSuccess, preview.Error);
        Assert.Equal(currency, preview.Value!.Currency);
        Assert.Equal(-expectedMinor, preview.Value.DifferenceMinor);

        var completed = await service.CompleteAsync(account.Id, statementBalance, statementDate, true, "Synthetic precision reconciliation");
        Assert.True(completed.IsSuccess, completed.Error);
        Assert.True(completed.Value!.AdjustmentCreated);
        Assert.Equal(-expectedMinor, completed.Value.DifferenceMinor);

        var transaction = Assert.Single(await _store.SearchTransactionsAsync());
        Assert.Equal(TransactionType.Adjustment, transaction.Type);
        Assert.Equal(currency, transaction.Currency);
        Assert.Equal(-expectedMinor, transaction.AmountMinor);
        Assert.Equal(statementBalance, Assert.Single(await _store.GetAccountsAsync()).BalanceMinor);
    }

    private static Money ParseMoney(string majorText, string currency)
        => Money.FromMajorUnits(decimal.Parse(majorText, CultureInfo.InvariantCulture), currency);
}
