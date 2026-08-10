using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class ReportConsistencyTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-report-consistency-{Guid.NewGuid():N}");
    private FinanceStoreTests.TestFactory _factory = null!;
    private FinanceStore _store = null!;
    private Account _account = null!;
    private readonly DateOnly _periodDate = new(2026, 8, 10);

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
    public async Task CategorySpending_UsesSplitCategoriesInsteadOfParentCategory()
    {
        var food = new Category { Name = "Food", Icon = "food" };
        var transport = new Category { Name = "Transport", Icon = "transport" };
        await _store.SaveCategoryAsync(food);
        await _store.SaveCategoryAsync(transport);
        var transaction = new FinanceTransaction
        {
            Type = TransactionType.Expense,
            AmountMinor = -1_000,
            Currency = "INR",
            AccountId = _account.Id,
            CategoryId = food.Id,
            OccurredAtUtc = Utc(_periodDate),
            Splits =
            [
                new TransactionSplit { CategoryId = food.Id, AmountMinor = -700 },
                new TransactionSplit { CategoryId = transport.Id, AmountMinor = -300 }
            ]
        };
        await _store.SaveTransactionAsync(transaction);
        var reports = new AdvancedReportService(_factory);

        var series = await reports.GetCategorySpendingAsync(Utc(new DateOnly(2026, 8, 1)), Utc(new DateOnly(2026, 9, 1)), "INR");

        Assert.Equal(700, series.Points.Single(x => x.Label == "Food").ValueMinor);
        Assert.Equal(300, series.Points.Single(x => x.Label == "Transport").ValueMinor);
    }

    [Fact]
    public async Task CategoryBudget_IncludesNestedDescendantSplitSpending()
    {
        var root = new Category { Name = "Living", Icon = "home" };
        await _store.SaveCategoryAsync(root);
        var child = new Category { Name = "Food", Icon = "food", ParentId = root.Id };
        await _store.SaveCategoryAsync(child);
        var grandchild = new Category { Name = "Groceries", Icon = "basket", ParentId = child.Id };
        await _store.SaveCategoryAsync(grandchild);
        var other = new Category { Name = "Other", Icon = "tag" };
        await _store.SaveCategoryAsync(other);

        var transaction = new FinanceTransaction
        {
            Type = TransactionType.Expense,
            AmountMinor = -1_000,
            Currency = "INR",
            AccountId = _account.Id,
            OccurredAtUtc = Utc(_periodDate),
            Splits =
            [
                new TransactionSplit { CategoryId = grandchild.Id, AmountMinor = -700 },
                new TransactionSplit { CategoryId = other.Id, AmountMinor = -300 }
            ]
        };
        await _store.SaveTransactionAsync(transaction);
        var budget = new Budget
        {
            Name = "Living plan",
            Kind = BudgetKind.Category,
            Cadence = BudgetCadence.Monthly,
            CategoryId = root.Id,
            LimitMinor = 5_000,
            Currency = "INR"
        };
        await _store.SaveBudgetAsync(budget);

        var item = Assert.Single(await new AdvancedReportService(_factory).GetBudgetPerformanceAsync(_periodDate));

        Assert.Equal(700, item.ActualMinor);
        Assert.Equal(4_300, item.VarianceMinor);
    }

    [Fact]
    public async Task DisabledRollover_DoesNotChangeExplicitPeriodPlan()
    {
        var budget = new Budget
        {
            Name = "Monthly plan",
            Kind = BudgetKind.Overall,
            Cadence = BudgetCadence.Monthly,
            LimitMinor = 1_000,
            Currency = "INR",
            RolloverEnabled = false
        };
        budget.Periods.Add(new BudgetPeriod
        {
            BudgetId = budget.Id,
            StartsOn = new DateOnly(2026, 8, 1),
            EndsOn = new DateOnly(2026, 8, 31),
            PlannedMinor = 1_000,
            RolloverMinor = 500
        });
        await _store.SaveBudgetAsync(budget);

        var item = Assert.Single(await new AdvancedReportService(_factory).GetBudgetPerformanceAsync(_periodDate));

        Assert.Equal(1_000, item.PlannedMinor);
    }

    [Fact]
    public async Task EnabledRollover_AddsExplicitPeriodRollover()
    {
        var budget = new Budget
        {
            Name = "Monthly plan",
            Kind = BudgetKind.Overall,
            Cadence = BudgetCadence.Monthly,
            LimitMinor = 1_000,
            Currency = "INR",
            RolloverEnabled = true
        };
        budget.Periods.Add(new BudgetPeriod
        {
            BudgetId = budget.Id,
            StartsOn = new DateOnly(2026, 8, 1),
            EndsOn = new DateOnly(2026, 8, 31),
            PlannedMinor = 1_000,
            RolloverMinor = 500
        });
        await _store.SaveBudgetAsync(budget);

        var item = Assert.Single(await new AdvancedReportService(_factory).GetBudgetPerformanceAsync(_periodDate));

        Assert.Equal(1_500, item.PlannedMinor);
    }

    [Fact]
    public async Task InvalidReportRange_IsRejected()
    {
        var reports = new AdvancedReportService(_factory);
        var date = DateTimeOffset.UtcNow;

        await Assert.ThrowsAsync<ArgumentException>(() => reports.GetIncomeExpenseAsync(date, date, "INR"));
    }

    private static DateTimeOffset Utc(DateOnly date)
        => new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
}
