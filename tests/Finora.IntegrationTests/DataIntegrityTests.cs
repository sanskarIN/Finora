using Finora.Application;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class DataIntegrityTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-integrity-{Guid.NewGuid():N}");
    private FinanceStoreTests.TestFactory _factory = null!;
    private FinanceStore _store = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        var databasePath = Path.Combine(_root, "finora.db");
        var options = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseSqlite($"Data Source={databasePath}")
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
    public async Task CheckAsync_HealthyDatabase_ReturnsHealthyReportWithoutPrivateContents()
    {
        var bank = new Account { Name = "Private Bank Name", Type = AccountType.Bank, Currency = "INR", OpeningBalanceMinor = 25_000 };
        var wallet = new Account { Name = "Private Wallet Name", Type = AccountType.DigitalWallet, Currency = "INR" };
        await _store.SaveAccountAsync(bank);
        await _store.SaveAccountAsync(wallet);
        await _store.RecordTransferAsync(bank.Id, wallet.Id, 2_500, DateTimeOffset.UtcNow, "private transfer note");

        var report = await new DataIntegrityService(_factory, _root).CheckAsync();

        Assert.True(report.IsHealthy);
        Assert.True(report.DatabaseIntegrityPassed);
        Assert.True(report.ForeignKeysPassed);
        Assert.Equal(2, report.AccountsChecked);
        Assert.Equal(2, report.TransactionsChecked);
        Assert.Empty(report.Issues);
        var sanitized = report.ToSanitizedText();
        Assert.DoesNotContain("Private Bank Name", sanitized, StringComparison.Ordinal);
        Assert.DoesNotContain("Private Wallet Name", sanitized, StringComparison.Ordinal);
        Assert.DoesNotContain("private transfer note", sanitized, StringComparison.Ordinal);
        Assert.DoesNotContain("2500", sanitized, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CheckAsync_MissingTransferHalf_ReportsBrokenPair()
    {
        var bank = new Account { Name = "Bank", Type = AccountType.Bank, Currency = "INR", OpeningBalanceMinor = 10_000 };
        var wallet = new Account { Name = "Wallet", Type = AccountType.DigitalWallet, Currency = "INR" };
        await _store.SaveAccountAsync(bank);
        await _store.SaveAccountAsync(wallet);
        var transfer = await _store.RecordTransferAsync(bank.Id, wallet.Id, 1_000, DateTimeOffset.UtcNow, null);

        await using (var db = await _factory.CreateDbContextAsync())
        {
            var destination = await db.Transactions.SingleAsync(x => x.Id == transfer.DestinationTransactionId);
            db.Transactions.Remove(destination);
            await db.SaveChangesAsync();
        }

        var report = await new DataIntegrityService(_factory, _root).CheckAsync();

        Assert.False(report.IsHealthy);
        Assert.Contains(report.Issues, x => x.Code == "TRANSFER_PAIR_BROKEN" && x.Severity == IntegritySeverity.Error);
    }
}
