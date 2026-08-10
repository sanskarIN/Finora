using System.Data.Common;
using System.Security.Cryptography;
using Finora.Application;
using Finora.Domain;
using Microsoft.EntityFrameworkCore;

namespace Finora.Infrastructure;

public sealed class DataIntegrityService(
    IDbContextFactory<FinoraDbContext> factory,
    string appDataRoot) : IDataIntegrityService
{
    private readonly string _appDataRoot = Path.GetFullPath(appDataRoot);
    private string AttachmentRoot => Path.GetFullPath(Path.Combine(_appDataRoot, "attachments"));

    public async Task<IntegrityReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var issues = new List<IntegrityIssue>();
        await using var db = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var databaseIntegrityPassed = await CheckSqliteIntegrityAsync(db, issues, cancellationToken).ConfigureAwait(false);
        var foreignKeysPassed = await CheckForeignKeysAsync(db, issues, cancellationToken).ConfigureAwait(false);

        var accounts = await db.Accounts.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var accountById = accounts.ToDictionary(x => x.Id);
        CheckAccounts(accounts, issues);

        var categories = await db.Categories.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var categoryById = categories.ToDictionary(x => x.Id);
        CheckCategoryTree(categories, issues);

        var transactions = await db.Transactions.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var transactionById = transactions.ToDictionary(x => x.Id);
        CheckTransactionValues(transactions, issues);
        CheckTransactionAccounts(transactions, accountById, issues);
        CheckTransactionCategories(transactions, categoryById, issues);
        CheckTransferPairs(transactions, issues);

        var splits = await db.TransactionSplits.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        CheckSplits(splits, transactionById, categoryById, issues);

        var budgets = await db.Budgets.AsNoTracking()
            .Include(x => x.Periods)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        CheckBudgets(budgets, categoryById, issues);

        var goals = await db.SavingsGoals.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var contributions = await db.GoalContributions.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        CheckGoalsAndContributions(goals, contributions, transactionById, issues);

        var rules = await db.RecurrenceRules.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var ruleById = rules.ToDictionary(x => x.Id);
        CheckRecurrenceRules(rules, accountById, categoryById, issues);

        var occurrences = await db.RecurrenceOccurrences.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        CheckRecurrenceOccurrences(occurrences, ruleById, transactionById, issues);

        var reconciliations = await db.AccountReconciliations.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        CheckReconciliations(reconciliations, accountById, transactionById, issues);

        var attachments = await db.Attachments.AsNoTracking()
            .Select(x => new AttachmentRow(x.Id, x.TransactionId, x.RelativePath, x.SizeBytes, x.Sha256))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        await CheckAttachmentsAsync(attachments, transactionById.Keys.ToHashSet(), issues, cancellationToken).ConfigureAwait(false);

        return new IntegrityReport(
            DateTimeOffset.UtcNow,
            databaseIntegrityPassed,
            foreignKeysPassed,
            accounts.Count,
            transactions.Count,
            attachments.Count,
            occurrences.Count,
            issues);
    }

    private static async Task<bool> CheckSqliteIntegrityAsync(
        FinoraDbContext db,
        ICollection<IntegrityIssue> issues,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check;";
        var failures = 0;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var value = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            if (!string.Equals(value, "ok", StringComparison.OrdinalIgnoreCase)) failures++;
        }

        if (failures == 0) return true;
        issues.Add(new IntegrityIssue(
            "SQLITE_INTEGRITY",
            IntegritySeverity.Error,
            "SQLite reported an internal database integrity problem. Create a backup only after reviewing recovery options.",
            failures));
        return false;
    }

    private static async Task<bool> CheckForeignKeysAsync(
        FinoraDbContext db,
        ICollection<IntegrityIssue> issues,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_key_check;";
        var violations = 0;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) violations++;

        if (violations == 0) return true;
        issues.Add(new IntegrityIssue(
            "FOREIGN_KEY_VIOLATION",
            IntegritySeverity.Error,
            "One or more local records reference missing parent records.",
            violations));
        return false;
    }

    private static void CheckAccounts(IReadOnlyCollection<Account> accounts, ICollection<IntegrityIssue> issues)
    {
        var invalid = 0;
        foreach (var account in accounts)
        {
            try { DomainRules.ValidateAccount(account); }
            catch (Exception exception) when (IsValidationException(exception)) { invalid++; }
        }
        AddIssueIfAny(issues, "ACCOUNT_INVALID", invalid, "One or more account records violate Finora account invariants.");
    }

    private static void CheckTransactionValues(
        IReadOnlyCollection<FinanceTransaction> transactions,
        ICollection<IntegrityIssue> issues)
    {
        var unsupportedAmounts = transactions.Count(x => x.AmountMinor is 0 or long.MinValue);
        var semanticSigns = transactions.Count(x =>
            (x.Type == TransactionType.Expense && x.AmountMinor >= 0) ||
            (x.Type is TransactionType.Income or TransactionType.Refund && x.AmountMinor <= 0));
        var invalidCurrencies = 0;
        var invalidDates = transactions.Count(x => x.OccurredAtUtc == default);
        var invalidLinks = transactions.Count(x =>
            (x.Type == TransactionType.Transfer && (x.TransferGroupId is null || x.CounterpartyAccountId is null || x.CounterpartyAccountId == x.AccountId)) ||
            (x.Type != TransactionType.Transfer && (x.TransferGroupId is not null || x.CounterpartyAccountId is not null)));

        foreach (var transaction in transactions)
        {
            try { DomainRules.ValidateCurrency(transaction.Currency); }
            catch (ArgumentException) { invalidCurrencies++; }
        }

        AddIssueIfAny(issues, "TRANSACTION_AMOUNT_INVALID", unsupportedAmounts, "Transactions contain a zero or unsupported extreme minor-unit amount.");
        AddIssueIfAny(issues, "TRANSACTION_SIGN_INVALID", semanticSigns, "Expense, income, or refund rows use an invalid amount sign.");
        AddIssueIfAny(issues, "TRANSACTION_CURRENCY_INVALID", invalidCurrencies, "Transactions contain an invalid currency code.");
        AddIssueIfAny(issues, "TRANSACTION_DATE_INVALID", invalidDates, "Transactions contain an invalid date/time value.");
        AddIssueIfAny(issues, "TRANSACTION_LINK_INVALID", invalidLinks, "Transactions contain invalid transfer linkage metadata.");
    }

    private static void CheckTransactionAccounts(
        IReadOnlyCollection<FinanceTransaction> transactions,
        IReadOnlyDictionary<Guid, Account> accountById,
        ICollection<IntegrityIssue> issues)
    {
        var missingAccounts = 0;
        var currencyMismatches = 0;
        var missingCounterparties = 0;

        foreach (var transaction in transactions)
        {
            if (!accountById.TryGetValue(transaction.AccountId, out var account))
            {
                missingAccounts++;
            }
            else if (!string.Equals(account.Currency, transaction.Currency, StringComparison.OrdinalIgnoreCase))
            {
                currencyMismatches++;
            }

            if (transaction.Type == TransactionType.Transfer && transaction.CounterpartyAccountId is Guid counterpartyId &&
                !accountById.ContainsKey(counterpartyId))
                missingCounterparties++;
        }

        AddIssueIfAny(issues, "TRANSACTION_ACCOUNT_MISSING", missingAccounts, "Transactions reference an account that is not present.");
        AddIssueIfAny(issues, "TRANSACTION_CURRENCY_MISMATCH", currencyMismatches, "Transaction currency does not match its account currency.");
        AddIssueIfAny(issues, "TRANSFER_COUNTERPARTY_MISSING", missingCounterparties, "Transfer rows reference a counterparty account that is not present.");
    }

    private static void CheckTransactionCategories(
        IReadOnlyCollection<FinanceTransaction> transactions,
        IReadOnlyDictionary<Guid, Category> categoryById,
        ICollection<IntegrityIssue> issues)
    {
        var missing = transactions.Count(x => x.CategoryId is Guid id && !categoryById.ContainsKey(id));
        AddIssueIfAny(issues, "TRANSACTION_CATEGORY_MISSING", missing, "Transactions reference a category that is not present.");
    }

    private static void CheckTransferPairs(
        IReadOnlyCollection<FinanceTransaction> transactions,
        ICollection<IntegrityIssue> issues)
    {
        var broken = 0;
        foreach (var group in transactions.Where(x => x.TransferGroupId is not null).GroupBy(x => x.TransferGroupId!.Value))
        {
            var pair = group.ToList();
            if (pair.Count != 2)
            {
                broken++;
                continue;
            }

            var left = pair[0];
            var right = pair[1];
            var balancesToZero = false;
            try { balancesToZero = checked(left.AmountMinor + right.AmountMinor) == 0; }
            catch (OverflowException) { }

            var valid =
                left.Type == TransactionType.Transfer &&
                right.Type == TransactionType.Transfer &&
                left.AccountId != right.AccountId &&
                balancesToZero &&
                Math.Sign(left.AmountMinor) != Math.Sign(right.AmountMinor) &&
                string.Equals(left.Currency, right.Currency, StringComparison.OrdinalIgnoreCase) &&
                left.CounterpartyAccountId == right.AccountId &&
                right.CounterpartyAccountId == left.AccountId &&
                left.IsDeleted == right.IsDeleted;
            if (!valid) broken++;
        }

        AddIssueIfAny(issues, "TRANSFER_PAIR_BROKEN", broken, "Linked transfer pairs are incomplete or do not balance to zero.");
    }

    private static void CheckSplits(
        IReadOnlyCollection<TransactionSplit> splits,
        IReadOnlyDictionary<Guid, FinanceTransaction> transactionById,
        IReadOnlyDictionary<Guid, Category> categoryById,
        ICollection<IntegrityIssue> issues)
    {
        var invalidGroups = 0;
        var missingCategories = 0;

        foreach (var split in splits)
        {
            if (split.CategoryId is Guid categoryId && !categoryById.ContainsKey(categoryId)) missingCategories++;
        }

        foreach (var group in splits.GroupBy(x => x.TransactionId))
        {
            if (!transactionById.TryGetValue(group.Key, out var transaction))
            {
                invalidGroups++;
                continue;
            }

            if (transaction.Type == TransactionType.Transfer)
            {
                invalidGroups++;
                continue;
            }

            long total = 0;
            var invalid = false;
            try
            {
                foreach (var split in group)
                {
                    if (split.AmountMinor is 0 or long.MinValue || Math.Sign(split.AmountMinor) != Math.Sign(transaction.AmountMinor))
                    {
                        invalid = true;
                        break;
                    }
                    total = checked(total + split.AmountMinor);
                }
            }
            catch (OverflowException)
            {
                invalid = true;
            }

            if (invalid || total != transaction.AmountMinor) invalidGroups++;
        }

        AddIssueIfAny(issues, "TRANSACTION_SPLIT_TOTAL", invalidGroups, "One or more split transactions have an invalid sign/value or do not add up to the parent amount.");
        AddIssueIfAny(issues, "TRANSACTION_SPLIT_CATEGORY_MISSING", missingCategories, "Transaction splits reference a category that is not present.");
    }

    private static void CheckCategoryTree(IReadOnlyCollection<Category> categories, ICollection<IntegrityIssue> issues)
    {
        var parentById = categories.ToDictionary(x => x.Id, x => x.ParentId);
        var missingParents = categories.Count(x => x.ParentId is Guid parentId && !parentById.ContainsKey(parentId));
        var invalidNames = categories.Count(x => string.IsNullOrWhiteSpace(x.Name) || x.Name.Trim().Length > 120);
        var cyclic = new HashSet<Guid>();

        foreach (var category in categories)
        {
            var path = new HashSet<Guid>();
            Guid? current = category.Id;
            while (current is Guid id)
            {
                if (!path.Add(id))
                {
                    foreach (var member in path) cyclic.Add(member);
                    break;
                }
                if (!parentById.TryGetValue(id, out current)) break;
            }
        }

        AddIssueIfAny(issues, "CATEGORY_PARENT_MISSING", missingParents, "Categories reference a parent category that is not present.");
        AddIssueIfAny(issues, "CATEGORY_INVALID", invalidNames, "Categories contain invalid names.");
        AddIssueIfAny(issues, "CATEGORY_CYCLE", cyclic.Count, "The category hierarchy contains a parent/child cycle.");
    }

    private static void CheckBudgets(
        IReadOnlyCollection<Budget> budgets,
        IReadOnlyDictionary<Guid, Category> categoryById,
        ICollection<IntegrityIssue> issues)
    {
        var invalid = 0;
        foreach (var budget in budgets)
        {
            var failed = false;
            try
            {
                DomainRules.ValidateBudget(budget);
                if (budget.Cadence == BudgetCadence.Custom && budget.Periods.Count == 0)
                    throw new InvalidOperationException("Custom budgets require an explicit period.");

                if (budget.CategoryId is Guid categoryId)
                {
                    if (!categoryById.TryGetValue(categoryId, out var category))
                        throw new InvalidOperationException("Budget category is missing.");
                    if (!budget.IsArchived && category.IsArchived)
                        throw new InvalidOperationException("Active budget references an archived category.");
                    if (budget.Kind == BudgetKind.Subcategory && category.ParentId is null)
                        throw new InvalidOperationException("Subcategory budget references a root category.");
                }

                foreach (var period in budget.Periods)
                {
                    var rollover = budget.RolloverEnabled ? period.RolloverMinor : 0L;
                    if (checked(period.PlannedMinor + rollover) <= 0)
                        throw new InvalidOperationException("Budget effective planned amount must remain positive.");
                }
            }
            catch (Exception exception) when (IsValidationException(exception))
            {
                failed = true;
            }
            if (failed) invalid++;
        }

        AddIssueIfAny(issues, "BUDGET_INVALID", invalid, "One or more budgets or budget periods violate Finora budget invariants.");
    }

    private static void CheckGoalsAndContributions(
        IReadOnlyCollection<SavingsGoal> goals,
        IReadOnlyCollection<GoalContribution> contributions,
        IReadOnlyDictionary<Guid, FinanceTransaction> transactionById,
        ICollection<IntegrityIssue> issues)
    {
        var goalById = goals.ToDictionary(x => x.Id);
        var invalidGoals = 0;
        var invalidContributions = 0;
        var invalidCompletionState = 0;

        foreach (var goal in goals)
        {
            try { DomainRules.ValidateSavingsGoal(goal); }
            catch (Exception exception) when (IsValidationException(exception)) { invalidGoals++; }
        }

        foreach (var contribution in contributions)
        {
            try
            {
                DomainRules.ValidateGoalContribution(contribution);
                if (!goalById.TryGetValue(contribution.SavingsGoalId, out var goal))
                    throw new InvalidOperationException("Goal contribution parent is missing.");
                if (contribution.TransactionId is Guid transactionId)
                {
                    if (!transactionById.TryGetValue(transactionId, out var transaction) || transaction.IsDeleted)
                        throw new InvalidOperationException("Linked goal transaction is missing or deleted.");
                    if (!string.Equals(transaction.Currency, goal.Currency, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Linked goal transaction currency differs from goal currency.");
                }
            }
            catch (Exception exception) when (IsValidationException(exception))
            {
                invalidContributions++;
            }
        }

        foreach (var goal in goals)
        {
            var historyValid = true;
            var current = goal.StartingMinor;
            try
            {
                foreach (var contribution in contributions
                    .Where(x => x.SavingsGoalId == goal.Id)
                    .OrderBy(x => x.OccurredAtUtc)
                    .ThenBy(x => x.CreatedAtUtc)
                    .ThenBy(x => x.Id))
                {
                    current = checked(current + contribution.AmountMinor);
                    if (current < 0)
                    {
                        historyValid = false;
                        break;
                    }
                }
            }
            catch (OverflowException)
            {
                historyValid = false;
            }

            if (!historyValid)
            {
                invalidContributions++;
                continue;
            }

            if (goal.TargetMinor > 0 && goal.IsCompleted != (current >= goal.TargetMinor))
                invalidCompletionState++;
        }

        AddIssueIfAny(issues, "GOAL_INVALID", invalidGoals, "One or more savings goals violate Finora goal invariants.");
        AddIssueIfAny(issues, "GOAL_CONTRIBUTION_INVALID", invalidContributions, "Savings goal contribution history contains an invalid relationship, currency, amount, or running balance.");
        AddIssueIfAny(issues, "GOAL_STATE_INVALID", invalidCompletionState, "Savings goal completion state does not match its current progress.");
    }

    private static void CheckRecurrenceRules(
        IReadOnlyCollection<RecurrenceRule> rules,
        IReadOnlyDictionary<Guid, Account> accountById,
        IReadOnlyDictionary<Guid, Category> categoryById,
        ICollection<IntegrityIssue> issues)
    {
        var invalidRules = 0;
        var invalidRelations = 0;

        foreach (var rule in rules)
        {
            try { DomainRules.ValidateRecurrenceRule(rule); }
            catch (Exception exception) when (IsValidationException(exception)) { invalidRules++; }

            var relationInvalid = false;
            if (!accountById.TryGetValue(rule.AccountId, out var source) ||
                !string.Equals(source.Currency, rule.Currency, StringComparison.OrdinalIgnoreCase) ||
                (rule.Status == RecurrenceStatus.Active && source.State == AccountState.Archived))
            {
                relationInvalid = true;
            }

            if (rule.DestinationAccountId is Guid destinationId)
            {
                if (!accountById.TryGetValue(destinationId, out var destination) ||
                    !string.Equals(destination.Currency, rule.Currency, StringComparison.OrdinalIgnoreCase) ||
                    (rule.Status == RecurrenceStatus.Active && destination.State == AccountState.Archived))
                    relationInvalid = true;
            }

            if (rule.CategoryId is Guid categoryId)
            {
                if (!categoryById.TryGetValue(categoryId, out var category) ||
                    (rule.Status == RecurrenceStatus.Active && category.IsArchived))
                    relationInvalid = true;
            }

            if (relationInvalid) invalidRelations++;
        }

        AddIssueIfAny(issues, "RECURRENCE_RULE_INVALID", invalidRules, "One or more recurring rules violate Finora recurrence invariants.");
        AddIssueIfAny(issues, "RECURRENCE_RELATION_INVALID", invalidRelations, "Recurring rules have an invalid account, currency, destination, category, or active-state dependency.");
    }

    private static void CheckRecurrenceOccurrences(
        IReadOnlyCollection<RecurrenceOccurrence> occurrences,
        IReadOnlyDictionary<Guid, RecurrenceRule> ruleById,
        IReadOnlyDictionary<Guid, FinanceTransaction> transactionById,
        ICollection<IntegrityIssue> issues)
    {
        var duplicateCount = occurrences.GroupBy(x => (x.RecurrenceRuleId, x.DueOn)).Count(group => group.Count() > 1);
        var missingGeneratedTransactions = 0;
        var invalidState = 0;

        foreach (var occurrence in occurrences)
        {
            if (!ruleById.TryGetValue(occurrence.RecurrenceRuleId, out var rule))
            {
                invalidState++;
                continue;
            }

            FinanceTransaction? generated = null;
            if (occurrence.GeneratedTransactionId is Guid generatedId)
            {
                if (!transactionById.TryGetValue(generatedId, out generated))
                {
                    missingGeneratedTransactions++;
                }
                else if (generated.RecurrenceRuleId != rule.Id || generated.IsDeleted)
                {
                    invalidState++;
                }
            }

            var stateValid = occurrence.Status switch
            {
                OccurrenceStatus.Paid =>
                    generated is not null && occurrence.PaidAmountMinor == rule.AmountMinor,
                OccurrenceStatus.PartiallyPaid =>
                    generated is not null && occurrence.PaidAmountMinor is long partial && partial > 0 && partial < rule.AmountMinor,
                OccurrenceStatus.Pending or OccurrenceStatus.Skipped =>
                    generated is null && occurrence.PaidAmountMinor is null or 0 && occurrence.PostponedTo is null,
                OccurrenceStatus.Postponed =>
                    generated is null && occurrence.PaidAmountMinor is null or 0 && occurrence.PostponedTo is DateOnly postponed && postponed > occurrence.DueOn && (rule.EndsOn is null || postponed <= rule.EndsOn),
                _ => false
            };
            if (!stateValid) invalidState++;
        }

        AddIssueIfAny(issues, "RECURRENCE_DUPLICATE", duplicateCount, "Duplicate recurrence occurrences exist for the same rule and due date.");
        AddIssueIfAny(issues, "RECURRENCE_TRANSACTION_MISSING", missingGeneratedTransactions, "Recurring occurrences reference a generated transaction that is missing.");
        AddIssueIfAny(issues, "RECURRENCE_STATE_INVALID", invalidState, "Recurring occurrence payment or postponement state is inconsistent with its rule and generated transaction.");
    }

    private static void CheckReconciliations(
        IReadOnlyCollection<AccountReconciliation> reconciliations,
        IReadOnlyDictionary<Guid, Account> accountById,
        IReadOnlyDictionary<Guid, FinanceTransaction> transactionById,
        ICollection<IntegrityIssue> issues)
    {
        var invalid = 0;
        foreach (var reconciliation in reconciliations)
        {
            var failed = false;
            try
            {
                if (!accountById.ContainsKey(reconciliation.AccountId))
                    throw new InvalidOperationException("Reconciliation account is missing.");
                if (reconciliation.StatementDateUtc == default || reconciliation.CompletedAtUtc == default)
                    throw new InvalidOperationException("Reconciliation timestamp is invalid.");
                if (reconciliation.DifferenceMinor == long.MinValue ||
                    checked(reconciliation.StatementBalanceMinor - reconciliation.BookBalanceMinor) != reconciliation.DifferenceMinor)
                    throw new InvalidOperationException("Reconciliation difference does not match its balances.");

                if (reconciliation.AdjustmentCreated)
                {
                    if (reconciliation.AdjustmentTransactionId is not Guid adjustmentId ||
                        !transactionById.TryGetValue(adjustmentId, out var adjustment) ||
                        adjustment.IsDeleted ||
                        adjustment.Type != TransactionType.Adjustment ||
                        adjustment.AccountId != reconciliation.AccountId ||
                        adjustment.AmountMinor != reconciliation.DifferenceMinor)
                        throw new InvalidOperationException("Reconciliation adjustment link is invalid.");
                }
                else if (reconciliation.AdjustmentTransactionId is not null)
                {
                    throw new InvalidOperationException("Reconciliation contains an unexpected adjustment link.");
                }
            }
            catch (Exception exception) when (IsValidationException(exception))
            {
                failed = true;
            }
            if (failed) invalid++;
        }

        AddIssueIfAny(issues, "RECONCILIATION_INVALID", invalid, "One or more reconciliation records contain invalid balances, timestamps, or adjustment linkage.");
    }

    private async Task CheckAttachmentsAsync(
        IReadOnlyCollection<AttachmentRow> attachments,
        IReadOnlySet<Guid> transactionIds,
        ICollection<IntegrityIssue> issues,
        CancellationToken cancellationToken)
    {
        var unsafePaths = 0;
        var missingParents = 0;
        var missingFiles = 0;
        var sizeMismatches = 0;
        var hashMismatches = 0;
        var invalidHashes = 0;

        foreach (var attachment in attachments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!transactionIds.Contains(attachment.TransactionId)) missingParents++;

            string fullPath;
            try { fullPath = ResolveSafePath(attachment.RelativePath); }
            catch (InvalidDataException) { unsafePaths++; continue; }

            if (!File.Exists(fullPath))
            {
                missingFiles++;
                continue;
            }

            var info = new FileInfo(fullPath);
            if (attachment.SizeBytes <= 0 || info.Length != attachment.SizeBytes)
            {
                sizeMismatches++;
                continue;
            }

            if (attachment.Sha256 is null || attachment.Sha256.Length != 32)
            {
                invalidHashes++;
                continue;
            }

            await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var actualHash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
            if (!CryptographicOperations.FixedTimeEquals(actualHash, attachment.Sha256)) hashMismatches++;
        }

        AddIssueIfAny(issues, "ATTACHMENT_PATH_UNSAFE", unsafePaths, "Attachment metadata contains a path outside Finora private receipt storage or through a symbolic link.");
        AddIssueIfAny(issues, "ATTACHMENT_TRANSACTION_MISSING", missingParents, "Attachment metadata references a transaction that is not present.");
        AddIssueIfAny(issues, "ATTACHMENT_FILE_MISSING", missingFiles, "Attachment metadata exists but the local receipt file is missing.");
        AddIssueIfAny(issues, "ATTACHMENT_SIZE_MISMATCH", sizeMismatches, "Attachment file size no longer matches stored metadata.");
        AddIssueIfAny(issues, "ATTACHMENT_HASH_INVALID", invalidHashes, "Attachment checksum metadata is missing or invalid.");
        AddIssueIfAny(issues, "ATTACHMENT_HASH_MISMATCH", hashMismatches, "Attachment file checksum no longer matches stored metadata.");
    }

    private string ResolveSafePath(string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var prefix = "attachments" + Path.DirectorySeparatorChar;
        if (!normalized.StartsWith(prefix, PathSafety.Comparison))
            throw new InvalidDataException("Attachment path is outside Finora private receipt storage.");
        return PathSafety.ResolveDescendantWithoutLinks(AttachmentRoot, normalized[prefix.Length..], "Attachment path escaped app-private receipt storage or traversed a link.");
    }

    private static void AddIssueIfAny(ICollection<IntegrityIssue> issues, string code, int count, string message)
    {
        if (count > 0) issues.Add(new IntegrityIssue(code, IntegritySeverity.Error, message, count));
    }

    private static bool IsValidationException(Exception exception)
        => exception is ArgumentException or InvalidOperationException or OverflowException or InvalidDataException;

    private static async Task EnsureOpenAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
    }

    private sealed record AttachmentRow(Guid Id, Guid TransactionId, string RelativePath, long SizeBytes, byte[]? Sha256);
}
