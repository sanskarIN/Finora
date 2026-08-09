using Finora.Application;
using Finora.Domain;
using Microsoft.EntityFrameworkCore;

namespace Finora.Infrastructure;

public sealed class FinanceStore(IDbContextFactory<FinoraDbContext> factory, DatabaseInitializer initializer) : IFinanceStore
{
    private readonly IDbContextFactory<FinoraDbContext> _factory = factory;
    private readonly DatabaseInitializer _initializer = initializer;

    public Task InitializeAsync(CancellationToken cancellationToken = default) => _initializer.InitializeAsync(cancellationToken);

    public async Task<IReadOnlyList<AccountSummary>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var accounts = await db.Accounts.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
        var sums = await db.Transactions.AsNoTracking().Where(x => !x.IsDeleted).GroupBy(x => x.AccountId).Select(g => new { g.Key, Sum = g.Sum(x => x.AmountMinor) }).ToDictionaryAsync(x => x.Key, x => x.Sum, cancellationToken).ConfigureAwait(false);
        return accounts.Select(x => new AccountSummary(x.Id, x.Name, x.Type, x.Currency, checked(x.OpeningBalanceMinor + sums.GetValueOrDefault(x.Id)), x.State)).ToList();
    }

    public async Task<Guid> SaveAccountAsync(Account account, CancellationToken cancellationToken = default)
    {
        DomainRules.ValidateAccount(account);
        account.Name = account.Name.Trim(); account.Currency = account.Currency.Trim().ToUpperInvariant(); account.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var existing = await db.Accounts.SingleOrDefaultAsync(x => x.Id == account.Id, cancellationToken).ConfigureAwait(false);
        if (existing is null) db.Accounts.Add(account); else db.Entry(existing).CurrentValues.SetValues(account);
        db.AuditEntries.Add(new AuditEntry { EntityType = "Account", EntityId = account.Id, Action = existing is null ? "Created" : "Updated" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false); return account.Id;
    }

    public async Task ArchiveAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var account = await db.Accounts.SingleAsync(x => x.Id == accountId, cancellationToken).ConfigureAwait(false);
        account.State = AccountState.Archived; account.UpdatedAtUtc = DateTimeOffset.UtcNow;
        db.AuditEntries.Add(new AuditEntry { EntityType = "Account", EntityId = account.Id, Action = "Archived" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TransactionListItem>> SearchTransactionsAsync(string? query = null, Guid? accountId = null, Guid? categoryId = null, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rows = db.Transactions.AsNoTracking().Include(x => x.Account).Include(x => x.Category).Where(x => !x.IsDeleted);
        if (accountId is not null) rows = rows.Where(x => x.AccountId == accountId);
        if (categoryId is not null) rows = rows.Where(x => x.CategoryId == categoryId);
        if (from is not null) rows = rows.Where(x => x.OccurredAtUtc >= from);
        if (to is not null) rows = rows.Where(x => x.OccurredAtUtc <= to);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var text = query.Trim();
            rows = rows.Where(x => (x.Merchant != null && x.Merchant.Contains(text)) || (x.Note != null && x.Note.Contains(text)) || (x.PaymentMethod != null && x.PaymentMethod.Contains(text)) || (x.ManualLocation != null && x.ManualLocation.Contains(text)) || (x.Account != null && x.Account.Name.Contains(text)) || (x.Category != null && x.Category.Name.Contains(text)));
        }
        return await rows.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.CreatedAtUtc).Select(x => new TransactionListItem(x.Id, x.Type, x.AmountMinor, x.Currency, x.OccurredAtUtc, x.Account!.Name, x.Category != null ? x.Category.Name : null, x.Merchant, x.Note)).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Guid> SaveTransactionAsync(FinanceTransaction transaction, CancellationToken cancellationToken = default)
    {
        DomainRules.ValidateTransaction(transaction);
        if (transaction.Type == TransactionType.Transfer) throw new InvalidOperationException("Use RecordTransferAsync so both sides of a transfer are written atomically.");
        transaction.Currency = transaction.Currency.Trim().ToUpperInvariant(); transaction.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        if (!await db.Accounts.AnyAsync(x => x.Id == transaction.AccountId && x.State != AccountState.Archived, cancellationToken).ConfigureAwait(false)) throw new InvalidOperationException("Account unavailable.");
        if (transaction.CategoryId is Guid categoryId && !await db.Categories.AnyAsync(x => x.Id == categoryId && !x.IsArchived, cancellationToken).ConfigureAwait(false)) throw new InvalidOperationException("Category unavailable.");
        await using var scope = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var existing = await db.Transactions.Include(x => x.Splits).Include(x => x.TransactionTags).SingleOrDefaultAsync(x => x.Id == transaction.Id, cancellationToken).ConfigureAwait(false);
        if (existing is null) db.Transactions.Add(transaction);
        else
        {
            db.TransactionRevisions.Add(new TransactionRevision { TransactionId = existing.Id, ChangeKind = "BeforeEdit", SnapshotJson = TransactionRevisionSerializer.Serialize(existing, existing.Splits, existing.TransactionTags.Select(x => x.TagId).ToList()), ChangedAtUtc = DateTimeOffset.UtcNow });
            db.Entry(existing).CurrentValues.SetValues(transaction);
        }
        db.AuditEntries.Add(new AuditEntry { EntityType = "Transaction", EntityId = transaction.Id, Action = existing is null ? "Created" : "UpdatedWithRevision" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false); await scope.CommitAsync(cancellationToken).ConfigureAwait(false); return transaction.Id;
    }

    public async Task<(Guid SourceTransactionId, Guid DestinationTransactionId)> RecordTransferAsync(Guid sourceAccountId, Guid destinationAccountId, long amountMinor, DateTimeOffset occurredAtUtc, string? note, CancellationToken cancellationToken = default)
    {
        if (sourceAccountId == destinationAccountId || amountMinor <= 0) throw new ArgumentException("Transfer requires different accounts and a positive amount.");
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var scope = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var accounts = await db.Accounts.Where(x => (x.Id == sourceAccountId || x.Id == destinationAccountId) && x.State != AccountState.Archived).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (accounts.Count != 2) throw new InvalidOperationException("Transfer account missing or archived.");
        var source = accounts.Single(x => x.Id == sourceAccountId); var destination = accounts.Single(x => x.Id == destinationAccountId);
        if (!string.Equals(source.Currency, destination.Currency, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Cross-currency transfer requires an explicit exchange workflow.");
        var group = Guid.NewGuid();
        var outgoing = new FinanceTransaction { Type = TransactionType.Transfer, AmountMinor = -amountMinor, Currency = source.Currency, AccountId = source.Id, CounterpartyAccountId = destination.Id, TransferGroupId = group, OccurredAtUtc = occurredAtUtc, Note = note?.Trim() };
        var incoming = new FinanceTransaction { Type = TransactionType.Transfer, AmountMinor = amountMinor, Currency = source.Currency, AccountId = destination.Id, CounterpartyAccountId = source.Id, TransferGroupId = group, OccurredAtUtc = occurredAtUtc, Note = note?.Trim() };
        db.Transactions.AddRange(outgoing, incoming); db.AuditEntries.Add(new AuditEntry { EntityType = "Transfer", EntityId = group, Action = "Created" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false); await scope.CommitAsync(cancellationToken).ConfigureAwait(false); return (outgoing.Id, incoming.Id);
    }

    public async Task SoftDeleteTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default) => await SetDeletedAsync(transactionId, true, cancellationToken).ConfigureAwait(false);
    public async Task RestoreDeletedTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default) => await SetDeletedAsync(transactionId, false, cancellationToken).ConfigureAwait(false);

    private async Task SetDeletedAsync(Guid transactionId, bool deleted, CancellationToken cancellationToken)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var scope = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var selected = await db.Transactions.Include(x => x.Splits).Include(x => x.TransactionTags).SingleAsync(x => x.Id == transactionId, cancellationToken).ConfigureAwait(false);
        var rows = selected.TransferGroupId is Guid group ? await db.Transactions.Include(x => x.Splits).Include(x => x.TransactionTags).Where(x => x.TransferGroupId == group).ToListAsync(cancellationToken).ConfigureAwait(false) : [selected];
        foreach (var item in rows)
        {
            db.TransactionRevisions.Add(new TransactionRevision { TransactionId = item.Id, ChangeKind = deleted ? "BeforeDelete" : "BeforeRestore", SnapshotJson = TransactionRevisionSerializer.Serialize(item, item.Splits, item.TransactionTags.Select(x => x.TagId).ToList()), ChangedAtUtc = DateTimeOffset.UtcNow });
            item.IsDeleted = deleted; item.DeletedAtUtc = deleted ? DateTimeOffset.UtcNow : null; item.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }
        db.AuditEntries.Add(new AuditEntry { EntityType = selected.TransferGroupId is null ? "Transaction" : "Transfer", EntityId = selected.TransferGroupId ?? selected.Id, Action = deleted ? "SoftDeleted" : "Restored" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false); await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Categories.AsNoTracking().Where(x => !x.IsArchived).OrderBy(x => x.ParentId).ThenBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Guid> SaveCategoryAsync(Category category, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category.Name)) throw new ArgumentException("Category name is required.");
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var existing = await db.Categories.SingleOrDefaultAsync(x => x.Id == category.Id, cancellationToken).ConfigureAwait(false);
        if (existing is null) db.Categories.Add(category); else db.Entry(existing).CurrentValues.SetValues(category);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false); return category.Id;
    }

    public async Task<IReadOnlyList<BudgetSnapshot>> GetBudgetsAsync(DateOnly periodDate, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var budgets = await db.Budgets.AsNoTracking().Include(x => x.Periods).Where(x => !x.IsArchived).OrderBy(x => x.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
        var categories = await db.Categories.AsNoTracking().Select(x => new { x.Id, x.ParentId }).ToListAsync(cancellationToken).ConfigureAwait(false);
        var snapshots = new List<BudgetSnapshot>();
        foreach (var budget in budgets)
        {
            var period = ResolveBudgetPeriod(budget, periodDate);
            var from = new DateTimeOffset(period.Start.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero); var to = new DateTimeOffset(period.End.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var transactions = await db.Transactions.AsNoTracking().Include(x => x.Splits).Where(x => !x.IsDeleted && x.Currency == budget.Currency && x.Type != TransactionType.Transfer && x.OccurredAtUtc >= from && x.OccurredAtUtc < to).ToListAsync(cancellationToken).ConfigureAwait(false);
            HashSet<Guid>? categoryIds = null;
            if (budget.CategoryId is Guid root)
            {
                categoryIds = [root];
                if (budget.Kind == BudgetKind.Category)
                {
                    var added = true; while (added) { added = false; foreach (var child in categories.Where(x => x.ParentId is Guid parent && categoryIds.Contains(parent))) if (categoryIds.Add(child.Id)) added = true; }
                }
            }
            long actual = 0;
            foreach (var item in transactions)
            {
                if (categoryIds is null) actual = checked(actual + Math.Abs(Math.Min(0, item.AmountMinor)));
                else if (item.Splits.Count > 0) actual = checked(actual + item.Splits.Where(x => x.AmountMinor < 0 && x.CategoryId is Guid id && categoryIds.Contains(id)).Sum(x => -x.AmountMinor));
                else if (item.AmountMinor < 0 && item.CategoryId is Guid id && categoryIds.Contains(id)) actual = checked(actual - item.AmountMinor);
            }
            snapshots.Add(new BudgetSnapshot(budget.Id, budget.Name, period.Planned, actual, budget.Currency, budget.WarningThresholdPercent));
        }
        return snapshots;
    }

    public async Task<Guid> SaveBudgetAsync(Budget budget, CancellationToken cancellationToken = default)
    {
        if (budget.LimitMinor <= 0) throw new ArgumentOutOfRangeException(nameof(budget.LimitMinor));
        if (budget.WarningThresholdPercent is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(budget.WarningThresholdPercent));
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var existing = await db.Budgets.SingleOrDefaultAsync(x => x.Id == budget.Id, cancellationToken).ConfigureAwait(false);
        if (existing is null) db.Budgets.Add(budget); else db.Entry(existing).CurrentValues.SetValues(budget);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false); return budget.Id;
    }

    public async Task<IReadOnlyList<SavingsGoalSnapshot>> GetSavingsGoalsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var goals = await db.SavingsGoals.AsNoTracking().Include(x => x.Contributions).OrderBy(x => x.TargetDate).ThenBy(x => x.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
        return goals.Select(g => { var current = checked(g.StartingMinor + g.Contributions.Sum(x => x.AmountMinor)); var progress = g.TargetMinor <= 0 ? 0d : Math.Clamp((double)current / g.TargetMinor, 0d, 1d); return new SavingsGoalSnapshot(g.Id, g.Name, g.TargetMinor, current, g.Currency, g.TargetDate, progress); }).ToList();
    }

    public async Task<Guid> SaveSavingsGoalAsync(SavingsGoal goal, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(goal.Name) || goal.TargetMinor <= 0 || goal.StartingMinor < 0) throw new ArgumentException("Savings goal values are invalid.");
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var existing = await db.SavingsGoals.SingleOrDefaultAsync(x => x.Id == goal.Id, cancellationToken).ConfigureAwait(false);
        if (existing is null) db.SavingsGoals.Add(goal); else db.Entry(existing).CurrentValues.SetValues(goal);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false); return goal.Id;
    }

    public async Task AddGoalContributionAsync(GoalContribution contribution, CancellationToken cancellationToken = default)
    {
        if (contribution.AmountMinor == 0) throw new ArgumentException("Contribution cannot be zero.");
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var goal = await db.SavingsGoals.Include(x => x.Contributions).SingleOrDefaultAsync(x => x.Id == contribution.SavingsGoalId, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("Savings goal not found.");
        var current = checked(goal.StartingMinor + goal.Contributions.Sum(x => x.AmountMinor));
        if (checked(current + contribution.AmountMinor) < 0) throw new InvalidOperationException("A withdrawal cannot reduce goal progress below zero.");
        if (contribution.TransactionId is Guid transactionId && !await db.Transactions.AnyAsync(x => x.Id == transactionId && !x.IsDeleted, cancellationToken).ConfigureAwait(false)) throw new InvalidOperationException("Linked transaction was not found.");
        db.GoalContributions.Add(contribution); goal.IsCompleted = checked(current + contribution.AmountMinor) >= goal.TargetMinor; goal.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RecurrenceRule>> GetRecurrenceRulesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.RecurrenceRules.AsNoTracking().Where(x => x.Status != RecurrenceStatus.Archived).OrderBy(x => x.NextDueOn).ThenBy(x => x.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Guid> SaveRecurrenceRuleAsync(RecurrenceRule rule, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rule.Name) || rule.AmountMinor <= 0 || rule.Interval <= 0) throw new ArgumentException("Recurring rule values are invalid.");
        rule.NextDueOn ??= rule.StartsOn; rule.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        if (!await db.Accounts.AnyAsync(x => x.Id == rule.AccountId && x.State != AccountState.Archived, cancellationToken).ConfigureAwait(false)) throw new InvalidOperationException("Recurring account unavailable.");
        if (rule.TransactionType == TransactionType.Transfer && rule.DestinationAccountId is null) throw new InvalidOperationException("Recurring transfers require a destination account.");
        var existing = await db.RecurrenceRules.SingleOrDefaultAsync(x => x.Id == rule.Id, cancellationToken).ConfigureAwait(false);
        if (existing is null) db.RecurrenceRules.Add(rule); else db.Entry(existing).CurrentValues.SetValues(rule);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false); return rule.Id;
    }

    public async Task<int> ProcessDueRecurrencesAsync(DateOnly throughDate, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rules = await db.RecurrenceRules.Where(x => x.Status == RecurrenceStatus.Active && x.NextDueOn != null && x.NextDueOn <= throughDate).ToListAsync(cancellationToken).ConfigureAwait(false);
        var created = 0;
        foreach (var rule in rules)
        {
            var due = rule.NextDueOn!.Value;
            while (due <= throughDate && (rule.EndsOn is null || due <= rule.EndsOn))
            {
                if (!await db.RecurrenceOccurrences.AnyAsync(x => x.RecurrenceRuleId == rule.Id && x.DueOn == due, cancellationToken).ConfigureAwait(false)) { db.RecurrenceOccurrences.Add(new RecurrenceOccurrence { RecurrenceRuleId = rule.Id, DueOn = due, Status = OccurrenceStatus.Pending }); created++; }
                rule.LastGeneratedOn = due; due = DomainRules.GetNextOccurrence(rule, due); rule.NextDueOn = due;
            }
            if (rule.EndsOn is DateOnly ends && rule.NextDueOn > ends) rule.Status = RecurrenceStatus.Completed;
            rule.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false); return created;
    }

    public async Task<DashboardSnapshot> GetDashboardAsync(DateOnly start, DateOnly end, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var from = new DateTimeOffset(start.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero); var to = new DateTimeOffset(end.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var transactions = await db.Transactions.AsNoTracking().Include(x => x.Account).Include(x => x.Category).Where(x => !x.IsDeleted && x.OccurredAtUtc >= from && x.OccurredAtUtc < to).ToListAsync(cancellationToken).ConfigureAwait(false);
        var income = transactions.Where(x => x.AmountMinor > 0 && x.Type != TransactionType.Transfer).Sum(x => x.AmountMinor);
        var expense = -transactions.Where(x => x.AmountMinor < 0 && x.Type != TransactionType.Transfer).Sum(x => x.AmountMinor);
        var accounts = await GetAccountsAsync(cancellationToken).ConfigureAwait(false);
        var recent = transactions.OrderByDescending(x => x.OccurredAtUtc).Take(8).Select(x => new TransactionListItem(x.Id, x.Type, x.AmountMinor, x.Currency, x.OccurredAtUtc, x.Account?.Name ?? string.Empty, x.Category?.Name, x.Merchant, x.Note)).ToList();
        var top = transactions.Where(x => x.AmountMinor < 0 && x.Type != TransactionType.Transfer).GroupBy(x => x.Category?.Name ?? "Uncategorized").Select(g => new CategorySpend(g.Key, -g.Sum(x => x.AmountMinor))).OrderByDescending(x => x.AmountMinor).Take(5).ToList();
        var budgets = await GetBudgetsAsync(start, cancellationToken).ConfigureAwait(false); var remaining = budgets.Sum(x => Math.Max(0, x.PlannedMinor - x.ActualMinor));
        return new DashboardSnapshot(accounts.Where(x => x.State != AccountState.Hidden).Sum(x => x.BalanceMinor), income, expense, checked(income - expense), remaining, recent, top);
    }

    public async Task DeleteAllDataAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var scope = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        db.TransactionRevisions.RemoveRange(db.TransactionRevisions); db.AccountReconciliations.RemoveRange(db.AccountReconciliations); db.NotificationSchedules.RemoveRange(db.NotificationSchedules); db.TransactionTags.RemoveRange(db.TransactionTags); db.TransactionSplits.RemoveRange(db.TransactionSplits); db.Attachments.RemoveRange(db.Attachments); db.RecurrenceOccurrences.RemoveRange(db.RecurrenceOccurrences); db.GoalContributions.RemoveRange(db.GoalContributions); db.BudgetPeriods.RemoveRange(db.BudgetPeriods); db.Transactions.RemoveRange(db.Transactions); db.RecurrenceRules.RemoveRange(db.RecurrenceRules); db.Budgets.RemoveRange(db.Budgets); db.SavingsGoals.RemoveRange(db.SavingsGoals); db.Tags.RemoveRange(db.Tags); db.Accounts.RemoveRange(db.Accounts); db.AuditEntries.RemoveRange(db.AuditEntries); db.BackupMetadata.RemoveRange(db.BackupMetadata);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false); await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static (DateOnly Start, DateOnly End, long Planned) ResolveBudgetPeriod(Budget budget, DateOnly date)
    {
        var explicitPeriod = budget.Periods.FirstOrDefault(x => x.StartsOn <= date && x.EndsOn >= date);
        if (explicitPeriod is not null) return (explicitPeriod.StartsOn, explicitPeriod.EndsOn, checked(explicitPeriod.PlannedMinor + (budget.RolloverEnabled ? explicitPeriod.RolloverMinor : 0)));
        if (budget.Cadence == BudgetCadence.Weekly) { var offset = ((int)date.DayOfWeek + 6) % 7; var start = date.AddDays(-offset); return (start, start.AddDays(6), budget.LimitMinor); }
        if (budget.Cadence == BudgetCadence.Monthly) { var start = new DateOnly(date.Year, date.Month, 1); return (start, new DateOnly(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month)), budget.LimitMinor); }
        return (date, date, budget.LimitMinor);
    }
}
