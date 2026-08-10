using Finora.Application;
using Finora.Shared;
using Microsoft.EntityFrameworkCore;

namespace Finora.Infrastructure;

public sealed class RestoreRecoveryService(
    IDbContextFactory<FinoraDbContext> factory,
    string appDataRoot) : IStorageRecoveryService
{
    public const string CommitMarkerKey = "internal.restore.commit";

    private readonly IDbContextFactory<FinoraDbContext> _factory = factory;
    private readonly string _appDataRoot = Path.GetFullPath(appDataRoot);
    private readonly RestoreRecoveryJournal _journal = new(appDataRoot);
    private string AttachmentRoot => Path.GetFullPath(Path.Combine(_appDataRoot, "attachments"));

    public async Task<Result<StorageRecoveryReport>> RecoverAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var state = await _journal.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (state is null)
            {
                var cleaned = await RemoveOrphanCommitMarkerAsync(cancellationToken).ConfigureAwait(false);
                cleaned += CleanupOrphanRestoreDirectories();
                return Result<StorageRecoveryReport>.Success(new StorageRecoveryReport(false, false, false, cleaned));
            }

            PathSafety.EnsureNotLinkIfExists(AttachmentRoot, "Finora receipt storage cannot be a symbolic link or reparse point during recovery.");
            await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var marker = await db.AppSettings.AsNoTracking()
                .SingleOrDefaultAsync(setting => setting.Key == CommitMarkerKey, cancellationToken)
                .ConfigureAwait(false);
            var markerMatches = marker is not null && string.Equals(marker.Value, state.RestoreId, StringComparison.Ordinal);
            var databaseCommitted = state.MarkerMeansPending ? !markerMatches : markerMatches;

            var staged = _journal.ResolveStagedDirectory(state);
            var rollback = _journal.ResolveRollbackDirectory(state);
            var cleanupCount = 0;

            if (databaseCommitted)
            {
                cleanupCount += DeleteDirectoryIfPresent(staged);
                cleanupCount += DeleteDirectoryIfPresent(rollback);
                _journal.Delete();
                await RemoveCommitMarkerAsync(db, cancellationToken).ConfigureAwait(false);
                cleanupCount += CleanupOrphanRestoreDirectories();
                return Result<StorageRecoveryReport>.Success(new StorageRecoveryReport(true, false, true, cleanupCount));
            }

            var restoredPreviousAttachments = false;
            if (state.HadLiveAttachmentRoot)
            {
                if (state.RollbackCopyReady)
                {
                    if (!Directory.Exists(rollback))
                        return Result<StorageRecoveryReport>.Failure("An interrupted restore is missing its verified receipt rollback copy. The recovery journal was preserved for manual repair.");
                    PathSafety.EnsureNotLinkIfExists(rollback, "Receipt rollback copy cannot be a symbolic link or reparse point.");

                    cleanupCount += DeleteDirectoryIfPresent(AttachmentRoot);
                    Directory.Move(rollback, AttachmentRoot);
                    restoredPreviousAttachments = true;
                }
                else
                {
                    if (!Directory.Exists(AttachmentRoot))
                        return Result<StorageRecoveryReport>.Failure("An interrupted restore lost the live receipt directory before its rollback copy was ready. The recovery journal was preserved for manual repair.");
                    cleanupCount += DeleteDirectoryIfPresent(rollback);
                }
            }
            else
            {
                cleanupCount += DeleteDirectoryIfPresent(AttachmentRoot);
                cleanupCount += DeleteDirectoryIfPresent(rollback);
            }

            cleanupCount += DeleteDirectoryIfPresent(staged);
            _journal.Delete();
            await RemoveCommitMarkerAsync(db, cancellationToken).ConfigureAwait(false);
            cleanupCount += CleanupOrphanRestoreDirectories();
            return Result<StorageRecoveryReport>.Success(new StorageRecoveryReport(true, restoredPreviousAttachments, false, cleanupCount));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or DbUpdateException or InvalidOperationException)
        {
            return Result<StorageRecoveryReport>.Failure("Finora detected an interrupted restore but could not complete safe recovery automatically. The recovery journal was preserved.");
        }
    }

    private async Task<int> RemoveOrphanCommitMarkerAsync(CancellationToken cancellationToken)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var marker = await db.AppSettings.SingleOrDefaultAsync(setting => setting.Key == CommitMarkerKey, cancellationToken).ConfigureAwait(false);
        if (marker is null) return 0;
        db.AppSettings.Remove(marker);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return 1;
    }

    private static async Task RemoveCommitMarkerAsync(FinoraDbContext db, CancellationToken cancellationToken)
    {
        var marker = await db.AppSettings.SingleOrDefaultAsync(setting => setting.Key == CommitMarkerKey, cancellationToken).ConfigureAwait(false);
        if (marker is null) return;
        db.AppSettings.Remove(marker);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private int CleanupOrphanRestoreDirectories()
    {
        if (!Directory.Exists(_appDataRoot)) return 0;
        PathSafety.EnsureNotLinkIfExists(_appDataRoot, "App-private recovery root cannot be a symbolic link or reparse point.");
        var removed = 0;
        foreach (var pattern in new[] { "attachments.restore.*", "attachments.rollback.*" })
        {
            foreach (var directory in Directory.EnumerateDirectories(_appDataRoot, pattern, SearchOption.TopDirectoryOnly))
            {
                PathSafety.EnsureDescendant(_appDataRoot, directory, "Restore cleanup path escaped app-private storage.");
                removed += DeleteDirectoryIfPresent(directory);
            }
        }
        return removed;
    }

    private static int DeleteDirectoryIfPresent(string path)
    {
        if (!Directory.Exists(path)) return 0;
        if (PathSafety.IsSymbolicLink(path)) Directory.Delete(path);
        else Directory.Delete(path, recursive: true);
        return 1;
    }
}
