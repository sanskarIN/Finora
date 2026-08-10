using Finora.Infrastructure;

namespace Finora.IntegrationTests;

public sealed class PrivacyLoggerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-privacy-log-{Guid.NewGuid():N}");

    public PrivacyLoggerTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try { Directory.Delete(_root, true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    [Fact]
    public async Task Logger_NeverSerializesExceptionMessageOrCallerProperties()
    {
        var logger = new PrivacyLogger(_root);
        const string propertySecret = "merchant-secret-123";
        const string exceptionSecret = "C:\\private\\finance\\receipt.pdf";

        logger.Information("Dashboard Refresh / user", new Dictionary<string, object?>
        {
            ["merchant"] = propertySecret,
            ["amount"] = 999_999L
        });
        logger.Error(new InvalidOperationException(exceptionSecret), "Reports Export / failed");

        var path = await logger.ExportSanitizedLogAsync();
        var text = await WaitForContentAsync(path, expectedMinimumLines: 2);

        Assert.DoesNotContain(propertySecret, text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(exceptionSecret, text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("999999", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DashboardRefreshuser", text, StringComparison.Ordinal);
        Assert.Contains("ReportsExportfailed.InvalidOperationException", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Logger_RotatesBoundedCurrentLogBeforeNextWrite()
    {
        var current = Path.Combine(_root, "finora-diagnostic.log");
        var previous = Path.Combine(_root, "finora-diagnostic.previous.log");
        await File.WriteAllTextAsync(current, new string('x', 512 * 1024));
        var logger = new PrivacyLogger(_root);

        logger.Information("RotationCheck");

        await WaitUntilAsync(() => File.Exists(previous) && File.Exists(current) && new FileInfo(current).Length < 8_192);
        var currentText = await File.ReadAllTextAsync(current);
        Assert.Contains("RotationCheck", currentText, StringComparison.Ordinal);
        Assert.True(new FileInfo(previous).Length >= 512 * 1024);
    }

    [Fact]
    public async Task Logger_RefusesLinkedDiagnosticFile_WhenSymbolicLinksAreSupported()
    {
        var outside = Path.Combine(_root, "outside.log");
        await File.WriteAllTextAsync(outside, "outside-original");
        var linked = Path.Combine(_root, "finora-diagnostic.log");
        try
        {
            File.CreateSymbolicLink(linked, outside);
        }
        catch (Exception exception) when (exception is PlatformNotSupportedException or UnauthorizedAccessException or IOException)
        {
            return;
        }

        var logger = new PrivacyLogger(_root);
        logger.Information("MustNotFollowLink");
        await Task.Delay(100);

        Assert.Equal("outside-original", await File.ReadAllTextAsync(outside));
        await Assert.ThrowsAsync<InvalidDataException>(() => logger.ExportSanitizedLogAsync());
    }

    private static async Task<string> WaitForContentAsync(string path, int expectedMinimumLines)
    {
        string text = string.Empty;
        await WaitUntilAsync(async () =>
        {
            if (!File.Exists(path)) return false;
            text = await File.ReadAllTextAsync(path);
            return text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length >= expectedMinimumLines;
        });
        return text;
    }

    private static async Task WaitUntilAsync(Func<bool> predicate)
        => await WaitUntilAsync(() => Task.FromResult(predicate()));

    private static async Task WaitUntilAsync(Func<Task<bool>> predicate)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await predicate()) return;
            await Task.Delay(25);
        }
        Assert.Fail("Timed out waiting for asynchronous diagnostic write.");
    }
}
