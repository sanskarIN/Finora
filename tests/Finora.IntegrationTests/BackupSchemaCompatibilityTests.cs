using Finora.Domain;
using Finora.Infrastructure;
using Finora.Shared;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class BackupSchemaCompatibilityTests : IAsyncLifetime
{
    private const string Password = "schema-password-123";
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-backup-schema-{Guid.NewGuid():N}");
    private FinanceStoreTests.TestFactory _factory = null!;
    private FinanceStore _store = null!;
    private BackupService _backup = null!;
    private Guid _accountId;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        var options = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_root, "finora.db")}")
            .Options;
        _factory = new FinanceStoreTests.TestFactory(options);
        _store = new FinanceStore(_factory, new DatabaseInitializer(_factory));
        await _store.InitializeAsync();
        var account = new Account { Name = "Schema account", Type = AccountType.Bank, Currency = "INR" };
        await _store.SaveAccountAsync(account);
        _accountId = account.Id;
        _backup = new BackupService(_factory, _root);
    }

    public Task DisposeAsync()
    {
        try { Directory.Delete(_root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task AuthenticatedNewerSchema_IsRejectedWithoutMutatingCurrentData()
    {
        var valid = await _backup.CreateEncryptedBackupAsync(Password);
        var newer = BackupTestCipher.RewriteJson(valid, Password, root =>
            root["schemaVersion"] = AppConstants.DatabaseSchemaVersion + 1);

        await using (var previewStream = new MemoryStream(newer, writable: false))
        {
            var preview = await _backup.PreviewEncryptedBackupAsync(previewStream, Password);
            Assert.False(preview.IsSuccess);
            Assert.Contains("newer Finora schema", preview.Error, StringComparison.OrdinalIgnoreCase);
        }

        await using (var restoreStream = new MemoryStream(newer, writable: false))
        {
            var restore = await _backup.RestoreEncryptedBackupAsync(restoreStream, Password);
            Assert.False(restore.IsSuccess);
        }

        Assert.Contains(await _store.GetAccountsAsync(), account => account.Id == _accountId);
    }

    [Fact]
    public async Task AuthenticatedInvalidSchemaMarker_IsRejectedWithoutMutatingCurrentData()
    {
        var valid = await _backup.CreateEncryptedBackupAsync(Password);
        var invalid = BackupTestCipher.RewriteJson(valid, Password, root => root["schemaVersion"] = 0);

        await using (var previewStream = new MemoryStream(invalid, writable: false))
        {
            var preview = await _backup.PreviewEncryptedBackupAsync(previewStream, Password);
            Assert.False(preview.IsSuccess);
        }

        await using (var restoreStream = new MemoryStream(invalid, writable: false))
        {
            var restore = await _backup.RestoreEncryptedBackupAsync(restoreStream, Password);
            Assert.False(restore.IsSuccess);
        }

        Assert.Contains(await _store.GetAccountsAsync(), account => account.Id == _accountId);
    }
}
