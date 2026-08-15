using System.Text.Json;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class RestoreRecoveryRollbackLinkTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-recovery-rollback-link-{Guid.NewGuid():N}");
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
        try { Directory.Delete(_root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task LinkedRollbackCopy_FailsClosedAndPreservesLiveTreeAndRecoveryState()
    {
        var restoreId = Guid.NewGuid().ToString("N");
        var live = Path.Combine(_root, "attachments");
        Directory.CreateDirectory(live);
        await File.WriteAllTextAsync(Path.Combine(live, "receipt.bin"), "current-live");

        var outside = Path.Combine(_root, "outside-rollback");
        Directory.CreateDirectory(outside);
        await File.WriteAllTextAsync(Path.Combine(outside, "receipt.bin"), "outside-data");
        var rollbackName = $"attachments.rollback.{restoreId}";
        var rollback = Path.Combine(_root, rollbackName);
        try
        {
            Directory.CreateSymbolicLink(rollback, outside);
        }
        catch (Exception exception) when (exception is PlatformNotSupportedException or UnauthorizedAccessException or IOException)
        {
            return;
        }

        await using (var db = await _factory.CreateDbContextAsync())
        {
            db.AppSettings.Add(new AppSetting { Key = RestoreRecoveryService.CommitMarkerKey, Value = restoreId });
            await db.SaveChangesAsync();
        }

        var journalPayload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            restoreId,
            stagedDirectoryName = $"attachments.restore.wrapper.{restoreId}",
            rollbackDirectoryName = rollbackName,
            hadLiveAttachmentRoot = true,
            rollbackCopyReady = true,
            markerMeansPending = true,
            createdAtUtc = DateTimeOffset.UtcNow
        }, Json);
        var journal = Path.Combine(_root, "finora-restore-recovery.json");
        await File.WriteAllBytesAsync(journal, journalPayload);

        var result = await new RestoreRecoveryService(_factory, _root).RecoverAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("current-live", await File.ReadAllTextAsync(Path.Combine(live, "receipt.bin")));
        Assert.Equal("outside-data", await File.ReadAllTextAsync(Path.Combine(outside, "receipt.bin")));
        Assert.True(File.Exists(journal));
        await using var verify = await _factory.CreateDbContextAsync();
        Assert.True(await verify.AppSettings.AnyAsync(setting =>
            setting.Key == RestoreRecoveryService.CommitMarkerKey && setting.Value == restoreId));
    }
}
