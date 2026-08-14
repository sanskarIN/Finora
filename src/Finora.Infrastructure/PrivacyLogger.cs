using Finora.Application;

namespace Finora.Infrastructure;

public sealed class PrivacyLogger(string directory) : IPrivacyLogger, IDisposable
{
    private const long MaximumLogBytes = 512 * 1024;
    private const string CurrentFileName = "finora-diagnostic.log";
    private const string PreviousFileName = "finora-diagnostic.previous.log";
    private readonly string _directory = Path.GetFullPath(directory);
    private readonly string _path = Path.Combine(Path.GetFullPath(directory), CurrentFileName);
    private readonly string _previousPath = Path.Combine(Path.GetFullPath(directory), PreviousFileName);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    public void Information(string eventName, IReadOnlyDictionary<string, object?>? properties = null)
    {
        // Caller properties are intentionally ignored. Diagnostics contain only bounded
        // event/type tokens so finance values cannot accidentally be serialized.
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
            EnsureSafeStorage();
            if (!File.Exists(_path))
                await File.WriteAllTextAsync(_path, string.Empty, cancellationToken).ConfigureAwait(false);
            PathSafety.EnsureNotLinkIfExists(_path, "Diagnostic log cannot be a symbolic link or reparse point.");
            return _path;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _gate.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private async Task WriteAsync(string level, string eventToken)
    {
        try
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                EnsureSafeStorage();
                RotateIfNeeded();
                var line = $"{DateTimeOffset.UtcNow:O}|{level}|{eventToken}{Environment.NewLine}";
                await File.AppendAllTextAsync(_path, line).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ObjectDisposedException or InvalidDataException)
        {
            // Diagnostics must never crash or block core finance workflows.
        }
    }

    private void EnsureSafeStorage()
    {
        Directory.CreateDirectory(_directory);
        PathSafety.EnsureNotLinkIfExists(_directory, "Diagnostic directory cannot be a symbolic link or reparse point.");
        PathSafety.EnsureNoLinkTraversal(_directory, _path, "Diagnostic log path is invalid.");
        PathSafety.EnsureNoLinkTraversal(_directory, _previousPath, "Previous diagnostic log path is invalid.");
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(_path) || new FileInfo(_path).Length < MaximumLogBytes)
            return;

        try
        {
            PathSafety.EnsureNotLinkIfExists(_path, "Diagnostic log cannot be a symbolic link or reparse point.");
            PathSafety.EnsureNotLinkIfExists(_previousPath, "Previous diagnostic log cannot be a symbolic link or reparse point.");
            if (File.Exists(_previousPath)) File.Delete(_previousPath);
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
        catch (InvalidDataException)
        {
            // Linked/tampered diagnostic paths are never followed.
        }
    }

    private static string SanitizeToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "event";
        var safe = new string(value
            .Where(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-')
            .Take(100)
            .ToArray());
        return string.IsNullOrEmpty(safe) ? "event" : safe;
    }
}
