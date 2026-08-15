using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class BackupCryptographicFailureTests : IAsyncLifetime
{
    private const string Password = "correct-password-123";
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-backup-crypto-{Guid.NewGuid():N}");
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
        var account = new Account { Name = "Protected account", Type = AccountType.Bank, Currency = "INR" };
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
    public async Task WrongPassword_IsRejectedWithoutMutatingCurrentData()
    {
        var bytes = await _backup.CreateEncryptedBackupAsync(Password);

        await using (var previewStream = new MemoryStream(bytes, writable: false))
        {
            var preview = await _backup.PreviewEncryptedBackupAsync(previewStream, "wrong-password-456");
            Assert.False(preview.IsSuccess);
        }

        await using (var restoreStream = new MemoryStream(bytes, writable: false))
        {
            var restore = await _backup.RestoreEncryptedBackupAsync(restoreStream, "wrong-password-456");
            Assert.False(restore.IsSuccess);
        }

        Assert.Contains(await _store.GetAccountsAsync(), account => account.Id == _accountId);
    }

    [Fact]
    public async Task TamperedCiphertext_IsRejectedWithoutMutatingCurrentData()
    {
        var bytes = await _backup.CreateEncryptedBackupAsync(Password);
        var tampered = bytes.ToArray();
        tampered[^1] ^= 0x01;

        await using (var previewStream = new MemoryStream(tampered, writable: false))
        {
            var preview = await _backup.PreviewEncryptedBackupAsync(previewStream, Password);
            Assert.False(preview.IsSuccess);
        }

        await using (var restoreStream = new MemoryStream(tampered, writable: false))
        {
            var restore = await _backup.RestoreEncryptedBackupAsync(restoreStream, Password);
            Assert.False(restore.IsSuccess);
        }

        Assert.Contains(await _store.GetAccountsAsync(), account => account.Id == _accountId);
    }
}
