using Finora.Application;
using Finora.Domain;
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
    private string AttachmentRoot => Path.Combine(_appDataRoot, "attachments");

    public async Task<Result<StorageRecoveryReport>> RecoverAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var state = await _journal.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (state is null)
            {
                var cleaned = await RemoveOrphanCommitMarkerAsync(cancellationToken).ConfigureAwait(false);
                return Result<StorageRecoveryReport>.Success(new StorageRecoveryReport(false, false, false, cleaned));
            }

            await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var marker = await db.AppSettings.AsNoTracking()
                .SingleOrDefaultAsync(setting => setting.Key == CommitMarkerKey, cancellationToken)
                .ConfigureAwait(false);
            var databaseCommitted = marker is not null && string.Equals(marker.Value, state.RestoreId, StringComparison.Ordinal);

            var staged = _journal.ResolveStagedDirectory(state);
            var rollback = _journal.ResolveRollbackDirectory(state);
            var cleanupCount = 0;

            if (databaseCommitted)
            {
                cleanupCount += DeleteDirectoryIfPresent(staged);
                cleanupCount += DeleteDirectoryIfPresent(rollback);
                _journal.Delete();
                await RemoveCommitMarkerAsync(db, cancellationToken).ConfigureAwait(false);
                return Result<StorageRecoveryReport>.Success(new StorageRecoveryReport(true, false, true, cleanupCount));
            }

            var restoredPreviousAttachments = false;
            if (state.HadLiveAttachmentRoot)
            {
                if (Directory.Exists(rollback))
                {
                    cleanupCount += DeleteDirectoryIfPresent(AttachmentRoot);
                    Directory.Move(rollback, AttachmentRoot);
                    restoredPreviousAttachments = true;
                }
                else if (!Directory.Exists(AttachmentRoot))
                {
                    return Result<StorageRecoveryReport>.Failure("An interrupted restore could not locate either the previous receipt directory or its rollback copy. The recovery journal was preserved for manual repair.");
                }
                // When the rollback directory does not exist but the live directory does,
                // the crash occurred before the first swap. The existing live directory is
                // still the pre-restore copy and must remain untouched.
            }
            else
            {
                cleanupCount += DeleteDirectoryIfPresent(AttachmentRoot);
                cleanupCount += DeleteDirectoryIfPresent(rollback);
            }

            cleanupCount += DeleteDirectoryIfPresent(staged);
            _journal.Delete();
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

    private static int DeleteDirectoryIfPresent(string path)
    {
        if (!Directory.Exists(path)) return 0;
        Directory.Delete(path, recursive: true);
        return 1;
    }
}
