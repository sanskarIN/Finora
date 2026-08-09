using Finora.Application;
using Finora.Domain;
using Finora.Shared;
using Microsoft.EntityFrameworkCore;

namespace Finora.Infrastructure;

public sealed class ReconciliationService(IDbContextFactory<FinoraDbContext> factory) : IReconciliationService
{
    private readonly IDbContextFactory<FinoraDbContext> _factory = factory;

    public async Task<Result<ReconciliationPreview>> PreviewAsync(Guid accountId, long statementBalanceMinor, DateTimeOffset statementDateUtc, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var account = await db.Accounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == accountId, cancellationToken).ConfigureAwait(false);
        if (account is null) return Result<ReconciliationPreview>.Failure("Account not found.");
        var transactionTotal = await db.Transactions.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.OccurredAtUtc <= statementDateUtc).SumAsync(x => (long?)x.AmountMinor, cancellationToken).ConfigureAwait(false) ?? 0L;
        var bookBalance = checked(account.OpeningBalanceMinor + transactionTotal);
        var difference = checked(statementBalanceMinor - bookBalance);
        return Result<ReconciliationPreview>.Success(new ReconciliationPreview(account.Id, account.Name, account.Currency, bookBalance, statementBalanceMinor, difference, statementDateUtc));
    }

    public async Task<Result<ReconciliationHistoryItem>> CompleteAsync(Guid accountId, long statementBalanceMinor, DateTimeOffset statementDateUtc, bool createAdjustment, string? note, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var account = await db.Accounts.SingleOrDefaultAsync(x => x.Id == accountId, cancellationToken).ConfigureAwait(false);
        if (account is null) return Result<ReconciliationHistoryItem>.Failure("Account not found.");
        if (account.State == AccountState.Archived) return Result<ReconciliationHistoryItem>.Failure("Archived accounts cannot be reconciled.");
        var transactionTotal = await db.Transactions.Where(x => x.AccountId == accountId && !x.IsDeleted && x.OccurredAtUtc <= statementDateUtc).SumAsync(x => (long?)x.AmountMinor, cancellationToken).ConfigureAwait(false) ?? 0L;
        var bookBalance = checked(account.OpeningBalanceMinor + transactionTotal);
        var difference = checked(statementBalanceMinor - bookBalance);
        Guid? adjustmentId = null;
        if (difference != 0 && !createAdjustment) return Result<ReconciliationHistoryItem>.Failure("The statement and book balances differ. Resolve the difference or allow Finora to create an explicit adjustment.");
        if (difference != 0)
        {
            var adjustment = new FinanceTransaction { Type = TransactionType.Adjustment, AmountMinor = difference, Currency = account.Currency, AccountId = account.Id, OccurredAtUtc = statementDateUtc, Note = string.IsNullOrWhiteSpace(note) ? "Reconciliation adjustment" : $"Reconciliation adjustment — {note.Trim()}" };
            db.Transactions.Add(adjustment);
            adjustmentId = adjustment.Id;
        }
        var completedAt = DateTimeOffset.UtcNow;
        var reconciliation = new AccountReconciliation { AccountId = account.Id, StatementDateUtc = statementDateUtc, BookBalanceMinor = bookBalance, StatementBalanceMinor = statementBalanceMinor, DifferenceMinor = difference, AdjustmentCreated = adjustmentId is not null, AdjustmentTransactionId = adjustmentId, Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(), CompletedAtUtc = completedAt };
        db.AccountReconciliations.Add(reconciliation);
        account.LastReconciledAtUtc = statementDateUtc;
        account.ReconciledBalanceMinor = statementBalanceMinor;
        account.UpdatedAtUtc = completedAt;
        db.AuditEntries.Add(new AuditEntry { EntityType = "AccountReconciliation", EntityId = reconciliation.Id, Action = adjustmentId is null ? "Completed" : "CompletedWithAdjustment" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return Result<ReconciliationHistoryItem>.Success(new ReconciliationHistoryItem(reconciliation.Id, reconciliation.AccountId, reconciliation.StatementDateUtc, reconciliation.BookBalanceMinor, reconciliation.StatementBalanceMinor, reconciliation.DifferenceMinor, reconciliation.AdjustmentCreated, reconciliation.AdjustmentTransactionId, reconciliation.Note, reconciliation.CompletedAtUtc));
    }

    public async Task<IReadOnlyList<ReconciliationHistoryItem>> GetHistoryAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.AccountReconciliations.AsNoTracking().Where(x => x.AccountId == accountId).OrderByDescending(x => x.StatementDateUtc).Select(x => new ReconciliationHistoryItem(x.Id, x.AccountId, x.StatementDateUtc, x.BookBalanceMinor, x.StatementBalanceMinor, x.DifferenceMinor, x.AdjustmentCreated, x.AdjustmentTransactionId, x.Note, x.CompletedAtUtc)).ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
