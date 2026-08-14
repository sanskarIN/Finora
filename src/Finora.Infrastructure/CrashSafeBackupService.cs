using Finora.Application;
using Finora.Domain;
using Finora.Shared;
using Microsoft.EntityFrameworkCore;

namespace Finora.Infrastructure;

public sealed class CrashSafeBackupService(
    IDbContextFactory<FinoraDbContext> factory,
    string appDataRoot) : IBackupService, IDisposable
{
    private readonly IDbContextFactory<FinoraDbContext> _factory = factory;
    private readonly string _appDataRoot = Path.GetFullPath(appDataRoot);
    private readonly BackupService _inner = new(factory, appDataRoot);
    private readonly RestoreRecoveryService _recovery = new(factory, appDataRoot);
    private readonly RestoreRecoveryJournal _journal = new(appDataRoot);
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private bool _disposed;
    private string AttachmentRoot => Path.GetFullPath(Path.Combine(_appDataRoot, "attachments"));

    public async Task<byte[]> CreateEncryptedBackupAsync(string password, CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureRecoveredAsync(cancellationToken).ConfigureAwait(false);
            return await _inner.CreateEncryptedBackupAsync(password, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async Task<Result<BackupPreview>> PreviewEncryptedBackupAsync(Stream backupStream, string password, CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var recovery = await _recovery.RecoverAsync(cancellationToken).ConfigureAwait(false);
            if (!recovery.IsSuccess)
                return Result<BackupPreview>.Failure(recovery.Error ?? "Finora could not recover a previous interrupted restore.");
            return await _inner.PreviewEncryptedBackupAsync(backupStream, password, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async Task<Result> RestoreEncryptedBackupAsync(Stream backupStream, string password, CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await RestoreCoreAsync(backupStream, password, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _operationGate.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private async Task<Result> RestoreCoreAsync(Stream backupStream, string password, CancellationToken cancellationToken)
    {
        var previousRecovery = await _recovery.RecoverAsync(cancellationToken).ConfigureAwait(false);
        if (!previousRecovery.IsSuccess)
            return Result.Failure(previousRecovery.Error ?? "Finora could not recover a previous interrupted restore.");

        var restoreId = Guid.NewGuid().ToString("N");
        var state = new RestoreRecoveryState(
            restoreId,
            $"attachments.restore.wrapper.{restoreId}",
            $"attachments.rollback.{restoreId}",
            Directory.Exists(AttachmentRoot),
            RollbackCopyReady: false,
            MarkerMeansPending: true,
            DateTimeOffset.UtcNow);

        try
        {
            PathSafety.EnsureNotLinkIfExists(AttachmentRoot, "Finora receipt storage cannot be a symbolic link or reparse point during restore.");
            await SetPendingMarkerAsync(restoreId, cancellationToken).ConfigureAwait(false);
            await _journal.WriteAsync(state, cancellationToken).ConfigureAwait(false);

            if (state.HadLiveAttachmentRoot)
            {
                var rollback = _journal.ResolveRollbackDirectory(state);
                await CopyDirectoryAsync(AttachmentRoot, rollback, cancellationToken).ConfigureAwait(false);
            }

            state = state with { RollbackCopyReady = true };
            await _journal.WriteAsync(state, cancellationToken).ConfigureAwait(false);

            var result = await _inner.RestoreEncryptedBackupAsync(backupStream, password, cancellationToken).ConfigureAwait(false);
            var finalRecovery = await _recovery.RecoverAsync(CancellationToken.None).ConfigureAwait(false);
            if (!finalRecovery.IsSuccess)
                return Result.Failure(finalRecovery.Error ?? "Restore finished but Finora could not finalize crash recovery safely.");
            return result;
        }
        catch (OperationCanceledException)
        {
            await _recovery.RecoverAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or DbUpdateException or InvalidOperationException or ArgumentException)
        {
            var recovery = await _recovery.RecoverAsync(CancellationToken.None).ConfigureAwait(false);
            return recovery.IsSuccess
                ? Result.Failure("Restore failed and Finora restored the previous local state.")
                : Result.Failure(recovery.Error ?? "Restore failed and automatic recovery also failed. The recovery journal was preserved.");
        }
    }

    private async Task EnsureRecoveredAsync(CancellationToken cancellationToken)
    {
        var result = await _recovery.RecoverAsync(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error ?? "Interrupted restore recovery failed.");
    }

    private async Task SetPendingMarkerAsync(string restoreId, CancellationToken cancellationToken)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var existing = await db.AppSettings.SingleOrDefaultAsync(setting => setting.Key == RestoreRecoveryService.CommitMarkerKey, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            db.AppSettings.Add(new AppSetting { Key = RestoreRecoveryService.CommitMarkerKey, Value = restoreId });
        }
        else
        {
            existing.Value = restoreId;
            existing.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task CopyDirectoryAsync(string source, string destination, CancellationToken cancellationToken)
    {
        var sourceRoot = Path.GetFullPath(source);
        var destinationRoot = Path.GetFullPath(destination);
        if (!Directory.Exists(sourceRoot)) return;
        PathSafety.EnsureNotLinkIfExists(sourceRoot, "Receipt rollback source cannot be a symbolic link or reparse point.");
        Directory.CreateDirectory(destinationRoot);
        PathSafety.EnsureNotLinkIfExists(destinationRoot, "Receipt rollback destination cannot be a symbolic link or reparse point.");

        foreach (var file in PathSafety.EnumerateFilesWithoutLinks(sourceRoot, "Receipt rollback encountered a linked or escaped path."))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(sourceRoot, file);
            var target = PathSafety.ResolveDescendantWithoutLinks(destinationRoot, relative, "Receipt rollback destination path is invalid.");
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            await using var input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await input.CopyToAsync(output, 81920, cancellationToken).ConfigureAwait(false);
            await output.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
