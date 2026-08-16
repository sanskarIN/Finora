using Finora.Application;
using Finora.Domain;
using Finora.Shared;
using Microsoft.EntityFrameworkCore;

namespace Finora.Infrastructure;

public sealed class FinanceStore(IDbContextFactory<FinoraDbContext> factory, DatabaseInitializer initializer, TimeZoneInfo? localTimeZone = null) : IFinanceStore
{
    private const int MaximumGeneratedOccurrencesPerRule = 10_000;
    private readonly IDbContextFactory<FinoraDbContext> _factory = factory;
    private readonly DatabaseInitializer _initializer = initializer;
    private readonly TimeZoneInfo _localTimeZone = localTimeZone ?? TimeZoneInfo.Local;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
        => _initializer.InitializeAsync(cancellationToken);

    public async Task<IReadOnlyList<AccountSummary>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var accounts = await db.Accounts.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
        var amounts = await db.Transactions.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Select(x => new { x.AccountId, x.AmountMinor })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var sums = new Dictionary<Guid, long>();
        foreach (var row in amounts)
            sums[row.AccountId] = checked(sums.GetValueOrDefault(row.AccountId) + row.AmountMinor);

        return accounts
            .Select(account => new AccountSummary(
                account.Id,
                account.Name,
                account.Type,
                account.Currency,
                checked(account.OpeningBalanceMinor + sums.GetValueOrDefault(account.Id)),
                account.State))
            .ToList();
    }

    public async Task<Guid> SaveAccountAsync(Account account, CancellationToken cancellationToken = default)
    {
        DomainRules.ValidateAccount(account);
        account.Name = account.Name.Trim();
        account.Currency = account.Currency.Trim().ToUpperInvariant();
        account.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var existing = await db.Accounts.SingleOrDefaultAsync(x => x.Id == account.Id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            db.Accounts.Add(account);
        }
        else
        {
            var currencyChanged = !string.Equals(existing.Currency, account.Currency, StringComparison.OrdinalIgnoreCase);
            if (currencyChanged)
            {
                var hasTransactions = await db.Transactions.AnyAsync(x => x.AccountId == account.Id || x.CounterpartyAccountId == account.Id, cancellationToken).ConfigureAwait(false);
                var hasRecurrence = await db.RecurrenceRules.AnyAsync(x => x.AccountId == account.Id || x.DestinationAccountId == account.Id, cancellationToken).ConfigureAwait(false);
                if (hasTransactions || hasRecurrence)
                    throw new InvalidOperationException("Account currency cannot change after financial or recurring records reference the account.");
            }
            db.Entry(existing).CurrentValues.SetValues(account);
        }

        db.AuditEntries.Add(new AuditEntry { EntityType = "Account", EntityId = account.Id, Action = existing is null ? "Created" : "Updated" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return account.Id;
    }

    public async Task ArchiveAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        if (await db.RecurrenceRules.AsNoTracking().AnyAsync(rule => rule.Status == RecurrenceStatus.Active && (rule.AccountId == accountId || rule.DestinationAccountId == accountId), cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Pause, complete, or archive recurring items that use this account before archiving the account.");
        var account = await db.Accounts.SingleAsync(x => x.Id == accountId, cancellationToken).ConfigureAwait(false);
        account.State = AccountState.Archived;
        account.UpdatedAtUtc = DateTimeOffset.UtcNow;
        db.AuditEntries.Add(new AuditEntry { EntityType = "Account", EntityId = account.Id, Action = "Archived" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TransactionListItem>> SearchTransactionsAsync(
        string? query = null,
        Guid? accountId = null,
        Guid? categoryId = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rows = db.Transactions.AsNoTracking()
            .Include(x => x.Account)
            .Include(x => x.Category)
            .Where(x => !x.IsDeleted);

        if (accountId is not null) rows = rows.Where(x => x.AccountId == accountId);
        if (categoryId is not null) rows = rows.Where(x => x.CategoryId == categoryId);
        if (from is not null) rows = rows.Where(x => x.OccurredAtUtc >= from);
        if (to is not null) rows = rows.Where(x => x.OccurredAtUtc <= to);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var text = query.Trim();
            rows = rows.Where(x =>
                (x.Merchant != null && x.Merchant.Contains(text)) ||
                (x.Note != null && x.Note.Contains(text)) ||
                (x.PaymentMethod != null && x.PaymentMethod.Contains(text)) ||
                (x.ManualLocation != null && x.ManualLocation.Contains(text)) ||
                (x.Account != null && x.Account.Name.Contains(text)) ||
                (x.Category != null && x.Category.Name.Contains(text)));
        }

        return await rows
            .OrderByDescending(x => x.OccurredAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new TransactionListItem(
                x.Id,
                x.Type,
                x.AmountMinor,
                x.Currency,
                x.OccurredAtUtc,
                x.Account!.Name,
                x.Category != null ? x.Category.Name : null,
                x.Merchant,
                x.Note))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Guid> SaveTransactionAsync(FinanceTransaction transaction, CancellationToken cancellationToken = default)
    {
        DomainRules.ValidateTransaction(transaction);
        if (transaction.Type == TransactionType.Transfer)
            throw new InvalidOperationException("Use RecordTransferAsync so both sides of a transfer are written atomically.");

        transaction.Currency = transaction.Currency.Trim().ToUpperInvariant();
        transaction.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var account = await db.Accounts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == transaction.AccountId && x.State != AccountState.Archived, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Account unavailable.");
        if (!string.Equals(account.Currency, transaction.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Transaction currency must match the selected account currency.");

        if (transaction.CategoryId is Guid categoryId &&
            !await db.Categories.AnyAsync(x => x.Id == categoryId && !x.IsArchived, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Category unavailable.");

        var splitCategoryIds = transaction.Splits.Where(x => x.CategoryId is not null).Select(x => x.CategoryId!.Value).Distinct().ToList();
        if (splitCategoryIds.Count > 0)
        {
            var activeSplitCategories = await db.Categories.CountAsync(x => splitCategoryIds.Contains(x.Id) && !x.IsArchived, cancellationToken).ConfigureAwait(false);
            if (activeSplitCategories != splitCategoryIds.Count)
                throw new InvalidOperationException("One or more split categories are unavailable.");
        }

        await using var scope = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var existing = await db.Transactions
            .Include(x => x.Splits)
            .Include(x => x.TransactionTags)
            .SingleOrDefaultAsync(x => x.Id == transaction.Id, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            db.Transactions.Add(transaction);
        }
        else
        {
            if (existing.TransferGroupId is not null)
                throw new InvalidOperationException("Linked transfers must be edited through the transfer workflow.");
            db.TransactionRevisions.Add(new TransactionRevision
            {
                TransactionId = existing.Id,
                ChangeKind = "BeforeEdit",
                SnapshotJson = TransactionRevisionSerializer.Serialize(existing, existing.Splits, existing.TransactionTags.Select(x => x.TagId).ToList()),
                ChangedAtUtc = DateTimeOffset.UtcNow
            });
            db.Entry(existing).CurrentValues.SetValues(transaction);
        }

        db.AuditEntries.Add(new AuditEntry { EntityType = "Transaction", EntityId = transaction.Id, Action = existing is null ? "Created" : "UpdatedWithRevision" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
        return transaction.Id;
    }

    public async Task<(Guid SourceTransactionId, Guid DestinationTransactionId)> RecordTransferAsync(
        Guid sourceAccountId,
        Guid destinationAccountId,
        long amountMinor,
        DateTimeOffset occurredAtUtc,
        string? note,
        CancellationToken cancellationToken = default)
    {
        if (sourceAccountId == destinationAccountId || amountMinor <= 0)
            throw new ArgumentException("Transfer requires different accounts and a positive amount.");
        if (occurredAtUtc == default)
            throw new ArgumentException("Transfer date/time is required.", nameof(occurredAtUtc));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var scope = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var accounts = await db.Accounts
            .Where(x => (x.Id == sourceAccountId || x.Id == destinationAccountId) && x.State != AccountState.Archived)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (accounts.Count != 2)
            throw new InvalidOperationException("Transfer account missing or archived.");

        var source = accounts.Single(x => x.Id == sourceAccountId);
        var destination = accounts.Single(x => x.Id == destinationAccountId);
        if (!string.Equals(source.Currency, destination.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Cross-currency transfer requires an explicit exchange workflow.");

        var group = Guid.NewGuid();
        var outgoing = new FinanceTransaction
        {
            Type = TransactionType.Transfer,
            AmountMinor = checked(-amountMinor),
            Currency = source.Currency,
            AccountId = source.Id,
            CounterpartyAccountId = destination.Id,
            TransferGroupId = group,
            OccurredAtUtc = occurredAtUtc,
            Note = NormalizeOptional(note)
        };
        var incoming = new FinanceTransaction
        {
            Type = TransactionType.Transfer,
            AmountMinor = amountMinor,
            Currency = source.Currency,
            AccountId = destination.Id,
            CounterpartyAccountId = source.Id,
            TransferGroupId = group,
            OccurredAtUtc = occurredAtUtc,
            Note = NormalizeOptional(note)
        };

        db.Transactions.AddRange(outgoing, incoming);
        db.AuditEntries.Add(new AuditEntry { EntityType = "Transfer", EntityId = group, Action = "Created" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
        return (outgoing.Id, incoming.Id);
    }

    public Task SoftDeleteTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default)
        => SetDeletedAsync(transactionId, true, cancellationToken);

    public Task RestoreDeletedTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default)
        => SetDeletedAsync(transactionId, false, cancellationToken);

    private async Task SetDeletedAsync(Guid transactionId, bool deleted, CancellationToken cancellationToken)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var scope = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var selected = await db.Transactions
            .Include(x => x.Splits)
            .Include(x => x.TransactionTags)
            .SingleAsync(x => x.Id == transactionId, cancellationToken)
            .ConfigureAwait(false);

        List<FinanceTransaction> rows;
        if (selected.TransferGroupId is Guid group)
        {
            rows = await db.Transactions
                .Include(x => x.Splits)
                .Include(x => x.TransactionTags)
                .Where(x => x.TransferGroupId == group)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            ValidateTransferPair(rows);
        }
        else
        {
            rows = [selected];
        }

        if (rows.All(x => x.IsDeleted == deleted))
        {
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        foreach (var item in rows)
        {
            db.TransactionRevisions.Add(new TransactionRevision
            {
                TransactionId = item.Id,
                ChangeKind = deleted ? "BeforeDelete" : "BeforeRestore",
                SnapshotJson = TransactionRevisionSerializer.Serialize(item, item.Splits, item.TransactionTags.Select(x => x.TagId).ToList()),
                ChangedAtUtc = DateTimeOffset.UtcNow
            });
            item.IsDeleted = deleted;
            item.DeletedAtUtc = deleted ? DateTimeOffset.UtcNow : null;
            item.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        db.AuditEntries.Add(new AuditEntry
        {
            EntityType = selected.TransferGroupId is null ? "Transaction" : "Transfer",
            EntityId = selected.TransferGroupId ?? selected.Id,
            Action = deleted ? "SoftDeleted" : "Restored"
        });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Categories.AsNoTracking()
            .Where(x => !x.IsArchived)
            .OrderBy(x => x.ParentId)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Guid> SaveCategoryAsync(Category category, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(category);
        category.Name = category.Name.Trim();
        if (category.Name.Length is 0 or > 120)
            throw new ArgumentException("Category name must contain 1–120 characters.", nameof(category));
        if (category.ParentId == category.Id)
            throw new InvalidOperationException("A category cannot be its own parent.");

        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        if (category.ParentId is Guid parentId)
        {
            var categories = await db.Categories.AsNoTracking()
                .Select(x => new { x.Id, x.ParentId, x.IsArchived })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var parent = categories.SingleOrDefault(x => x.Id == parentId);
            if (parent is null || parent.IsArchived)
                throw new InvalidOperationException("Parent category is unavailable.");

            var parentById = categories.ToDictionary(x => x.Id, x => x.ParentId);
            Guid? current = parentId;
            while (current is Guid id)
            {
                if (id == category.Id)
                    throw new InvalidOperationException("Category hierarchy cannot contain a cycle.");
                if (!parentById.TryGetValue(id, out current)) break;
            }
        }

        var existing = await db.Categories.SingleOrDefaultAsync(x => x.Id == category.Id, cancellationToken).ConfigureAwait(false);
        if (existing is null) db.Categories.Add(category);
        else db.Entry(existing).CurrentValues.SetValues(category);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return category.Id;
    }

    public async Task<IReadOnlyList<BudgetSnapshot>> GetBudgetsAsync(DateOnly periodDate, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var budgets = await db.Budgets.AsNoTracking()
            .Include(x => x.Periods)
            .Where(x => !x.IsArchived)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var categories = await db.Categories.AsNoTracking()
            .Select(x => new { x.Id, x.ParentId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var snapshots = new List<BudgetSnapshot>();

        foreach (var budget in budgets)
        {
            DomainRules.ValidateBudget(budget);
            if (!BudgetPeriodPolicy.TryResolve(budget, periodDate, out var period)) continue;
            var utcRange = LocalDateRange.ToUtc(period.StartsOn, period.EndsOn, _localTimeZone);
            var transactions = await db.Transactions.AsNoTracking()
                .Include(x => x.Splits)
                .Where(x => !x.IsDeleted && x.Currency == budget.Currency && x.Type != TransactionType.Transfer && x.OccurredAtUtc >= utcRange.FromUtc && x.OccurredAtUtc < utcRange.ToExclusiveUtc)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            HashSet<Guid>? categoryIds = null;
            if (budget.CategoryId is Guid root)
            {
                categoryIds = [root];
                if (budget.Kind == BudgetKind.Category)
                {
                    var added = true;
                    while (added)
                    {
                        added = false;
                        foreach (var child in categories.Where(x => x.ParentId is Guid parent && categoryIds.Contains(parent)))
                            if (categoryIds.Add(child.Id)) added = true;
                    }
                }
            }

            long actual = 0;
            foreach (var transaction in transactions)
            {
                if (categoryIds is null)
                {
                    actual = checked(actual + ExpenseMagnitude(transaction.AmountMinor));
                    continue;
                }

                if (transaction.Splits.Count > 0)
                {
                    foreach (var split in transaction.Splits.Where(x => x.AmountMinor < 0 && x.CategoryId is Guid id && categoryIds.Contains(id)))
                        actual = checked(actual + ExpenseMagnitude(split.AmountMinor));
                }
                else if (transaction.AmountMinor < 0 && transaction.CategoryId is Guid id && categoryIds.Contains(id))
                {
                    actual = checked(actual + ExpenseMagnitude(transaction.AmountMinor));
                }
            }

            snapshots.Add(new BudgetSnapshot(budget.Id, budget.Name, period.PlannedMinor, actual, budget.Currency, budget.WarningThresholdPercent));
        }

        return snapshots;
    }

    public async Task<Guid> SaveBudgetAsync(Budget budget, CancellationToken cancellationToken = default)
    {
        if (budget.Cadence == BudgetCadence.Custom && budget.Periods.Count == 0)
            throw new InvalidOperationException("Custom budgets require at least one explicit period.");
        DomainRules.ValidateBudget(budget);
        budget.Name = budget.Name.Trim();
        budget.Currency = budget.Currency.Trim().ToUpperInvariant();
        budget.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        if (budget.CategoryId is Guid categoryId)
        {
            var target = await db.Categories.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == categoryId && !x.IsArchived, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException("Budget category is unavailable.");
            if (budget.Kind == BudgetKind.Subcategory && target.ParentId is null)
                throw new InvalidOperationException("A subcategory budget must target a child category.");
        }

        await using var scope = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var existing = await db.Budgets.SingleOrDefaultAsync(x => x.Id == budget.Id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            db.Budgets.Add(budget);
        }
        else
        {
            db.Entry(existing).CurrentValues.SetValues(budget);
            await db.BudgetPeriods.Where(x => x.BudgetId == existing.Id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            foreach (var period in budget.Periods)
            {
                period.BudgetId = existing.Id;
                db.BudgetPeriods.Add(period);
            }
        }
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
        return budget.Id;
    }

    public async Task<IReadOnlyList<SavingsGoalSnapshot>> GetSavingsGoalsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var goals = await db.SavingsGoals.AsNoTracking()
            .Include(x => x.Contributions)
            .OrderBy(x => x.TargetDate)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return goals.Select(goal =>
        {
            DomainRules.ValidateSavingsGoal(goal);
            var contributions = SumChecked(goal.Contributions.Select(x => x.AmountMinor));
            var current = checked(goal.StartingMinor + contributions);
            var progress = goal.TargetMinor <= 0 ? 0d : Math.Clamp((double)current / goal.TargetMinor, 0d, 1d);
            return new SavingsGoalSnapshot(goal.Id, goal.Name, goal.TargetMinor, current, goal.Currency, goal.TargetDate, progress);
        }).ToList();
    }

    public async Task<Guid> SaveSavingsGoalAsync(SavingsGoal goal, CancellationToken cancellationToken = default)
    {
        DomainRules.ValidateSavingsGoal(goal);
        goal.Name = goal.Name.Trim();
        goal.Currency = goal.Currency.Trim().ToUpperInvariant();
        goal.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var existing = await db.SavingsGoals.SingleOrDefaultAsync(x => x.Id == goal.Id, cancellationToken).ConfigureAwait(false);
        if (existing is null) db.SavingsGoals.Add(goal);
        else db.Entry(existing).CurrentValues.SetValues(goal);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return goal.Id;
    }

    public async Task AddGoalContributionAsync(GoalContribution contribution, CancellationToken cancellationToken = default)
    {
        DomainRules.ValidateGoalContribution(contribution);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var goal = await db.SavingsGoals
            .Include(x => x.Contributions)
            .SingleOrDefaultAsync(x => x.Id == contribution.SavingsGoalId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Savings goal not found.");
        DomainRules.ValidateSavingsGoal(goal);

        var current = checked(goal.StartingMinor + SumChecked(goal.Contributions.Select(x => x.AmountMinor)));
        var updated = checked(current + contribution.AmountMinor);
        if (updated < 0)
            throw new InvalidOperationException("A withdrawal cannot reduce goal progress below zero.");

        if (contribution.TransactionId is Guid transactionId)
        {
            var linked = await db.Transactions.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == transactionId && !x.IsDeleted, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException("Linked transaction was not found.");
            if (!string.Equals(linked.Currency, goal.Currency, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Linked transaction currency must match the savings goal currency.");
        }

        db.GoalContributions.Add(contribution);
        goal.IsCompleted = updated >= goal.TargetMinor;
        goal.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RecurrenceRule>> GetRecurrenceRulesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.RecurrenceRules.AsNoTracking()
            .Where(x => x.Status != RecurrenceStatus.Archived)
            .OrderBy(x => x.NextDueOn)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Guid> SaveRecurrenceRuleAsync(RecurrenceRule rule, CancellationToken cancellationToken = default)
    {
        DomainRules.ValidateRecurrenceRule(rule);
        rule.Name = rule.Name.Trim();
        rule.Currency = rule.Currency.Trim().ToUpperInvariant();
        rule.NextDueOn ??= rule.StartsOn;
        rule.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var source = await db.Accounts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == rule.AccountId && x.State != AccountState.Archived, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Recurring account unavailable.");
        if (!string.Equals(source.Currency, rule.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Recurring item currency must match its source account currency.");

        if (rule.DestinationAccountId is Guid destinationId)
        {
            var destination = await db.Accounts.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == destinationId && x.State != AccountState.Archived, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException("Recurring destination account unavailable.");
            if (!string.Equals(destination.Currency, rule.Currency, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Recurring transfer accounts must use the same currency.");
        }

        if (rule.CategoryId is Guid categoryId &&
            !await db.Categories.AnyAsync(x => x.Id == categoryId && !x.IsArchived, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Recurring category unavailable.");

        var existing = await db.RecurrenceRules.SingleOrDefaultAsync(x => x.Id == rule.Id, cancellationToken).ConfigureAwait(false);
        if (existing is null) db.RecurrenceRules.Add(rule);
        else db.Entry(existing).CurrentValues.SetValues(rule);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return rule.Id;
    }

    public async Task<int> ProcessDueRecurrencesAsync(DateOnly throughDate, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rules = await db.RecurrenceRules
            .Where(x => x.Status == RecurrenceStatus.Active && x.NextDueOn != null && x.NextDueOn <= throughDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var created = 0;

        foreach (var rule in rules)
        {
            DomainRules.ValidateRecurrenceRule(rule);
            var existingDueDates = (await db.RecurrenceOccurrences.AsNoTracking()
                    .Where(x => x.RecurrenceRuleId == rule.Id)
                    .Select(x => x.DueOn)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false))
                .ToHashSet();

            var due = rule.NextDueOn!.Value;
            var generatedForRule = 0;
            while (due <= throughDate && (rule.EndsOn is null || due <= rule.EndsOn))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (++generatedForRule > MaximumGeneratedOccurrencesPerRule)
                    throw new InvalidDataException("Recurring rule backlog exceeds the safe processing limit.");

                if (existingDueDates.Add(due))
                {
                    db.RecurrenceOccurrences.Add(new RecurrenceOccurrence
                    {
                        RecurrenceRuleId = rule.Id,
                        DueOn = due,
                        Status = OccurrenceStatus.Pending
                    });
                    created++;
                }

                rule.LastGeneratedOn = due;
                var next = DomainRules.GetNextOccurrence(rule, due);
                if (next <= due)
                    throw new InvalidDataException("Recurring rule did not advance to a later date.");
                due = next;
                rule.NextDueOn = due;
            }

            if (rule.EndsOn is DateOnly ends && rule.NextDueOn > ends)
                rule.Status = RecurrenceStatus.Completed;
            rule.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return created;
    }

    public async Task<DashboardSnapshot> GetDashboardAsync(DateOnly start, DateOnly end, CancellationToken cancellationToken = default)
    {
        if (end < start) throw new ArgumentException("Dashboard period end cannot precede the start.");

        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var utcRange = LocalDateRange.ToUtc(start, end, _localTimeZone);
        var transactions = await db.Transactions.AsNoTracking()
            .Include(x => x.Account)
            .Include(x => x.Category)
            .Where(x => !x.IsDeleted && x.OccurredAtUtc >= utcRange.FromUtc && x.OccurredAtUtc < utcRange.ToExclusiveUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var accounts = await GetAccountsAsync(cancellationToken).ConfigureAwait(false);
        var budgets = await GetBudgetsAsync(start, cancellationToken).ConfigureAwait(false);

        var currencies = accounts.Where(x => x.State != AccountState.Hidden).Select(x => x.Currency)
            .Concat(transactions.Select(x => x.Currency))
            .Concat(budgets.Select(x => x.Currency))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (currencies.Count > 1)
            throw new InvalidOperationException("Legacy dashboard aggregation cannot combine currencies. Use the currency-specific report/dashboard path.");

        var income = SumChecked(transactions.Where(x => x.AmountMinor > 0 && x.Type != TransactionType.Transfer).Select(x => x.AmountMinor));
        var expense = SumChecked(transactions.Where(x => x.AmountMinor < 0 && x.Type != TransactionType.Transfer).Select(x => ExpenseMagnitude(x.AmountMinor)));
        var totalBalance = SumChecked(accounts.Where(x => x.State != AccountState.Hidden).Select(x => x.BalanceMinor));
        var recent = transactions.OrderByDescending(x => x.OccurredAtUtc).Take(8)
            .Select(x => new TransactionListItem(x.Id, x.Type, x.AmountMinor, x.Currency, x.OccurredAtUtc, x.Account?.Name ?? string.Empty, x.Category?.Name, x.Merchant, x.Note))
            .ToList();
        var top = transactions.Where(x => x.AmountMinor < 0 && x.Type != TransactionType.Transfer)
            .GroupBy(x => x.Category?.Name ?? "Uncategorized")
            .Select(group => new CategorySpend(group.Key, SumChecked(group.Select(x => ExpenseMagnitude(x.AmountMinor)))))
            .OrderByDescending(x => x.AmountMinor)
            .Take(5)
            .ToList();
        var remaining = SumChecked(budgets.Select(x => Math.Max(0L, checked(x.PlannedMinor - x.ActualMinor))));
        return new DashboardSnapshot(totalBalance, income, expense, checked(income - expense), remaining, recent, top);
    }

    public async Task DeleteAllDataAsync(CancellationToken cancellationToken = default)
    {
        var result = await new FinanceDataResetService(_factory).DeleteAllFinanceDataAsync(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error ?? "Finora could not delete all finance data safely.");
    }

    private static void ValidateTransferPair(IReadOnlyList<FinanceTransaction> rows)
    {
        if (rows.Count != 2) throw new InvalidDataException("Linked transfer is incomplete.");
        var left = rows[0];
        var right = rows[1];
        if (left.Type != TransactionType.Transfer || right.Type != TransactionType.Transfer ||
            left.TransferGroupId is null || left.TransferGroupId != right.TransferGroupId ||
            !string.Equals(left.Currency, right.Currency, StringComparison.OrdinalIgnoreCase) ||
            left.CounterpartyAccountId != right.AccountId || right.CounterpartyAccountId != left.AccountId ||
            left.AccountId == right.AccountId || left.IsDeleted != right.IsDeleted ||
            checked(left.AmountMinor + right.AmountMinor) != 0 ||
            Math.Sign(left.AmountMinor) == Math.Sign(right.AmountMinor))
            throw new InvalidDataException("Linked transfer pair is inconsistent.");
    }

    private static long ExpenseMagnitude(long amountMinor)
    {
        if (amountMinor == long.MinValue)
            throw new InvalidDataException("Stored monetary amount is outside the supported range.");
        return amountMinor < 0 ? -amountMinor : 0L;
    }

    private static long SumChecked(IEnumerable<long> values)
    {
        long total = 0;
        foreach (var value in values) total = checked(total + value);
        return total;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
