using Finora.Domain;

namespace Finora.Infrastructure;

internal static class BackupGraphValidator
{
    public static void Validate(
        IReadOnlyCollection<Account> accounts,
        IReadOnlyCollection<FinanceTransaction> transactions,
        IReadOnlyCollection<TransactionSplit> splits,
        IReadOnlyCollection<Category> categories,
        IReadOnlyCollection<Tag> tags,
        IReadOnlyCollection<TransactionTag> transactionTags,
        IReadOnlyCollection<Budget> budgets,
        IReadOnlyCollection<BudgetPeriod> budgetPeriods,
        IReadOnlyCollection<SavingsGoal> goals,
        IReadOnlyCollection<GoalContribution> contributions,
        IReadOnlyCollection<RecurrenceRule> rules,
        IReadOnlyCollection<RecurrenceOccurrence> occurrences,
        IReadOnlyCollection<Attachment> attachments,
        IReadOnlyCollection<TransactionRevision> revisions,
        IReadOnlyCollection<AccountReconciliation> reconciliations,
        IReadOnlyCollection<NotificationSchedule> notifications,
        IReadOnlyCollection<AppSetting> settings)
    {
        var accountById = accounts.ToDictionary(x => x.Id);
        foreach (var account in accounts) DomainRules.ValidateAccount(account);

        foreach (var category in categories) DomainRules.ValidateCategory(category);
        foreach (var tag in tags) DomainRules.ValidateTag(tag);
        foreach (var link in transactionTags) DomainRules.ValidateTransactionTag(link);
        foreach (var split in splits) DomainRules.ValidateTransactionSplit(split);
        foreach (var attachment in attachments) DomainRules.ValidateAttachmentMetadata(attachment);
        foreach (var revision in revisions) DomainRules.ValidateTransactionRevision(revision);
        foreach (var reconciliation in reconciliations) DomainRules.ValidateReconciliation(reconciliation);
        foreach (var notification in notifications) DomainRules.ValidateNotificationSchedule(notification);
        foreach (var setting in settings) DomainRules.ValidateAppSetting(setting);

        var categoryById = categories.ToDictionary(x => x.Id);
        ValidateCategoryTree(categoryById);
        var tagIds = tags.Select(x => x.Id).ToHashSet();
        var ruleById = rules.ToDictionary(x => x.Id);
        var transactionById = transactions.ToDictionary(x => x.Id);
        var goalById = goals.ToDictionary(x => x.Id);
        var budgetById = budgets.ToDictionary(x => x.Id);

        foreach (var rule in rules)
        {
            DomainRules.ValidateRecurrenceRule(rule);
            var source = Require(accountById, rule.AccountId, "Recurring source account is missing.");
            EnsureCurrency(source.Currency, rule.Currency, "Recurring source account currency does not match the rule.");
            if (rule.Status == RecurrenceStatus.Active && source.State == AccountState.Archived)
                throw new InvalidDataException("Active recurring source account is archived.");

            if (rule.DestinationAccountId is Guid destinationId)
            {
                var destination = Require(accountById, destinationId, "Recurring destination account is missing.");
                EnsureCurrency(destination.Currency, rule.Currency, "Recurring destination account currency does not match the rule.");
                if (rule.Status == RecurrenceStatus.Active && destination.State == AccountState.Archived)
                    throw new InvalidDataException("Active recurring destination account is archived.");
            }

            if (rule.CategoryId is Guid ruleCategoryId)
            {
                var category = Require(categoryById, ruleCategoryId, "Recurring category is missing.");
                if (category.IsArchived) throw new InvalidDataException("Recurring rule references an archived category.");
            }
        }

        foreach (var transaction in transactions)
        {
            DomainRules.ValidateTransaction(transaction);
            var account = Require(accountById, transaction.AccountId, "Transaction account is missing.");
            EnsureCurrency(account.Currency, transaction.Currency, "Transaction currency does not match its account.");
            if (transaction.CategoryId is Guid categoryId && !categoryById.ContainsKey(categoryId))
                throw new InvalidDataException("Transaction category is missing.");
            if (transaction.RecurrenceRuleId is Guid ruleId && !ruleById.ContainsKey(ruleId))
                throw new InvalidDataException("Transaction recurrence rule is missing.");
        }
        ValidateTransferGroups(transactions);
        ValidateSplits(splits, transactionById, categoryById);
        ValidateTransactionTags(transactionTags, transactionById.Keys.ToHashSet(), tagIds);

        foreach (var budget in budgets)
        {
            DomainRules.ValidateBudget(budget);
            if (budget.Cadence == BudgetCadence.Custom && budgetPeriods.All(period => period.BudgetId != budget.Id))
                throw new InvalidDataException("Custom budget requires at least one explicit period.");
            if (budget.CategoryId is not Guid categoryId) continue;
            var category = Require(categoryById, categoryId, "Budget category is missing.");
            if (category.IsArchived && !budget.IsArchived) throw new InvalidDataException("Active budget references an archived category.");
            if (budget.Kind == BudgetKind.Subcategory && category.ParentId is null)
                throw new InvalidDataException("Subcategory budget references a root category.");
        }
        ValidateBudgetPeriods(budgetPeriods, budgetById);

        foreach (var goal in goals) DomainRules.ValidateSavingsGoal(goal);
        ValidateGoalContributions(contributions, goalById, transactionById);
        ValidateOccurrences(occurrences, ruleById, transactionById);

        foreach (var attachment in attachments)
        {
            if (!transactionById.ContainsKey(attachment.TransactionId))
                throw new InvalidDataException("Attachment transaction is missing.");
        }

        foreach (var revision in revisions)
        {
            if (!transactionById.ContainsKey(revision.TransactionId))
                throw new InvalidDataException("Transaction revision parent is missing.");
        }

        ValidateReconciliations(reconciliations, accountById, transactionById);

        if (settings.Any(x => string.Equals(x.Key, "schema.version", StringComparison.Ordinal) || x.Key.StartsWith("internal.", StringComparison.Ordinal)))
            throw new InvalidDataException("Backup contains internal settings that must not be restored from a snapshot.");
    }

    private static void ValidateCategoryTree(IReadOnlyDictionary<Guid, Category> categoryById)
    {
        foreach (var category in categoryById.Values)
        {
            if (category.ParentId is Guid parentId && !categoryById.ContainsKey(parentId))
                throw new InvalidDataException("Category parent is missing.");

            var visited = new HashSet<Guid>();
            Guid? current = category.Id;
            while (current is Guid id)
            {
                if (!visited.Add(id)) throw new InvalidDataException("Category hierarchy contains a cycle.");
                if (!categoryById.TryGetValue(id, out var node)) break;
                current = node.ParentId;
            }
        }
    }

    private static void ValidateTransferGroups(IReadOnlyCollection<FinanceTransaction> transactions)
    {
        foreach (var group in transactions.Where(x => x.TransferGroupId is not null).GroupBy(x => x.TransferGroupId!.Value))
        {
            var pair = group.ToList();
            if (pair.Count != 2) throw new InvalidDataException("Backup contains an incomplete transfer group.");
            var left = pair[0];
            var right = pair[1];
            if (left.Type != TransactionType.Transfer || right.Type != TransactionType.Transfer ||
                left.AccountId == right.AccountId ||
                left.CounterpartyAccountId != right.AccountId ||
                right.CounterpartyAccountId != left.AccountId ||
                !string.Equals(left.Currency, right.Currency, StringComparison.OrdinalIgnoreCase) ||
                left.IsDeleted != right.IsDeleted ||
                Math.Sign(left.AmountMinor) == Math.Sign(right.AmountMinor) ||
                checked(left.AmountMinor + right.AmountMinor) != 0)
                throw new InvalidDataException("Backup contains an inconsistent transfer pair.");
        }
    }

    private static void ValidateSplits(
        IReadOnlyCollection<TransactionSplit> splits,
        IReadOnlyDictionary<Guid, FinanceTransaction> transactionById,
        IReadOnlyDictionary<Guid, Category> categoryById)
    {
        foreach (var split in splits)
        {
            if (!transactionById.ContainsKey(split.TransactionId)) throw new InvalidDataException("Transaction split parent is missing.");
            if (split.CategoryId is Guid categoryId && !categoryById.ContainsKey(categoryId))
                throw new InvalidDataException("Transaction split category is missing.");
        }

        foreach (var group in splits.GroupBy(x => x.TransactionId))
        {
            var transaction = transactionById[group.Key];
            if (transaction.Type == TransactionType.Transfer) throw new InvalidDataException("Transfer rows cannot contain category splits.");
            long total = 0;
            foreach (var split in group)
            {
                DomainRules.ValidateTransactionSplit(split, transaction.Id, transaction.AmountMinor);
                total = checked(total + split.AmountMinor);
            }
            if (total != transaction.AmountMinor) throw new InvalidDataException("Transaction split total does not match the parent transaction.");
        }
    }

    private static void ValidateTransactionTags(
        IReadOnlyCollection<TransactionTag> links,
        IReadOnlySet<Guid> transactionIds,
        IReadOnlySet<Guid> tagIds)
    {
        var seen = new HashSet<(Guid TransactionId, Guid TagId)>();
        foreach (var link in links)
        {
            if (!transactionIds.Contains(link.TransactionId) || !tagIds.Contains(link.TagId))
                throw new InvalidDataException("Transaction-tag link references a missing row.");
            if (!seen.Add((link.TransactionId, link.TagId)))
                throw new InvalidDataException("Backup contains a duplicate transaction-tag link.");
        }
    }

    private static void ValidateBudgetPeriods(
        IReadOnlyCollection<BudgetPeriod> periods,
        IReadOnlyDictionary<Guid, Budget> budgetById)
    {
        var unique = new HashSet<(Guid BudgetId, DateOnly StartsOn, DateOnly EndsOn)>();
        foreach (var period in periods)
        {
            DomainRules.ValidateBudgetPeriod(period);
            if (!budgetById.ContainsKey(period.BudgetId)) throw new InvalidDataException("Budget period parent is missing.");
            if (!unique.Add((period.BudgetId, period.StartsOn, period.EndsOn)))
                throw new InvalidDataException("Backup contains a duplicate budget period.");
        }

        foreach (var group in periods.GroupBy(x => x.BudgetId))
        {
            var ordered = group.OrderBy(x => x.StartsOn).ThenBy(x => x.EndsOn).ToList();
            for (var index = 1; index < ordered.Count; index++)
            {
                if (ordered[index].StartsOn <= ordered[index - 1].EndsOn)
                    throw new InvalidDataException("Backup contains overlapping budget periods.");
            }
        }
    }

    private static void ValidateGoalContributions(
        IReadOnlyCollection<GoalContribution> contributions,
        IReadOnlyDictionary<Guid, SavingsGoal> goalById,
        IReadOnlyDictionary<Guid, FinanceTransaction> transactionById)
    {
        foreach (var contribution in contributions)
        {
            DomainRules.ValidateGoalContribution(contribution);
            var goal = Require(goalById, contribution.SavingsGoalId, "Goal contribution parent is missing.");
            if (contribution.TransactionId is Guid transactionId)
            {
                var transaction = Require(transactionById, transactionId, "Linked goal transaction is missing.");
                if (transaction.IsDeleted) throw new InvalidDataException("Linked goal transaction is deleted.");
                EnsureCurrency(transaction.Currency, goal.Currency, "Linked goal transaction currency does not match the goal.");
            }
        }

        foreach (var goal in goalById.Values)
        {
            long current = goal.StartingMinor;
            foreach (var contribution in contributions.Where(x => x.SavingsGoalId == goal.Id).OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.CreatedAtUtc).ThenBy(x => x.Id))
            {
                current = checked(current + contribution.AmountMinor);
                if (current < 0) throw new InvalidDataException("Savings goal contribution history falls below zero.");
            }
            if (goal.IsCompleted != (current >= goal.TargetMinor))
                throw new InvalidDataException("Savings goal completion state does not match contribution progress.");
        }
    }

    private static void ValidateOccurrences(
        IReadOnlyCollection<RecurrenceOccurrence> occurrences,
        IReadOnlyDictionary<Guid, RecurrenceRule> ruleById,
        IReadOnlyDictionary<Guid, FinanceTransaction> transactionById)
    {
        var unique = new HashSet<(Guid RuleId, DateOnly DueOn)>();
        foreach (var occurrence in occurrences)
        {
            DomainRules.ValidateRecurrenceOccurrence(occurrence);
            var rule = Require(ruleById, occurrence.RecurrenceRuleId, "Recurrence occurrence rule is missing.");
            if (!unique.Add((occurrence.RecurrenceRuleId, occurrence.DueOn)))
                throw new InvalidDataException("Backup contains a duplicate recurrence occurrence.");
            if (occurrence.PostponedTo is DateOnly postponed && rule.EndsOn is DateOnly endsOn && postponed > endsOn)
                throw new InvalidDataException("Postponed recurrence date is after the rule end date.");

            var generated = occurrence.GeneratedTransactionId is Guid generatedId
                ? Require(transactionById, generatedId, "Generated recurrence transaction is missing.")
                : null;
            if (generated is not null && (generated.RecurrenceRuleId != rule.Id || generated.IsDeleted))
                throw new InvalidDataException("Generated transaction does not belong to the recurrence rule or is deleted.");

            switch (occurrence.Status)
            {
                case OccurrenceStatus.Paid:
                    if (occurrence.PaidAmountMinor != rule.AmountMinor || generated is null)
                        throw new InvalidDataException("Paid recurrence occurrence is incomplete.");
                    break;
                case OccurrenceStatus.PartiallyPaid:
                    if (occurrence.PaidAmountMinor is not long partial || partial <= 0 || partial >= rule.AmountMinor || generated is null)
                        throw new InvalidDataException("Partially paid recurrence occurrence is invalid.");
                    break;
            }
        }
    }

    private static void ValidateReconciliations(
        IReadOnlyCollection<AccountReconciliation> reconciliations,
        IReadOnlyDictionary<Guid, Account> accountById,
        IReadOnlyDictionary<Guid, FinanceTransaction> transactionById)
    {
        foreach (var reconciliation in reconciliations)
        {
            DomainRules.ValidateReconciliation(reconciliation);
            if (!accountById.ContainsKey(reconciliation.AccountId)) throw new InvalidDataException("Reconciliation account is missing.");
            if (!reconciliation.AdjustmentCreated) continue;

            var adjustment = Require(transactionById, reconciliation.AdjustmentTransactionId!.Value, "Reconciliation adjustment transaction is missing.");
            if (adjustment.IsDeleted || adjustment.Type != TransactionType.Adjustment || adjustment.AccountId != reconciliation.AccountId || adjustment.AmountMinor != reconciliation.DifferenceMinor)
                throw new InvalidDataException("Reconciliation adjustment transaction does not match the reconciliation.");
        }
    }

    private static TValue Require<TValue>(IReadOnlyDictionary<Guid, TValue> values, Guid id, string message) where TValue : class
    {
        if (id == Guid.Empty || !values.TryGetValue(id, out var value)) throw new InvalidDataException(message);
        return value;
    }

    private static void EnsureCurrency(string left, string right, string message)
    {
        if (!string.Equals(left, right, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException(message);
    }
}
