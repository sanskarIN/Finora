using System.Text;
using System.Text.Json;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class RestoreRecoveryTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-recovery-{Guid.NewGuid():N}");
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
        try { Directory.Delete(_root, true); } catch (IOException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task PendingMarker_RestoresVerifiedPreviousAttachmentTree()
    {
        var restoreId = Guid.NewGuid().ToString("N");
        var live = Path.Combine(_root, "attachments");
        var rollbackName = $"attachments.rollback.{restoreId}";
        var rollback = Path.Combine(_root, rollbackName);
        Directory.CreateDirectory(live);
        Directory.CreateDirectory(rollback);
        await File.WriteAllTextAsync(Path.Combine(live, "receipt.bin"), "new-uncommitted");
        await File.WriteAllTextAsync(Path.Combine(rollback, "receipt.bin"), "old-committed");
        await SetPendingMarkerAsync(restoreId);
        await WriteJournalAsync(restoreId, rollbackName, hadLive: true, rollbackReady: true);

        var result = await new RestoreRecoveryService(_factory, _root).RecoverAsync();

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.RestoredPreviousAttachments);
        Assert.Equal("old-committed", await File.ReadAllTextAsync(Path.Combine(live, "receipt.bin")));
        Assert.False(Directory.Exists(rollback));
        Assert.False(File.Exists(Path.Combine(_root, "finora-restore-recovery.json")));
        await AssertNoPendingMarkerAsync();
    }

    [Fact]
    public async Task MissingPendingMarker_FinalizesCommittedAttachmentTree()
    {
        var restoreId = Guid.NewGuid().ToString("N");
        var live = Path.Combine(_root, "attachments");
        var rollbackName = $"attachments.rollback.{restoreId}";
        var rollback = Path.Combine(_root, rollbackName);
        Directory.CreateDirectory(live);
        Directory.CreateDirectory(rollback);
        await File.WriteAllTextAsync(Path.Combine(live, "receipt.bin"), "new-committed");
        await File.WriteAllTextAsync(Path.Combine(rollback, "receipt.bin"), "old-copy");
        await WriteJournalAsync(restoreId, rollbackName, hadLive: true, rollbackReady: true);

        var result = await new RestoreRecoveryService(_factory, _root).RecoverAsync();

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.FinalizedCommittedRestore);
        Assert.Equal("new-committed", await File.ReadAllTextAsync(Path.Combine(live, "receipt.bin")));
        Assert.False(Directory.Exists(rollback));
        Assert.False(File.Exists(Path.Combine(_root, "finora-restore-recovery.json")));
    }

    [Fact]
    public async Task IncompleteRollbackCopy_KeepsUntouchedLiveTree()
    {
        var restoreId = Guid.NewGuid().ToString("N");
        var live = Path.Combine(_root, "attachments");
        var rollbackName = $"attachments.rollback.{restoreId}";
        var rollback = Path.Combine(_root, rollbackName);
        Directory.CreateDirectory(live);
        Directory.CreateDirectory(rollback);
        await File.WriteAllTextAsync(Path.Combine(live, "receipt.bin"), "old-live");
        await File.WriteAllTextAsync(Path.Combine(rollback, "partial.bin"), "partial-copy");
        await SetPendingMarkerAsync(restoreId);
        await WriteJournalAsync(restoreId, rollbackName, hadLive: true, rollbackReady: false);

        var result = await new RestoreRecoveryService(_factory, _root).RecoverAsync();

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.RestoredPreviousAttachments);
        Assert.Equal("old-live", await File.ReadAllTextAsync(Path.Combine(live, "receipt.bin")));
        Assert.False(Directory.Exists(rollback));
        await AssertNoPendingMarkerAsync();
    }

    [Fact]
    public async Task CrashSafeBackup_RoundTripLeavesNoRecoveryArtifacts()
    {
        var store = new FinanceStore(_factory, new DatabaseInitializer(_factory));
        var account = new Account { Name = "Synthetic cash", Type = AccountType.Cash, Currency = "INR" };
        await store.SaveAccountAsync(account);
        var service = new CrashSafeBackupService(_factory, _root);
        var bytes = await service.CreateEncryptedBackupAsync("synthetic-password-123");

        await using var stream = new MemoryStream(bytes);
        var result = await service.RestoreEncryptedBackupAsync(stream, "synthetic-password-123");

        Assert.True(result.IsSuccess);
        Assert.False(File.Exists(Path.Combine(_root, "finora-restore-recovery.json")));
        await AssertNoPendingMarkerAsync();
    }

    private async Task SetPendingMarkerAsync(string restoreId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.AppSettings.Add(new AppSetting { Key = RestoreRecoveryService.CommitMarkerKey, Value = restoreId });
        await db.SaveChangesAsync();
    }

    private async Task AssertNoPendingMarkerAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        Assert.False(await db.AppSettings.AnyAsync(setting => setting.Key == RestoreRecoveryService.CommitMarkerKey));
    }

    private async Task WriteJournalAsync(string restoreId, string rollbackName, bool hadLive, bool rollbackReady)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            restoreId,
            stagedDirectoryName = $"attachments.restore.wrapper.{restoreId}",
            rollbackDirectoryName = rollbackName,
            hadLiveAttachmentRoot = hadLive,
            rollbackCopyReady = rollbackReady,
            markerMeansPending = true,
            createdAtUtc = DateTimeOffset.UtcNow
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await File.WriteAllBytesAsync(Path.Combine(_root, "finora-restore-recovery.json"), payload);
    }
}
