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
        var rule = await db.RecurrenceRules.AsNoTracking().SingleOrDefaultAsync(x => x.Id == ruleId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Recurring rule not found.");
        DomainRules.ValidateRecurrenceRule(rule);

        var current = rule.NextDueOn ?? rule.StartsOn;
        var dates = new List<DateOnly>(count);
        while (dates.Count < count && (rule.EndsOn is null || current <= rule.EndsOn))
        {
            dates.Add(current);
            var next = DomainRules.GetNextOccurrence(rule, current);
            if (next <= current) throw new InvalidDataException("Recurring rule did not advance to a later date.");
            current = next;
        }
        return dates;
    }

    public async Task<IReadOnlyList<RecurrenceOccurrenceInfo>> GetOccurrencesAsync(DateOnly? from = null, DateOnly? to = null, bool includeCompleted = true, CancellationToken cancellationToken = default)
    {
        if (from is DateOnly startDate && to is DateOnly endDate && endDate < startDate)
            throw new ArgumentException("Occurrence range end cannot precede its start.");

        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var query = db.RecurrenceOccurrences.AsNoTracking().Include(x => x.RecurrenceRule).AsQueryable();
        if (from is DateOnly start) query = query.Where(x => x.DueOn >= start);
        if (to is DateOnly end) query = query.Where(x => x.DueOn <= end);
        if (!includeCompleted)
            query = query.Where(x => x.Status == OccurrenceStatus.Pending || x.Status == OccurrenceStatus.Postponed || x.Status == OccurrenceStatus.PartiallyPaid);
        return await query
            .OrderBy(x => x.PostponedTo ?? x.DueOn)
            .Select(x => new RecurrenceOccurrenceInfo(
                x.Id,
                x.RecurrenceRuleId,
                x.RecurrenceRule!.Name,
                x.DueOn,
                x.Status,
                x.RecurrenceRule.AmountMinor,
                x.RecurrenceRule.Currency,
                x.PaidAmountMinor,
                x.PostponedTo,
                x.GeneratedTransactionId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Result> PauseRuleAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rule = await db.RecurrenceRules.SingleOrDefaultAsync(x => x.Id == ruleId, cancellationToken).ConfigureAwait(false);
        if (rule is null) return Result.Failure("Recurring rule not found.");
        if (rule.Status == RecurrenceStatus.Paused) return Result.Success();
        if (rule.Status is RecurrenceStatus.Completed or RecurrenceStatus.Archived)
            return Result.Failure("Completed or archived recurring rules cannot be paused.");

        rule.Status = RecurrenceStatus.Paused;
        rule.UpdatedAtUtc = DateTimeOffset.UtcNow;
        db.AuditEntries.Add(new AuditEntry { EntityType = "RecurrenceRule", EntityId = rule.Id, Action = "Paused" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> ResumeRuleAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rule = await db.RecurrenceRules.SingleOrDefaultAsync(x => x.Id == ruleId, cancellationToken).ConfigureAwait(false);
        if (rule is null) return Result.Failure("Recurring rule not found.");
        if (rule.Status == RecurrenceStatus.Active) return Result.Success();
        if (rule.Status != RecurrenceStatus.Paused)
            return Result.Failure("Only a paused recurring rule can be resumed.");

        try { DomainRules.ValidateRecurrenceRule(rule); }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return Result.Failure($"Recurring rule is invalid: {exception.Message}");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (rule.EndsOn is DateOnly endsOn && endsOn < today)
            return Result.Failure("This recurring rule has already passed its end date and cannot be resumed.");
        var relationError = await ValidateRuleRelationsAsync(db, rule, cancellationToken).ConfigureAwait(false);
        if (relationError is not null) return Result.Failure(relationError);

        if (rule.NextDueOn is null) rule.NextDueOn = rule.StartsOn;
        rule.Status = RecurrenceStatus.Active;
        rule.UpdatedAtUtc = DateTimeOffset.UtcNow;
        db.AuditEntries.Add(new AuditEntry { EntityType = "RecurrenceRule", EntityId = rule.Id, Action = "Resumed" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> ArchiveRuleAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rule = await db.RecurrenceRules.SingleOrDefaultAsync(x => x.Id == ruleId, cancellationToken).ConfigureAwait(false);
        if (rule is null) return Result.Failure("Recurring rule not found.");
        if (rule.Status == RecurrenceStatus.Archived) return Result.Success();

        rule.Status = RecurrenceStatus.Archived;
        rule.UpdatedAtUtc = DateTimeOffset.UtcNow;
        db.AuditEntries.Add(new AuditEntry { EntityType = "RecurrenceRule", EntityId = rule.Id, Action = "Archived" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> MarkPaidAsync(Guid occurrenceId, long? paidAmountMinor = null, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var occurrence = await db.RecurrenceOccurrences
            .Include(x => x.RecurrenceRule)
            .SingleOrDefaultAsync(x => x.Id == occurrenceId, cancellationToken)
            .ConfigureAwait(false);
        if (occurrence?.RecurrenceRule is null) return Result.Failure("Recurring occurrence not found.");
        if (occurrence.Status == OccurrenceStatus.Skipped) return Result.Failure("Reopen the skipped occurrence before recording payment.");

        var rule = occurrence.RecurrenceRule;
        try { DomainRules.ValidateRecurrenceRule(rule); }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return Result.Failure($"Recurring rule is invalid: {exception.Message}");
        }

        var relationError = await ValidateRuleRelationsAsync(db, rule, cancellationToken).ConfigureAwait(false);
        if (relationError is not null) return Result.Failure(relationError);

        var paid = paidAmountMinor ?? rule.AmountMinor;
        if (paid <= 0 || paid > rule.AmountMinor)
            return Result.Failure("Paid amount must be greater than zero and cannot exceed the scheduled amount.");
        if (occurrence.Status == OccurrenceStatus.Paid)
        {
            if (occurrence.PaidAmountMinor == rule.AmountMinor && paid == rule.AmountMinor) return Result.Success();
            return Result.Failure("This occurrence is already fully paid. Edit the linked transaction instead of changing the completed occurrence.");
        }

        var effectiveDate = occurrence.PostponedTo ?? occurrence.DueOn;
        var occurredAt = new DateTimeOffset(effectiveDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        if (occurrence.GeneratedTransactionId is Guid existingTransactionId)
        {
            var existing = await db.Transactions.SingleOrDefaultAsync(x => x.Id == existingTransactionId, cancellationToken).ConfigureAwait(false);
            if (existing is null) return Result.Failure("The payment link is inconsistent; no changes were made.");
            if (existing.IsDeleted) return Result.Failure("The generated payment is deleted. Restore or resolve it before changing the occurrence.");
            if (existing.RecurrenceRuleId != rule.Id)
                return Result.Failure("The generated payment no longer belongs to this recurring rule.");

            if (rule.TransactionType == TransactionType.Transfer)
            {
                if (existing.TransferGroupId is not Guid transferGroup)
                    return Result.Failure("The generated recurring transfer lost its transfer-group link.");
                var pair = await db.Transactions.Where(x => x.TransferGroupId == transferGroup).ToListAsync(cancellationToken).ConfigureAwait(false);
                var pairError = ValidateGeneratedTransferPair(pair, rule);
                if (pairError is not null) return Result.Failure(pairError);

                foreach (var item in pair)
                {
                    item.AmountMinor = item.AmountMinor < 0 ? checked(-paid) : paid;
                    item.OccurredAtUtc = occurredAt;
                    item.UpdatedAtUtc = DateTimeOffset.UtcNow;
                }
            }
            else
            {
                if (existing.TransferGroupId is not null ||
                    existing.Type != rule.TransactionType ||
                    existing.AccountId != rule.AccountId ||
                    !string.Equals(existing.Currency, rule.Currency, StringComparison.OrdinalIgnoreCase))
                    return Result.Failure("The generated payment no longer matches this recurring rule.");

                existing.AmountMinor = SignedAmount(rule.TransactionType, paid);
                existing.OccurredAtUtc = occurredAt;
                existing.UpdatedAtUtc = DateTimeOffset.UtcNow;
            }
        }
        else if (rule.TransactionType == TransactionType.Transfer)
        {
            var destination = rule.DestinationAccountId!.Value;
            var group = Guid.NewGuid();
            var outgoing = new FinanceTransaction
            {
                Type = TransactionType.Transfer,
                AmountMinor = checked(-paid),
                Currency = rule.Currency,
                AccountId = rule.AccountId,
                CounterpartyAccountId = destination,
                TransferGroupId = group,
                OccurredAtUtc = occurredAt,
                RecurrenceRuleId = rule.Id,
                Merchant = rule.Merchant,
                Note = rule.Note
            };
            var incoming = new FinanceTransaction
            {
                Type = TransactionType.Transfer,
                AmountMinor = paid,
                Currency = rule.Currency,
                AccountId = destination,
                CounterpartyAccountId = rule.AccountId,
                TransferGroupId = group,
                OccurredAtUtc = occurredAt,
                RecurrenceRuleId = rule.Id,
                Merchant = rule.Merchant,
                Note = rule.Note
            };
            db.Transactions.AddRange(outgoing, incoming);
            occurrence.GeneratedTransactionId = outgoing.Id;
        }
        else
        {
            var item = new FinanceTransaction
            {
                Type = rule.TransactionType,
                AmountMinor = SignedAmount(rule.TransactionType, paid),
                Currency = rule.Currency,
                AccountId = rule.AccountId,
                CategoryId = rule.CategoryId,
                OccurredAtUtc = occurredAt,
                Merchant = rule.Merchant,
                Note = rule.Note,
                RecurrenceRuleId = rule.Id
            };
            DomainRules.ValidateTransaction(item);
            db.Transactions.Add(item);
            occurrence.GeneratedTransactionId = item.Id;
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
        if (occurrence.Status == OccurrenceStatus.Skipped) return Result.Success();
        if (occurrence.Status == OccurrenceStatus.Paid) return Result.Failure("A fully paid occurrence cannot be skipped.");
        if (occurrence.GeneratedTransactionId is not null || occurrence.PaidAmountMinor is > 0)
            return Result.Failure("A payment is already linked to this occurrence and must be resolved before skipping it.");
        occurrence.Status = OccurrenceStatus.Skipped;
        occurrence.PostponedTo = null;
        occurrence.UpdatedAtUtc = DateTimeOffset.UtcNow;
        db.AuditEntries.Add(new AuditEntry { EntityType = "RecurrenceOccurrence", EntityId = occurrence.Id, Action = "Skipped" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> PostponeAsync(Guid occurrenceId, DateOnly newDate, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var occurrence = await db.RecurrenceOccurrences
            .Include(x => x.RecurrenceRule)
            .SingleOrDefaultAsync(x => x.Id == occurrenceId, cancellationToken)
            .ConfigureAwait(false);
        if (occurrence?.RecurrenceRule is null) return Result.Failure("Recurring occurrence not found.");
        if (occurrence.Status == OccurrenceStatus.Skipped) return Result.Failure("Reopen the skipped occurrence before postponing it.");
        if (occurrence.Status == OccurrenceStatus.Paid) return Result.Failure("A fully paid occurrence cannot be postponed.");
        if (occurrence.GeneratedTransactionId is not null || occurrence.PaidAmountMinor is > 0)
            return Result.Failure("A payment is already linked to this occurrence and cannot be postponed.");
        if (newDate <= occurrence.DueOn) return Result.Failure("The postponed date must be after the original due date.");
        if (occurrence.RecurrenceRule.EndsOn is DateOnly endsOn && newDate > endsOn)
            return Result.Failure("The postponed date is after this recurring rule ends.");
        occurrence.PostponedTo = newDate;
        occurrence.Status = OccurrenceStatus.Postponed;
        occurrence.UpdatedAtUtc = DateTimeOffset.UtcNow;
        db.AuditEntries.Add(new AuditEntry { EntityType = "RecurrenceOccurrence", EntityId = occurrence.Id, Action = $"Postponed:{newDate:yyyy-MM-dd}" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> ReopenAsync(Guid occurrenceId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var occurrence = await db.RecurrenceOccurrences.SingleOrDefaultAsync(x => x.Id == occurrenceId, cancellationToken).ConfigureAwait(false);
        if (occurrence is null) return Result.Failure("Recurring occurrence not found.");
        if (occurrence.Status != OccurrenceStatus.Skipped) return Result.Failure("Only a skipped occurrence can be reopened.");
        if (occurrence.GeneratedTransactionId is not null || occurrence.PaidAmountMinor is > 0)
            return Result.Failure("This occurrence already has payment data and cannot be reopened as pending.");

        occurrence.Status = OccurrenceStatus.Pending;
        occurrence.PostponedTo = null;
        occurrence.UpdatedAtUtc = DateTimeOffset.UtcNow;
        db.AuditEntries.Add(new AuditEntry { EntityType = "RecurrenceOccurrence", EntityId = occurrence.Id, Action = "Reopened" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static async Task<string?> ValidateRuleRelationsAsync(FinoraDbContext db, RecurrenceRule rule, CancellationToken cancellationToken)
    {
        var accountIds = new List<Guid> { rule.AccountId };
        if (rule.DestinationAccountId is Guid destinationId) accountIds.Add(destinationId);
        var accounts = await db.Accounts.AsNoTracking()
            .Where(x => accountIds.Contains(x.Id) && x.State != AccountState.Archived)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (accounts.Count != accountIds.Distinct().Count()) return "One or more recurring accounts are unavailable.";
        if (accounts.Any(account => !string.Equals(account.Currency, rule.Currency, StringComparison.OrdinalIgnoreCase)))
            return "Recurring item currency no longer matches its account currency.";

        if (rule.CategoryId is Guid categoryId &&
            !await db.Categories.AsNoTracking().AnyAsync(x => x.Id == categoryId && !x.IsArchived, cancellationToken).ConfigureAwait(false))
            return "The recurring category is no longer available.";
        return null;
    }

    private static string? ValidateGeneratedTransferPair(IReadOnlyList<FinanceTransaction> pair, RecurrenceRule rule)
    {
        if (pair.Count != 2) return "The linked transfer payment is incomplete; no changes were made.";
        var left = pair[0];
        var right = pair[1];
        if (left.IsDeleted || right.IsDeleted) return "The generated transfer payment is deleted. Restore it before changing the occurrence.";
        if (left.Type != TransactionType.Transfer || right.Type != TransactionType.Transfer ||
            left.TransferGroupId is null || left.TransferGroupId != right.TransferGroupId ||
            left.RecurrenceRuleId != rule.Id || right.RecurrenceRuleId != rule.Id ||
            left.AccountId == right.AccountId ||
            left.CounterpartyAccountId != right.AccountId || right.CounterpartyAccountId != left.AccountId ||
            !string.Equals(left.Currency, rule.Currency, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(right.Currency, rule.Currency, StringComparison.OrdinalIgnoreCase) ||
            Math.Sign(left.AmountMinor) == Math.Sign(right.AmountMinor))
            return "The linked transfer payment is inconsistent; no changes were made.";

        if (rule.DestinationAccountId is not Guid destination ||
            !new[] { left.AccountId, right.AccountId }.Contains(rule.AccountId) ||
            !new[] { left.AccountId, right.AccountId }.Contains(destination))
            return "The linked transfer payment no longer matches the recurring accounts.";

        try
        {
            if (checked(left.AmountMinor + right.AmountMinor) != 0)
                return "The linked transfer payment does not balance; no changes were made.";
        }
        catch (OverflowException)
        {
            return "The linked transfer payment amount is outside the supported range.";
        }
        return null;
    }

    private static long SignedAmount(TransactionType type, long amount) => type switch
    {
        TransactionType.Expense => checked(-amount),
        TransactionType.Income or TransactionType.Refund => amount,
        _ => amount
    };
}
