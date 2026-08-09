using Finora.Shared;

namespace Finora.Application;

public sealed record AttachmentInfo(Guid Id, Guid TransactionId, string FileName, string ContentType, long SizeBytes, string Sha256Hex, DateTimeOffset CreatedAtUtc);

public interface IAttachmentService
{
    Task<IReadOnlyList<AttachmentInfo>> GetAttachmentsAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task<Result<AttachmentInfo>> AddAttachmentAsync(Guid transactionId, Stream source, string originalFileName, string contentType, CancellationToken cancellationToken = default);
    Task<Result<string>> GetLocalPathAsync(Guid attachmentId, CancellationToken cancellationToken = default);
    Task<Result> DeleteAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken = default);
    Task<long> GetStorageUsageBytesAsync(CancellationToken cancellationToken = default);
    Task<int> CleanupOrphanedFilesAsync(CancellationToken cancellationToken = default);
}
