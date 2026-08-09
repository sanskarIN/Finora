using Finora.Application;
using Finora.Domain;
using Finora.Shared;
using Microsoft.EntityFrameworkCore;

namespace Finora.Infrastructure;

public sealed class RecurringWorkflowService(IDbContextFactory<FinoraDbContext> factory) : IRecurringWorkflowService
{
    private readonly IDbContextFactory<FinoraDbContext> _factory = factory;

    public async Task<IReadOnlyList<DateOnly>> PreviewNextOccurrencesAsync(Guid ruleId, int count, CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 24);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rule = await db.RecurrenceRules.AsNoTracking().SingleOrDefaultAsync(x => x.Id == ruleId, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("Recurring rule not found.");
        var current = rule.NextDueOn ?? rule.StartsOn;
        var dates = new List<DateOnly>(count);
        while (dates.Count < count && (rule.EndsOn is null || current <= rule.EndsOn)) { dates.Add(current); current = DomainRules.GetNextOccurrence(rule, current); }
        return dates;
    }

    public async Task<IReadOnlyList<RecurrenceOccurrenceInfo>> GetOccurrencesAsync(DateOnly? from = null, DateOnly? to = null, bool includeCompleted = true, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var query = db.RecurrenceOccurrences.AsNoTracking().Include(x => x.RecurrenceRule).AsQueryable();
        if (from is DateOnly start) query = query.Where(x => x.DueOn >= start);
        if (to is DateOnly end) query = query.Where(x => x.DueOn <= end);
        if (!includeCompleted) query = query.Where(x => x.Status == OccurrenceStatus.Pending || x.Status == OccurrenceStatus.Postponed || x.Status == OccurrenceStatus.PartiallyPaid);
        return await query.OrderBy(x => x.PostponedTo ?? x.DueOn).Select(x => new RecurrenceOccurrenceInfo(x.Id, x.RecurrenceRuleId, x.RecurrenceRule!.Name, x.DueOn, x.Status, x.RecurrenceRule.AmountMinor, x.RecurrenceRule.Currency, x.PaidAmountMinor, x.PostponedTo, x.GeneratedTransactionId)).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result> MarkPaidAsync(Guid occurrenceId, long? paidAmountMinor = null, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var occurrence = await db.RecurrenceOccurrences.Include(x => x.RecurrenceRule).SingleOrDefaultAsync(x => x.Id == occurrenceId, cancellationToken).ConfigureAwait(false);
        if (occurrence?.RecurrenceRule is null) return Result.Failure("Recurring occurrence not found.");
        if (occurrence.Status == OccurrenceStatus.Skipped) return Result.Failure("A skipped occurrence must be reopened before recording payment.");
        var rule = occurrence.RecurrenceRule;
        var paid = paidAmountMinor ?? rule.AmountMinor;
        if (paid <= 0 || paid > rule.AmountMinor) return Result.Failure("Paid amount must be greater than zero and cannot exceed the scheduled amount.");
        var effectiveDate = occurrence.PostponedTo ?? occurrence.DueOn;
        var occurredAt = new DateTimeOffset(effectiveDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        if (occurrence.GeneratedTransactionId is Guid existingTransactionId)
        {
            var existing = await db.Transactions.SingleOrDefaultAsync(x => x.Id == existingTransactionId, cancellationToken).ConfigureAwait(false);
            if (existing is null) return Result.Failure("The payment link is inconsistent; no changes were made.");
            if (existing.TransferGroupId is Guid transferGroup)
            {
                var pair = await db.Transactions.Where(x => x.TransferGroupId == transferGroup).ToListAsync(cancellationToken).ConfigureAwait(false);
                if (pair.Count != 2) return Result.Failure("The linked transfer payment is inconsistent; no changes were made.");
                foreach (var item in pair) { item.AmountMinor = item.AmountMinor < 0 ? -paid : paid; item.OccurredAtUtc = occurredAt; item.UpdatedAtUtc = DateTimeOffset.UtcNow; }
            }
            else { existing.AmountMinor = SignedAmount(rule.TransactionType, paid); existing.OccurredAtUtc = occurredAt; existing.UpdatedAtUtc = DateTimeOffset.UtcNow; }
        }
        else if (rule.TransactionType == TransactionType.Transfer)
        {
            if (rule.DestinationAccountId is not Guid destination) return Result.Failure("Recurring transfer is missing a destination account.");
            var accounts = await db.Accounts.Where(x => x.Id == rule.AccountId || x.Id == destination).ToListAsync(cancellationToken).ConfigureAwait(false);
            if (accounts.Count != 2) return Result.Failure("One or both transfer accounts are unavailable.");
            var source = accounts.Single(x => x.Id == rule.AccountId); var target = accounts.Single(x => x.Id == destination);
            if (!string.Equals(source.Currency, target.Currency, StringComparison.OrdinalIgnoreCase)) return Result.Failure("Cross-currency recurring transfers require an explicit exchange workflow.");
            var group = Guid.NewGuid();
            var outgoing = new FinanceTransaction { Type = TransactionType.Transfer, AmountMinor = -paid, Currency = rule.Currency, AccountId = source.Id, CounterpartyAccountId = target.Id, TransferGroupId = group, OccurredAtUtc = occurredAt, RecurrenceRuleId = rule.Id, Merchant = rule.Merchant, Note = rule.Note };
            var incoming = new FinanceTransaction { Type = TransactionType.Transfer, AmountMinor = paid, Currency = rule.Currency, AccountId = target.Id, CounterpartyAccountId = source.Id, TransferGroupId = group, OccurredAtUtc = occurredAt, RecurrenceRuleId = rule.Id, Merchant = rule.Merchant, Note = rule.Note };
            db.Transactions.AddRange(outgoing, incoming); occurrence.GeneratedTransactionId = outgoing.Id;
        }
        else
        {
            var item = new FinanceTransaction { Type = rule.TransactionType, AmountMinor = SignedAmount(rule.TransactionType, paid), Currency = rule.Currency, AccountId = rule.AccountId, CategoryId = rule.CategoryId, OccurredAtUtc = occurredAt, Merchant = rule.Merchant, Note = rule.Note, RecurrenceRuleId = rule.Id };
            db.Transactions.Add(item); occurrence.GeneratedTransactionId = item.Id;
        }

        occurrence.PaidAmountMinor = paid;
        occurrence.Status = paid == rule.AmountMinor ? OccurrenceStatus.Paid : OccurrenceStatus.PartiallyPaid;
        occurrence.UpdatedAtUtc = DateTimeOffset.UtcNow;
        db.AuditEntries.Add(new AuditEntry { EntityType = "RecurrenceOccurrence", EntityId = occurrence.Id, Action = occurrence.Status.ToString() });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> SkipAsync(Guid occurrenceId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var occurrence = await db.RecurrenceOccurrences.SingleOrDefaultAsync(x => x.Id == occurrenceId, cancellationToken).ConfigureAwait(false);
        if (occurrence is null) return Result.Failure("Recurring occurrence not found.");
        if (occurrence.GeneratedTransactionId is not null || occurrence.PaidAmountMinor is > 0) return Result.Failure("A payment is already linked to this occurrence and must be resolved before skipping it.");
        occurrence.Status = OccurrenceStatus.Skipped; occurrence.PostponedTo = null; occurrence.UpdatedAtUtc = DateTimeOffset.UtcNow;
        db.AuditEntries.Add(new AuditEntry { EntityType = "RecurrenceOccurrence", EntityId = occurrence.Id, Action = "Skipped" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false); return Result.Success();
    }

    public async Task<Result> PostponeAsync(Guid occurrenceId, DateOnly newDate, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var occurrence = await db.RecurrenceOccurrences.Include(x => x.RecurrenceRule).SingleOrDefaultAsync(x => x.Id == occurrenceId, cancellationToken).ConfigureAwait(false);
        if (occurrence?.RecurrenceRule is null) return Result.Failure("Recurring occurrence not found.");
        if (occurrence.GeneratedTransactionId is not null || occurrence.PaidAmountMinor is > 0) return Result.Failure("A payment is already linked to this occurrence and cannot be postponed.");
        if (newDate <= occurrence.DueOn) return Result.Failure("The postponed date must be after the original due date.");
        if (occurrence.RecurrenceRule.EndsOn is DateOnly endsOn && newDate > endsOn) return Result.Failure("The postponed date is after this recurring rule ends.");
        occurrence.PostponedTo = newDate; occurrence.Status = OccurrenceStatus.Postponed; occurrence.UpdatedAtUtc = DateTimeOffset.UtcNow;
        db.AuditEntries.Add(new AuditEntry { EntityType = "RecurrenceOccurrence", EntityId = occurrence.Id, Action = $"Postponed:{newDate:yyyy-MM-dd}" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false); return Result.Success();
    }

    private static long SignedAmount(TransactionType type, long amount) => type switch { TransactionType.Expense => -amount, TransactionType.Income or TransactionType.Refund => amount, TransactionType.Adjustment => amount, _ => amount };
}
