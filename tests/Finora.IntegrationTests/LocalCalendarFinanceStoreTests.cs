using Finora.Application;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class LocalCalendarFinanceStoreTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-local-calendar-{Guid.NewGuid():N}");
    private FinanceStoreTests.TestFactory _factory = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        var options = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_root, "finora.db")}")
            .Options;
        _factory = new FinanceStoreTests.TestFactory(options);
        await new DatabaseInitializer(_factory).InitializeAsync();
    }

    public Task DisposeAsync()
    {
        try { Directory.Delete(_root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task BudgetWindow_UsesPositiveNonHourLocalBoundary()
    {
        var india = TimeZoneInfo.CreateCustomTimeZone(
            "IST-finora-test",
            TimeSpan.FromHours(5.5),
            "IST-finora-test",
            "IST-finora-test");
        var store = new FinanceStore(_factory, new DatabaseInitializer(_factory), india);
        var date = new DateOnly(2026, 8, 11);
        var account = new Account { Name = "IST cash", Type = AccountType.Cash, Currency = "INR" };
        await store.SaveAccountAsync(account);

        var budget = new Budget
        {
            Name = "IST one-day budget",
            Kind = BudgetKind.Overall,
            Cadence = BudgetCadence.Custom,
            LimitMinor = 10_000,
            Currency = "INR"
        };
        budget.Periods.Add(new BudgetPeriod
        {
            BudgetId = budget.Id,
            StartsOn = date,
            EndsOn = date,
            PlannedMinor = 10_000
        });
        await store.SaveBudgetAsync(budget);

        await store.SaveTransactionAsync(TransactionFactory.Create(
            TransactionType.Expense,
            1_000,
            "INR",
            account.Id,
            new DateTimeOffset(2026, 8, 10, 19, 0, 0, TimeSpan.Zero),
            merchant: "Inside local day"));
        await store.SaveTransactionAsync(TransactionFactory.Create(
            TransactionType.Expense,
            2_000,
            "INR",
            account.Id,
            new DateTimeOffset(2026, 8, 11, 18, 45, 0, TimeSpan.Zero),
            merchant: "Next local day"));

        var snapshot = Assert.Single(await store.GetBudgetsAsync(date));
        Assert.Equal(10_000, snapshot.PlannedMinor);
        Assert.Equal(1_000, snapshot.ActualMinor);
    }

    [Fact]
    public async Task DashboardWindow_UsesPositiveNonHourLocalBoundary()
    {
        var india = TimeZoneInfo.CreateCustomTimeZone(
            "IST-dashboard-test",
            TimeSpan.FromHours(5.5),
            "IST-dashboard-test",
            "IST-dashboard-test");
        var store = new FinanceStore(_factory, new DatabaseInitializer(_factory), india);
        var date = new DateOnly(2026, 8, 11);
        var account = new Account { Name = "IST bank", Type = AccountType.Bank, Currency = "INR" };
        await store.SaveAccountAsync(account);

        await store.SaveTransactionAsync(TransactionFactory.Create(
            TransactionType.Income,
            1_000,
            "INR",
            account.Id,
            new DateTimeOffset(2026, 8, 10, 19, 0, 0, TimeSpan.Zero),
            merchant: "Inside local day"));
        await store.SaveTransactionAsync(TransactionFactory.Create(
            TransactionType.Expense,
            200,
            "INR",
            account.Id,
            new DateTimeOffset(2026, 8, 11, 18, 45, 0, TimeSpan.Zero),
            merchant: "Next local day"));

        var dashboard = await store.GetDashboardAsync(date, date);

        Assert.Equal(1_000, dashboard.IncomeMinor);
        Assert.Equal(0, dashboard.ExpenseMinor);
        var recent = Assert.Single(dashboard.RecentTransactions);
        Assert.Equal("Inside local day", recent.Merchant);
    }

    [Fact]
    public async Task DashboardWindow_UsesDstTransitionBoundary()
    {
        var zone = CreateDstTestZone();
        var store = new FinanceStore(_factory, new DatabaseInitializer(_factory), zone);
        var date = new DateOnly(2026, 3, 8);
        var account = new Account { Name = "DST bank", Type = AccountType.Bank, Currency = "INR" };
        await store.SaveAccountAsync(account);

        await store.SaveTransactionAsync(TransactionFactory.Create(
            TransactionType.Income,
            1_500,
            "INR",
            account.Id,
            new DateTimeOffset(2026, 3, 8, 5, 30, 0, TimeSpan.Zero),
            merchant: "DST day"));
        await store.SaveTransactionAsync(TransactionFactory.Create(
            TransactionType.Expense,
            300,
            "INR",
            account.Id,
            new DateTimeOffset(2026, 3, 9, 4, 15, 0, TimeSpan.Zero),
            merchant: "After DST day"));

        var dashboard = await store.GetDashboardAsync(date, date);

        Assert.Equal(1_500, dashboard.IncomeMinor);
        Assert.Equal(0, dashboard.ExpenseMinor);
        Assert.Single(dashboard.RecentTransactions);
    }

    private static TimeZoneInfo CreateDstTestZone()
    {
        var daylightStart = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
            new DateTime(1, 1, 1, 2, 0, 0),
            3,
            2,
            DayOfWeek.Sunday);
        var daylightEnd = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
            new DateTime(1, 1, 1, 2, 0, 0),
            11,
            1,
            DayOfWeek.Sunday);
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2020, 1, 1),
            new DateTime(2030, 12, 31),
            TimeSpan.FromHours(1),
            daylightStart,
            daylightEnd);

        return TimeZoneInfo.CreateCustomTimeZone(
            "DST-finora-store-test",
            TimeSpan.FromHours(-5),
            "DST-finora-store-test",
            "DST-standard-test",
            "DST-daylight-test",
            [rule]);
    }
}
