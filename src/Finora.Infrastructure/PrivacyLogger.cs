using Finora.Application;

namespace Finora.Infrastructure;

public sealed class PrivacyLogger(string directory) : IPrivacyLogger
{
    private const long MaximumLogBytes = 512 * 1024;
    private readonly string _directory = Path.GetFullPath(directory);
    private readonly string _path = Path.Combine(Path.GetFullPath(directory), "finora-diagnostic.log");
    private readonly string _previousPath = Path.Combine(Path.GetFullPath(directory), "finora-diagnostic.previous.log");
    private readonly SemaphoreSlim _gate = new(1, 1);

    public void Information(string eventName, IReadOnlyDictionary<string, object?>? properties = null)
    {
        // Intentionally ignore caller-supplied properties. This logger is designed for
        // event/type diagnostics only so private finance values cannot accidentally be serialized.
        _ = WriteAsync("INFO", SanitizeToken(eventName));
    }

    public void Error(Exception exception, string eventName)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _ = WriteAsync("ERROR", $"{SanitizeToken(eventName)}.{SanitizeToken(exception.GetType().Name)}");
    }

    public async Task<string> ExportSanitizedLogAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(_directory);
            if (!File.Exists(_path))
                await File.WriteAllTextAsync(_path, string.Empty, cancellationToken).ConfigureAwait(false);
            return _path;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task WriteAsync(string level, string eventToken)
    {
        try
        {
            Directory.CreateDirectory(_directory);
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                RotateIfNeeded();
                var line = $"{DateTimeOffset.UtcNow:O}|{level}|{eventToken}{Environment.NewLine}";
                await File.AppendAllTextAsync(_path, line).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ObjectDisposedException)
        {
            // Diagnostics must never crash or block core finance workflows.
        }
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(_path) || new FileInfo(_path).Length < MaximumLogBytes)
            return;

        try
        {
            if (File.Exists(_previousPath))
                File.Delete(_previousPath);
            File.Move(_path, _previousPath);
        }
        catch (IOException)
        {
            // A locked log is left in place. The next write attempts rotation again.
        }
        catch (UnauthorizedAccessException)
        {
            // Logging remains best-effort and must not affect finance operations.
        }
    }

    private static string SanitizeToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "event";

        var safe = new string(value
            .Where(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-')
            .Take(100)
            .ToArray());
        return string.IsNullOrEmpty(safe) ? "event" : safe;
    }
}
