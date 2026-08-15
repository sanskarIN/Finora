using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class BackupAuthenticatedGraphTamperTests : IAsyncLifetime
{
    private const string Password = "graph-password-123";
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-backup-graph-tamper-{Guid.NewGuid():N}");
    private FinanceStoreTests.TestFactory _factory = null!;
    private FinanceStore _store = null!;
    private BackupService _backup = null!;
    private Guid _accountId;
    private Guid _transactionId;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        var options = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_root, "finora.db")}")
            .Options;
        _factory = new FinanceStoreTests.TestFactory(options);
        _store = new FinanceStore(_factory, new DatabaseInitializer(_factory));
        await _store.InitializeAsync();
        var account = new Account { Name = "Graph account", Type = AccountType.Bank, Currency = "INR" };
        await _store.SaveAccountAsync(account);
        _accountId = account.Id;

        var transaction = TransactionFactory.Create(
            TransactionType.Expense,
            1_250,
            "INR",
            account.Id,
            DateTimeOffset.UtcNow,
            merchant: "Graph test");
        await _store.SaveTransactionAsync(transaction);
        _transactionId = transaction.Id;
        _backup = new BackupService(_factory, _root);
    }

    public Task DisposeAsync()
    {
        try { Directory.Delete(_root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task AuthenticatedMissingTransactionAccount_IsRejectedWithoutMutation()
    {
        var valid = await _backup.CreateEncryptedBackupAsync(Password);
        var corrupted = BackupTestCipher.RewriteJson(valid, Password, root =>
        {
            var transactions = root["transactions"]?.AsArray()
                ?? throw new InvalidDataException("Backup fixture has no transactions array.");
            var transaction = transactions.Single()?.AsObject()
                ?? throw new InvalidDataException("Backup fixture has no transaction object.");
            transaction["accountId"] = Guid.NewGuid().ToString();
        });

        await using (var previewStream = new MemoryStream(corrupted, writable: false))
        {
            var preview = await _backup.PreviewEncryptedBackupAsync(previewStream, Password);
            Assert.False(preview.IsSuccess);
        }

        await using (var restoreStream = new MemoryStream(corrupted, writable: false))
        {
            var restore = await _backup.RestoreEncryptedBackupAsync(restoreStream, Password);
            Assert.False(restore.IsSuccess);
        }

        Assert.Contains(await _store.GetAccountsAsync(), account => account.Id == _accountId);
        Assert.Contains(await _store.SearchTransactionsAsync(), transaction => transaction.Id == _transactionId);
    }
}
