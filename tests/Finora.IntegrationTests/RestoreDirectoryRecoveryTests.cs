using Finora.Infrastructure;

namespace Finora.IntegrationTests;

public sealed class RestoreDirectoryRecoveryTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-restore-directory-{Guid.NewGuid():N}");

    public RestoreDirectoryRecoveryTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    [Fact]
    public async Task TryRestore_ReplacesPartialLiveDirectoryWithRollbackCopy()
    {
        var live = Path.Combine(_root, "attachments");
        var rollback = Path.Combine(_root, "attachments.rollback.test");
        Directory.CreateDirectory(live);
        Directory.CreateDirectory(rollback);
        await File.WriteAllTextAsync(Path.Combine(live, "partial.txt"), "partial");
        await File.WriteAllTextAsync(Path.Combine(rollback, "original.txt"), "original");
        string? rollbackState = rollback;

        var restored = RestoreDirectoryRecovery.TryRestore(live, ref rollbackState);

        Assert.True(restored);
        Assert.Null(rollbackState);
        Assert.False(Directory.Exists(rollback));
        Assert.False(File.Exists(Path.Combine(live, "partial.txt")));
        Assert.Equal("original", await File.ReadAllTextAsync(Path.Combine(live, "original.txt")));
    }

    [Fact]
    public void TryRestore_WithNoRollback_IsSuccessfulNoOp()
    {
        var live = Path.Combine(_root, "attachments");
        Directory.CreateDirectory(live);
        string? rollback = Path.Combine(_root, "missing.rollback");

        var restored = RestoreDirectoryRecovery.TryRestore(live, ref rollback);

        Assert.True(restored);
        Assert.Null(rollback);
        Assert.True(Directory.Exists(live));
    }
}
