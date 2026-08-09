using Finora.Shared;

namespace Finora.Application;

public sealed record StorageRecoveryReport(
    bool RecoveryWasRequired,
    bool RestoredPreviousAttachments,
    bool FinalizedCommittedRestore,
    int CleanupItemsRemoved);

public interface IStorageRecoveryService
{
    Task<Result<StorageRecoveryReport>> RecoverAsync(CancellationToken cancellationToken = default);
}
