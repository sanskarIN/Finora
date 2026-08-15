using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class BackupTruncationTests : IAsyncLifetime
{
    private const string Password = "truncation-password-123";
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-backup-truncated-{Guid.NewGuid():N}");
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
        var account = new Account { Name = "Truncation account", Type = AccountType.Cash, Currency = "INR" };
        await _store.SaveAccountAsync(account);
        _accountId = account.Id;
        _backup = new BackupService(_factory, _root);
    }

    public Task DisposeAsync()
    {
        try { Directory.Delete(_root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        return Task.CompletedTask;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(32)]
    [InlineData(64)]
    public async Task TruncatedBackup_IsRejectedWithoutMutatingCurrentData(int bytesToKeep)
    {
        var valid = await _backup.CreateEncryptedBackupAsync(Password);
        var keep = Math.Min(bytesToKeep, valid.Length - 1);
        var truncated = valid.AsSpan(0, keep).ToArray();

        await using (var previewStream = new MemoryStream(truncated, writable: false))
        {
            var preview = await _backup.PreviewEncryptedBackupAsync(previewStream, Password);
            Assert.False(preview.IsSuccess);
        }

        await using (var restoreStream = new MemoryStream(truncated, writable: false))
        {
            var restore = await _backup.RestoreEncryptedBackupAsync(restoreStream, Password);
            Assert.False(restore.IsSuccess);
        }

        Assert.Contains(await _store.GetAccountsAsync(), account => account.Id == _accountId);
    }
}
