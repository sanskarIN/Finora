using System.Text;
using Finora.Application;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class AdvancedFeatureTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-advanced-{Guid.NewGuid():N}");
    private FinanceStoreTests.TestFactory _factory = null!;
    private FinanceStore _store = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        var options = new DbContextOptionsBuilder<FinoraDbContext>().UseSqlite($"Data Source={Path.Combine(_root, "finora.db")}").Options;
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
    public async Task TransactionEdit_CreatesRevision_AndBulkCategorizationCreatesAnother()
    {
        var account = new Account { Name = "Bank", Type = AccountType.Bank, Currency = "INR" };
        var category = new Category { Name = "Food", Icon = "food" };
        await _store.SaveAccountAsync(account);
        await _store.SaveCategoryAsync(category);
        var tx = TransactionFactory.Create(TransactionType.Expense, 1234, "INR", account.Id, DateTimeOffset.UtcNow, merchant: "Cafe");
        await _store.SaveTransactionAsync(tx);

        var maintenance = new TransactionMaintenanceService(_factory);
        var update = await maintenance.UpdateTransactionAsync(new TransactionEditRequest(tx.Id, TransactionType.Expense, -1500, "INR", account.Id, null, tx.OccurredAtUtc, "Cafe", "edited", null, null, [], []));
        Assert.True(update.IsSuccess);
        Assert.Single(await maintenance.GetRevisionHistoryAsync(tx.Id));

        Assert.Equal(1, await maintenance.BulkCategorizeAsync([tx.Id], category.Id));
        Assert.Equal(2, (await maintenance.GetRevisionHistoryAsync(tx.Id)).Count);
    }

    [Fact]
    public async Task Reconciliation_RequiresExplicitAdjustmentForDifference()
    {
        var account = new Account { Name = "Bank", Type = AccountType.Bank, Currency = "INR", OpeningBalanceMinor = 10_000 };
        await _store.SaveAccountAsync(account);
        var service = new ReconciliationService(_factory);
        var rejected = await service.CompleteAsync(account.Id, 9_500, DateTimeOffset.UtcNow, false, "statement");
        Assert.False(rejected.IsSuccess);
        var accepted = await service.CompleteAsync(account.Id, 9_500, DateTimeOffset.UtcNow, true, "statement");
        Assert.True(accepted.IsSuccess);
        Assert.True(accepted.Value!.AdjustmentCreated);
        Assert.Equal(9_500, (await _store.GetAccountsAsync()).Single(x => x.Id == account.Id).BalanceMinor);
    }

    [Fact]
    public async Task CsvImport_UsesUserSelectedColumnMapping()
    {
        var account = new Account { Name = "Cash", Type = AccountType.Cash, Currency = "INR" };
        await _store.SaveAccountAsync(account);
        var csv = """
            When,Kind,Value,Wallet,Who
            2026-08-01,Expense,12.50,Cash,Tea Shop
            """;
        var mapping = new CsvColumnMapping("When", "Kind", "Value", "Wallet", null, null, "Who", null, null, null, null, null, null, false);
        var service = new CsvImportService(_factory);
        await using var previewStream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var preview = await service.PreviewWithMappingAsync(previewStream, "INR", mapping);
        Assert.True(preview.IsSuccess);
        Assert.Equal(1, preview.Value!.ValidRows);

        await using var importStream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var import = await service.ImportAsync(importStream, new CsvImportOptions(mapping, null, true, true, "INR"));
        Assert.True(import.IsSuccess);
        Assert.Equal(1, import.Value!.ImportedRows);
        Assert.Equal(-1250, (await _store.SearchTransactionsAsync()).Single().AmountMinor);
    }

    [Fact]
    public async Task EncryptedBackup_RoundTripsAttachmentBytes()
    {
        var account = new Account { Name = "Cash", Type = AccountType.Cash, Currency = "INR" };
        await _store.SaveAccountAsync(account);
        var tx = TransactionFactory.Create(TransactionType.Expense, 500, "INR", account.Id, DateTimeOffset.UtcNow);
        await _store.SaveTransactionAsync(tx);
        var attachments = new AttachmentService(_factory, _root);
        var original = Encoding.UTF8.GetBytes("%PDF-1.4 finora test receipt");
        await using (var stream = new MemoryStream(original))
        {
            var added = await attachments.AddAttachmentAsync(tx.Id, stream, "receipt.pdf", "application/pdf");
            Assert.True(added.IsSuccess);
        }

        var backup = new BackupService(_factory, _root);
        var bytes = await backup.CreateEncryptedBackupAsync("test-password-123");
        await _store.DeleteAllDataAsync();
        await attachments.CleanupOrphanedFilesAsync();
        await using var restore = new MemoryStream(bytes);
        var result = await backup.RestoreEncryptedBackupAsync(restore, "test-password-123");
        Assert.True(result.IsSuccess);
        var restoredInfo = Assert.Single(await attachments.GetAttachmentsAsync(tx.Id));
        var path = await attachments.GetLocalPathAsync(restoredInfo.Id);
        Assert.True(path.IsSuccess);
        Assert.Equal(original, await File.ReadAllBytesAsync(path.Value!));
    }
}
