using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class AdvancedReportCoverageTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-report-coverage-{Guid.NewGuid():N}");
    private FinanceStoreTests.TestFactory _factory = null!;
    private FinanceStore _store = null!;
    private Account _account = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        var options = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_root, "finora.db")}")
            .Options;
        _factory = new FinanceStoreTests.TestFactory(options);
        _store = new FinanceStore(_factory, new DatabaseInitializer(_factory));
        await _store.InitializeAsync();
        _account = new Account { Name = "Bank", Type = AccountType.Bank, Currency = "INR" };
        await _store.SaveAccountAsync(_account);
    }

    public Task DisposeAsync()
    {
        try { Directory.Delete(_root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task RecurringObligations_ReportKeepsCurrencyTypeStatusAndDueDate()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var active = new RecurrenceRule
        {
            Name = "Rent",
            Frequency = RecurrenceFrequency.Monthly,
            Interval = 1,
            StartsOn = today,
            NextDueOn = today.AddDays(5),
            TransactionType = TransactionType.Expense,
            AmountMinor = 25_000,
            Currency = "INR",
            AccountId = _account.Id,
            Status = RecurrenceStatus.Active
        };
        var paused = new RecurrenceRule
        {
            Name = "Subscription",
            Frequency = RecurrenceFrequency.Monthly,
            Interval = 1,
            StartsOn = today,
            NextDueOn = today.AddDays(10),
            TransactionType = TransactionType.Expense,
            AmountMinor = 499,
            Currency = "INR",
            AccountId = _account.Id,
            Status = RecurrenceStatus.Paused
        };
        await _store.SaveRecurrenceRuleAsync(active);
        await _store.SaveRecurrenceRuleAsync(paused);

        var rows = await new AdvancedReportService(_factory).GetRecurringObligationsAsync();

        Assert.Equal(2, rows.Count);
        var rent = Assert.Single(rows, x => x.RuleId == active.Id);
        Assert.Equal(TransactionType.Expense, rent.Type);
        Assert.Equal(RecurrenceStatus.Active, rent.Status);
        Assert.Equal(25_000, rent.AmountMinor);
        Assert.Equal("INR", rent.Currency);
        Assert.Equal(today.AddDays(5), rent.NextDueOn);
    }

    [Fact]
    public async Task SavingsProgress_ReportUsesCheckedContributionHistory()
    {
        var goal = new SavingsGoal
        {
            Name = "Emergency fund",
            TargetMinor = 2_000,
            StartingMinor = 1_000,
            Currency = "INR",
            TargetDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(6))
        };
        await _store.SaveSavingsGoalAsync(goal);
        await _store.AddGoalContributionAsync(new GoalContribution
        {
            SavingsGoalId = goal.Id,
            AmountMinor = 500,
            OccurredAtUtc = DateTimeOffset.UtcNow
        });

        var row = Assert.Single(await new AdvancedReportService(_factory).GetSavingsProgressAsync());

        Assert.Equal(1_500, row.CurrentMinor);
        Assert.Equal(2_000, row.TargetMinor);
        Assert.Equal("INR", row.Currency);
        Assert.InRange(row.Progress, 0.749d, 0.751d);
        Assert.False(row.IsCompleted);
    }

    [Fact]
    public async Task YearlyComparison_SeparatesCurrentAndPreviousCalendarYears()
    {
        var today = DateTime.Today;
        var currentYear = today.Year;
        await _store.SaveTransactionAsync(new FinanceTransaction
        {
            Type = TransactionType.Income,
            AmountMinor = 10_000,
            Currency = "INR",
            AccountId = _account.Id,
            OccurredAtUtc = LocalNoonUtc(today.Year, today.Month, today.Day)
        });
        await _store.SaveTransactionAsync(new FinanceTransaction
        {
            Type = TransactionType.Expense,
            AmountMinor = -4_000,
            Currency = "INR",
            AccountId = _account.Id,
            OccurredAtUtc = LocalNoonUtc(currentYear - 1, 6, 15)
        });

        var rows = await new AdvancedReportService(_factory).GetYearlyComparisonAsync(2, "INR");

        Assert.Equal(2, rows.Count);
        var previous = Assert.Single(rows, x => x.Year == currentYear - 1);
        var current = Assert.Single(rows, x => x.Year == currentYear);
        Assert.Equal(4_000, previous.ExpenseMinor);
        Assert.Equal(-4_000, previous.NetMinor);
        Assert.Equal(10_000, current.IncomeMinor);
        Assert.Equal(10_000, current.NetMinor);
    }

    [Fact]
    public async Task CurrentMonthlyAndYearlyComparisons_ExcludeFutureDatedRows()
    {
        var tomorrow = DateTime.Today.AddDays(1);
        await using (var db = await _factory.CreateDbContextAsync())
        {
            db.Transactions.Add(new FinanceTransaction
            {
                Type = TransactionType.Income,
                AmountMinor = 99_999,
                Currency = "INR",
                AccountId = _account.Id,
                OccurredAtUtc = LocalNoonUtc(tomorrow.Year, tomorrow.Month, tomorrow.Day)
            });
            await db.SaveChangesAsync();
        }

        var service = new AdvancedReportService(_factory);
        var monthly = await service.GetMonthlyComparisonAsync(1, "INR");
        var yearly = await service.GetYearlyComparisonAsync(1, "INR");

        Assert.Equal(0, Assert.Single(monthly).IncomeMinor);
        Assert.Equal(0, Assert.Single(yearly).IncomeMinor);
    }

    private static DateTimeOffset LocalNoonUtc(int year, int month, int day)
    {
        var local = new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local)).ToUniversalTime();
    }
}
