using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class RestoreRecoveryJournalLinkTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-recovery-journal-link-{Guid.NewGuid():N}");
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
    public async Task LinkedJournal_FailsClosedWithoutTouchingLinkTarget()
    {
        var outside = Path.Combine(_root, "outside-journal.json");
        const string outsideText = "external-journal-content";
        await File.WriteAllTextAsync(outside, outsideText);
        var journal = Path.Combine(_root, "finora-restore-recovery.json");

        try
        {
            File.CreateSymbolicLink(journal, outside);
        }
        catch (Exception exception) when (exception is PlatformNotSupportedException or UnauthorizedAccessException or IOException)
        {
            return;
        }

        var result = await new RestoreRecoveryService(_factory, _root).RecoverAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(outsideText, await File.ReadAllTextAsync(outside));
        Assert.True(File.Exists(journal));
    }
}
