using Finora.Application;

namespace Finora.Infrastructure;

public sealed class TemporaryArtifactCleaner(string cacheRoot) : ITemporaryArtifactCleaner
{
    private static readonly string[] ManagedPatterns =
    [
        "Finora-transactions-*.csv",
        "Finora-transactions-*.pdf",
        "Finora-*.finora-backup",
        "Finora-integrity-*.txt"
    ];

    private readonly string _cacheRoot = Path.GetFullPath(cacheRoot);

    public Task<int> CleanupStaleAsync(TimeSpan minimumAge, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minimumAge, TimeSpan.Zero);
        if (!Directory.Exists(_cacheRoot)) return Task.FromResult(0);

        var cutoffUtc = DateTimeOffset.UtcNow - minimumAge;
        var removed = 0;
        try
        {
            PathSafety.EnsureNotLinkIfExists(_cacheRoot, "Finora cache root cannot be a symbolic link or reparse point.");
            var candidates = new HashSet<string>(PathSafety.Comparer);
            foreach (var pattern in ManagedPatterns)
            {
                foreach (var path in Directory.EnumerateFiles(_cacheRoot, pattern, SearchOption.TopDirectoryOnly))
                    candidates.Add(Path.GetFullPath(path));
            }

            foreach (var path in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    PathSafety.EnsureDescendant(_cacheRoot, path, "Temporary artifact path escaped Finora cache storage.");
                    if (File.GetLastWriteTimeUtc(path) > cutoffUtc.UtcDateTime) continue;
                    File.Delete(path); // Deletes a file symlink itself rather than following its target.
                    removed++;
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
                {
                    // Cache cleanup is best-effort and must never block finance startup.
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return Task.FromResult(removed);
        }

        return Task.FromResult(removed);
    }
}
