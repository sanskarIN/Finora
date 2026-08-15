using Finora.Application;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class IntegritySplitTotalRegressionTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-integrity-split-{Guid.NewGuid():N}");
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
    public async Task IntegrityCheck_DetectsSplitTotalDriftAfterStorageCorruption()
    {
        var account = new Account { Name = "Split account", Type = AccountType.Bank, Currency = "INR" };
        await _store.SaveAccountAsync(account);
        var transaction = TransactionFactory.Create(TransactionType.Expense, 1_000, "INR", account.Id, DateTimeOffset.UtcNow);
        await _store.SaveTransactionAsync(transaction);

        var first = new TransactionSplit { TransactionId = transaction.Id, AmountMinor = -400 };
        var second = new TransactionSplit { TransactionId = transaction.Id, AmountMinor = -600 };
        await using (var db = await _factory.CreateDbContextAsync())
        {
            db.TransactionSplits.AddRange(first, second);
            await db.SaveChangesAsync();
            await db.TransactionSplits.Where(split => split.Id == second.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(split => split.AmountMinor, -500L));
        }

        var report = await new DataIntegrityService(_factory, _root).CheckAsync();

        Assert.False(report.IsHealthy);
        Assert.Contains(report.Issues, issue =>
            issue.Code == "TRANSACTION_SPLIT_TOTAL" &&
            issue.Severity == IntegritySeverity.Error &&
            issue.AffectedRecords == 1);
    }
}
