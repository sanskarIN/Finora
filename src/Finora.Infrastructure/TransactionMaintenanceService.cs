using Finora.Application;
using Finora.Domain;
using Finora.Shared;
using Microsoft.EntityFrameworkCore;

namespace Finora.Infrastructure;

public sealed class TransactionMaintenanceService(IDbContextFactory<FinoraDbContext> factory) : ITransactionMaintenanceService
{
    private readonly IDbContextFactory<FinoraDbContext> _factory = factory;

    public async Task<Result<TransactionDetail>> GetTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var transaction = await db.Transactions.AsNoTracking().Include(x => x.Splits).Include(x => x.TransactionTags).ThenInclude(x => x.Tag).Include(x => x.Attachments).SingleOrDefaultAsync(x => x.Id == transactionId, cancellationToken).ConfigureAwait(false);
        if (transaction is null) return Result<TransactionDetail>.Failure("Transaction not found.");
        var revisions = await db.TransactionRevisions.AsNoTracking().Where(x => x.TransactionId == transactionId).OrderByDescending(x => x.ChangedAtUtc).Select(x => new { x.Id, x.ChangedAtUtc, x.ChangeKind, x.SnapshotJson }).ToListAsync(cancellationToken).ConfigureAwait(false);
        var detail = new TransactionDetail(transaction.Id, transaction.Type, transaction.AmountMinor, transaction.Currency, transaction.AccountId, transaction.CategoryId, transaction.OccurredAtUtc, transaction.Merchant, transaction.Note, transaction.PaymentMethod, transaction.ManualLocation, transaction.Splits.Select(x => new TransactionSplitInput(x.CategoryId, x.AmountMinor, x.Note)).ToList(), transaction.TransactionTags.Where(x => x.Tag is not null).Select(x => new TransactionTagInfo(x.TagId, x.Tag!.Name, x.Tag.ColorLabel)).ToList(), transaction.Attachments.Select(x => new AttachmentInfo(x.Id, x.TransactionId, x.OriginalFileName, x.ContentType, x.SizeBytes, x.Sha256 is null ? string.Empty : Convert.ToHexString(x.Sha256), x.CreatedAtUtc)).ToList(), revisions.Select(x => new TransactionRevisionInfo(x.Id, x.ChangedAtUtc, x.ChangeKind, TransactionRevisionSerializer.Describe(x.SnapshotJson))).ToList(), transaction.IsDeleted);
        return Result<TransactionDetail>.Success(detail);
    }

    public async Task<Result> UpdateTransactionAsync(TransactionEditRequest request, CancellationToken cancellationToken = default)
    {
        if (request.AmountMinor == 0) return Result.Failure("Transaction amount cannot be zero.");
        if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Length is < 3 or > 8) return Result.Failure("Currency code is invalid.");
        if (request.Splits.Count > 0 && request.Splits.Sum(x => x.AmountMinor) != request.AmountMinor) return Result.Failure("Split amounts must equal the transaction amount.");
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transactionScope = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var transaction = await db.Transactions.Include(x => x.Splits).Include(x => x.TransactionTags).SingleOrDefaultAsync(x => x.Id == request.TransactionId, cancellationToken).ConfigureAwait(false);
        if (transaction is null) return Result.Failure("Transaction not found.");
        if (transaction.IsDeleted) return Result.Failure("Restore the transaction before editing it.");
        if (transaction.TransferGroupId is not null) return Result.Failure("Use the transfer editor for linked transfers.");
        if (!await db.Accounts.AnyAsync(x => x.Id == request.AccountId && x.State != AccountState.Archived, cancellationToken).ConfigureAwait(false)) return Result.Failure("The selected account is unavailable.");
        if (request.CategoryId is Guid categoryId && !await db.Categories.AnyAsync(x => x.Id == categoryId && !x.IsArchived, cancellationToken).ConfigureAwait(false)) return Result.Failure("The selected category is unavailable.");
        if (request.TagIds.Count > 0)
        {
            var activeTagCount = await db.Tags.CountAsync(x => request.TagIds.Contains(x.Id) && !x.IsArchived, cancellationToken).ConfigureAwait(false);
            if (activeTagCount != request.TagIds.Distinct().Count()) return Result.Failure("One or more selected tags are unavailable.");
        }
        db.TransactionRevisions.Add(new TransactionRevision { TransactionId = transaction.Id, ChangeKind = "BeforeEdit", SnapshotJson = TransactionRevisionSerializer.Serialize(transaction, transaction.Splits, transaction.TransactionTags.Select(x => x.TagId).ToList()), ChangedAtUtc = DateTimeOffset.UtcNow });
        transaction.Type = request.Type;
        transaction.AmountMinor = request.AmountMinor;
        transaction.Currency = request.Currency.Trim().ToUpperInvariant();
        transaction.AccountId = request.AccountId;
        transaction.CategoryId = request.CategoryId;
        transaction.OccurredAtUtc = request.OccurredAtUtc;
        transaction.Merchant = Normalize(request.Merchant, 240);
        transaction.Note = Normalize(request.Note, 2000);
        transaction.PaymentMethod = Normalize(request.PaymentMethod, 120);
        transaction.ManualLocation = Normalize(request.ManualLocation, 240);
        transaction.UpdatedAtUtc = DateTimeOffset.UtcNow;
        db.TransactionSplits.RemoveRange(transaction.Splits);
        transaction.Splits = request.Splits.Select(x => new TransactionSplit { TransactionId = transaction.Id, CategoryId = x.CategoryId, AmountMinor = x.AmountMinor, Note = Normalize(x.Note, 500) }).ToList();
        db.TransactionTags.RemoveRange(transaction.TransactionTags);
        transaction.TransactionTags = request.TagIds.Distinct().Select(tagId => new TransactionTag { TransactionId = transaction.Id, TagId = tagId }).ToList();
        db.AuditEntries.Add(new AuditEntry { EntityType = "Transaction", EntityId = transaction.Id, Action = "UpdatedWithRevision" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transactionScope.CommitAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> UpdateTransferAsync(Guid transactionId, long amountMinor, DateTimeOffset occurredAtUtc, string? note, CancellationToken cancellationToken = default)
    {
        if (amountMinor <= 0) return Result.Failure("Transfer amount must be positive.");
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transactionScope = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var selected = await db.Transactions.SingleOrDefaultAsync(x => x.Id == transactionId, cancellationToken).ConfigureAwait(false);
        if (selected?.TransferGroupId is not Guid groupId) return Result.Failure("Linked transfer not found.");
        var pair = await db.Transactions.Where(x => x.TransferGroupId == groupId).OrderBy(x => x.AmountMinor).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (pair.Count != 2) return Result.Failure("The linked transfer is inconsistent and was not changed.");
        if (pair.Any(x => x.IsDeleted)) return Result.Failure("Restore the transfer before editing it.");
        foreach (var item in pair)
        {
            db.TransactionRevisions.Add(new TransactionRevision { TransactionId = item.Id, ChangeKind = "BeforeTransferEdit", SnapshotJson = TransactionRevisionSerializer.Serialize(item), ChangedAtUtc = DateTimeOffset.UtcNow });
            item.AmountMinor = item.AmountMinor < 0 ? -amountMinor : amountMinor;
            item.OccurredAtUtc = occurredAtUtc;
            item.Note = Normalize(note, 2000);
            item.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }
        db.AuditEntries.Add(new AuditEntry { EntityType = "Transfer", EntityId = groupId, Action = "UpdatedWithRevision" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transactionScope.CommitAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<int> BulkCategorizeAsync(IReadOnlyCollection<Guid> transactionIds, Guid? categoryId, CancellationToken cancellationToken = default)
    {
        if (transactionIds.Count == 0) return 0;
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        if (categoryId is Guid requestedCategory && !await db.Categories.AnyAsync(x => x.Id == requestedCategory && !x.IsArchived, cancellationToken).ConfigureAwait(false)) throw new InvalidOperationException("The selected category is unavailable.");
        await using var transactionScope = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var rows = await db.Transactions.Include(x => x.Splits).Include(x => x.TransactionTags).Where(x => transactionIds.Contains(x.Id) && !x.IsDeleted && x.TransferGroupId == null).ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var row in rows)
        {
            db.TransactionRevisions.Add(new TransactionRevision { TransactionId = row.Id, ChangeKind = "BeforeBulkCategorize", SnapshotJson = TransactionRevisionSerializer.Serialize(row, row.Splits, row.TransactionTags.Select(x => x.TagId).ToList()), ChangedAtUtc = DateTimeOffset.UtcNow });
            row.CategoryId = categoryId;
            row.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }
        if (rows.Count > 0) db.AuditEntries.Add(new AuditEntry { EntityType = "TransactionBatch", EntityId = Guid.NewGuid(), Action = $"BulkCategorized:{rows.Count}" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transactionScope.CommitAsync(cancellationToken).ConfigureAwait(false);
        return rows.Count;
    }

    public async Task<IReadOnlyList<DuplicateTransactionCandidate>> FindLikelyDuplicatesAsync(DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var query = db.Transactions.AsNoTracking().Include(x => x.Account).Where(x => !x.IsDeleted && x.TransferGroupId == null);
        if (from is not null) query = query.Where(x => x.OccurredAtUtc >= from);
        if (to is not null) query = query.Where(x => x.OccurredAtUtc <= to);
        var rows = await query.OrderBy(x => x.OccurredAtUtc).Take(5000).ToListAsync(cancellationToken).ConfigureAwait(false);
        var results = new List<DuplicateTransactionCandidate>();
        foreach (var group in rows.GroupBy(x => new { x.AccountId, x.AmountMinor, x.Currency }))
        {
            var ordered = group.OrderBy(x => x.OccurredAtUtc).ToList();
            for (var i = 0; i < ordered.Count; i++)
            for (var j = i + 1; j < ordered.Count; j++)
            {
                var delta = ordered[j].OccurredAtUtc - ordered[i].OccurredAtUtc;
                if (delta > TimeSpan.FromDays(2)) break;
                var merchantMatch = string.Equals(NormalizeForCompare(ordered[i].Merchant), NormalizeForCompare(ordered[j].Merchant), StringComparison.Ordinal);
                var noteMatch = string.Equals(NormalizeForCompare(ordered[i].Note), NormalizeForCompare(ordered[j].Note), StringComparison.Ordinal);
                var confidence = merchantMatch ? 95 : noteMatch ? 85 : delta <= TimeSpan.FromMinutes(10) ? 80 : 70;
                results.Add(new DuplicateTransactionCandidate(ordered[i].Id, ordered[j].Id, ordered[j].OccurredAtUtc, ordered[j].AmountMinor, ordered[j].Currency, ordered[j].Account?.Name ?? string.Empty, ordered[j].Merchant, confidence));
            }
        }
        return results.OrderByDescending(x => x.ConfidencePercent).ThenByDescending(x => x.OccurredAtUtc).Take(250).ToList();
    }

    public async Task<IReadOnlyList<TransactionRevisionInfo>> GetRevisionHistoryAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rows = await db.TransactionRevisions.AsNoTracking().Where(x => x.TransactionId == transactionId).OrderByDescending(x => x.ChangedAtUtc).Select(x => new { x.Id, x.ChangedAtUtc, x.ChangeKind, x.SnapshotJson }).ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(x => new TransactionRevisionInfo(x.Id, x.ChangedAtUtc, x.ChangeKind, TransactionRevisionSerializer.Describe(x.SnapshotJson))).ToList();
    }

    private static string? Normalize(string? value, int maxLength) { if (string.IsNullOrWhiteSpace(value)) return null; var trimmed = value.Trim(); return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength]; }
    private static string NormalizeForCompare(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
}
