using Finora.Application;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class ReportCurrencyIsolationTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-report-currency-{Guid.NewGuid():N}");
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
        try { Directory.Delete(_root, true); } catch (IOException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task AggregatedReports_DoNotMixUnlikeCurrencies()
    {
        var inr = new Account { Name = "INR account", Type = AccountType.Bank, Currency = "INR" };
        var usd = new Account { Name = "USD account", Type = AccountType.Bank, Currency = "USD" };
        await _store.SaveAccountAsync(inr);
        await _store.SaveAccountAsync(usd);

        var timestamp = DateTimeOffset.UtcNow.AddMinutes(-1);
        await _store.SaveTransactionAsync(TransactionFactory.Create(TransactionType.Income, 10_000, "INR", inr.Id, timestamp, merchant: "Synthetic INR income"));
        await _store.SaveTransactionAsync(TransactionFactory.Create(TransactionType.Expense, 2_500, "INR", inr.Id, timestamp, merchant: "Synthetic INR expense"));
        await _store.SaveTransactionAsync(TransactionFactory.Create(TransactionType.Income, 90_000, "USD", usd.Id, timestamp, merchant: "Synthetic USD income"));
        await _store.SaveTransactionAsync(TransactionFactory.Create(TransactionType.Expense, 50_000, "USD", usd.Id, timestamp, merchant: "Synthetic USD expense"));

        var service = new AdvancedReportService(_factory);
        var from = timestamp.AddDays(-1);
        var to = timestamp.AddDays(1);
        var inrSeries = await service.GetIncomeExpenseAsync(from, to, "INR");
        var usdSeries = await service.GetIncomeExpenseAsync(from, to, "USD");

        Assert.Equal(10_000, inrSeries.Points.Single(point => point.Label == "Income").ValueMinor);
        Assert.Equal(2_500, inrSeries.Points.Single(point => point.Label == "Expense").ValueMinor);
        Assert.Equal(90_000, usdSeries.Points.Single(point => point.Label == "Income").ValueMinor);
        Assert.Equal(50_000, usdSeries.Points.Single(point => point.Label == "Expense").ValueMinor);
    }
}
