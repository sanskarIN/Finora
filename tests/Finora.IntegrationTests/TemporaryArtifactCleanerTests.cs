using Finora.Infrastructure;

namespace Finora.IntegrationTests;

public sealed class TemporaryArtifactCleanerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-cache-cleaner-{Guid.NewGuid():N}");

    public TemporaryArtifactCleanerTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try { Directory.Delete(_root, true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    [Fact]
    public async Task Cleanup_RemovesOnlyStaleManagedArtifacts()
    {
        var staleCsv = await CreateFileAsync("Finora-transactions-20260801-120000.csv", TimeSpan.FromDays(3));
        var stalePdf = await CreateFileAsync("Finora-transactions-20260801-120000.pdf", TimeSpan.FromDays(3));
        var staleBackup = await CreateFileAsync("Finora-20260801-120000.finora-backup", TimeSpan.FromDays(3));
        var staleIntegrity = await CreateFileAsync("Finora-integrity-20260801-120000.txt", TimeSpan.FromDays(3));
        var freshBackup = await CreateFileAsync("Finora-20260810-120000.finora-backup", TimeSpan.FromMinutes(30));
        var diagnostic = await CreateFileAsync("finora-diagnostic.log", TimeSpan.FromDays(3));
        var unrelated = await CreateFileAsync("notes.txt", TimeSpan.FromDays(3));

        var removed = await new TemporaryArtifactCleaner(_root).CleanupStaleAsync(TimeSpan.FromHours(24));

        Assert.Equal(4, removed);
        Assert.False(File.Exists(staleCsv));
        Assert.False(File.Exists(stalePdf));
        Assert.False(File.Exists(staleBackup));
        Assert.False(File.Exists(staleIntegrity));
        Assert.True(File.Exists(freshBackup));
        Assert.True(File.Exists(diagnostic));
        Assert.True(File.Exists(unrelated));
    }

    [Fact]
    public async Task Cleanup_RejectsNegativeMinimumAge()
    {
        var cleaner = new TemporaryArtifactCleaner(_root);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => cleaner.CleanupStaleAsync(TimeSpan.FromMinutes(-1)));
    }

    [Fact]
    public async Task Cleanup_DeletesManagedFileLinkWithoutDeletingTarget_WhenLinksAreSupported()
    {
        var target = Path.Combine(_root, "outside-target.bin");
        await File.WriteAllTextAsync(target, "keep-me");
        var link = Path.Combine(_root, "Finora-transactions-20260801-120000.csv");
        try
        {
            File.CreateSymbolicLink(link, target);
        }
        catch (Exception exception) when (exception is PlatformNotSupportedException or UnauthorizedAccessException or IOException)
        {
            return;
        }
        File.SetLastWriteTimeUtc(link, DateTime.UtcNow.AddDays(-3));

        await new TemporaryArtifactCleaner(_root).CleanupStaleAsync(TimeSpan.FromHours(24));

        Assert.False(File.Exists(link));
        Assert.True(File.Exists(target));
        Assert.Equal("keep-me", await File.ReadAllTextAsync(target));
    }

    private async Task<string> CreateFileAsync(string name, TimeSpan age)
    {
        var path = Path.Combine(_root, name);
        await File.WriteAllTextAsync(path, "synthetic");
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow - age);
        return path;
    }
}
