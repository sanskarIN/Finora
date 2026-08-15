using Finora.Application;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class IntegrityCurrencyRegressionTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-integrity-currency-{Guid.NewGuid():N}");
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
    public async Task IntegrityCheck_DetectsTransactionCurrencyDifferentFromAccount()
    {
        var account = new Account { Name = "Currency account", Type = AccountType.Bank, Currency = "INR" };
        await _store.SaveAccountAsync(account);
        var transaction = TransactionFactory.Create(TransactionType.Expense, 500, "INR", account.Id, DateTimeOffset.UtcNow);
        await _store.SaveTransactionAsync(transaction);

        await using (var db = await _factory.CreateDbContextAsync())
        {
            await db.Transactions.Where(row => row.Id == transaction.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.Currency, "USD"));
        }

        var report = await new DataIntegrityService(_factory, _root).CheckAsync();

        Assert.False(report.IsHealthy);
        Assert.Contains(report.Issues, issue =>
            issue.Code == "TRANSACTION_CURRENCY_MISMATCH" && issue.AffectedRecords == 1);
    }
}
