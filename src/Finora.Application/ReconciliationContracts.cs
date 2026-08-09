using Finora.Shared;

namespace Finora.Application;

public sealed record ReconciliationPreview(Guid AccountId, string AccountName, string Currency, long BookBalanceMinor, long StatementBalanceMinor, long DifferenceMinor, DateTimeOffset StatementDateUtc);
public sealed record ReconciliationHistoryItem(Guid Id, Guid AccountId, DateTimeOffset StatementDateUtc, long BookBalanceMinor, long StatementBalanceMinor, long DifferenceMinor, bool AdjustmentCreated, Guid? AdjustmentTransactionId, string? Note, DateTimeOffset CompletedAtUtc);

public interface IReconciliationService
{
    Task<Result<ReconciliationPreview>> PreviewAsync(Guid accountId, long statementBalanceMinor, DateTimeOffset statementDateUtc, CancellationToken cancellationToken = default);
    Task<Result<ReconciliationHistoryItem>> CompleteAsync(Guid accountId, long statementBalanceMinor, DateTimeOffset statementDateUtc, bool createAdjustment, string? note, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReconciliationHistoryItem>> GetHistoryAsync(Guid accountId, CancellationToken cancellationToken = default);
}
