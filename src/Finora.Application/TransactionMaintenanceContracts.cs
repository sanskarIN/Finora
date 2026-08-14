using Finora.Domain;
using Finora.Shared;

namespace Finora.Application;

public sealed record TransactionSplitInput(Guid? CategoryId, long AmountMinor, string? Note);
public sealed record TransactionTagInfo(Guid Id, string Name, string? ColorLabel);
public sealed record TransactionRevisionInfo(Guid Id, DateTimeOffset ChangedAtUtc, string ChangeKind, string Summary);
public sealed record TransactionDetail(Guid Id, TransactionType Type, long AmountMinor, string Currency, Guid AccountId, Guid? CategoryId, DateTimeOffset OccurredAtUtc, string? Merchant, string? Note, string? PaymentMethod, string? ManualLocation, IReadOnlyList<TransactionSplitInput> Splits, IReadOnlyList<TransactionTagInfo> Tags, IReadOnlyList<AttachmentInfo> Attachments, IReadOnlyList<TransactionRevisionInfo> Revisions, bool IsDeleted);
public sealed record TransactionEditRequest(Guid TransactionId, TransactionType Type, long AmountMinor, string Currency, Guid AccountId, Guid? CategoryId, DateTimeOffset OccurredAtUtc, string? Merchant, string? Note, string? PaymentMethod, string? ManualLocation, IReadOnlyList<TransactionSplitInput> Splits, IReadOnlyCollection<Guid> TagIds);
public sealed record DuplicateTransactionCandidate(Guid TransactionId, Guid PossibleDuplicateId, DateTimeOffset OccurredAtUtc, long AmountMinor, string Currency, string AccountName, string? Merchant, int ConfidencePercent);

public interface ITransactionMaintenanceService
{
    Task<Result<TransactionDetail>> GetTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task<Result> UpdateTransactionAsync(TransactionEditRequest request, CancellationToken cancellationToken = default);
    Task<Result> UpdateTransferAsync(Guid transactionId, long amountMinor, DateTimeOffset occurredAtUtc, string? note, CancellationToken cancellationToken = default);
    Task<int> BulkCategorizeAsync(IReadOnlyCollection<Guid> transactionIds, Guid? categoryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DuplicateTransactionCandidate>> FindLikelyDuplicatesAsync(DateTimeOffset? from = null, DateTimeOffset? through = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TransactionRevisionInfo>> GetRevisionHistoryAsync(Guid transactionId, CancellationToken cancellationToken = default);
}
