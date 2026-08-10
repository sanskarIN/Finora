namespace Finora.Domain;

public static class DomainRules
{
    private static readonly HashSet<string> AllowedAttachmentContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/heic", "image/heif", "application/pdf"
    };

    public static void ValidateAccount(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);
        if (string.IsNullOrWhiteSpace(account.Name)) throw new ArgumentException("Account name is required.", nameof(account));
        if (account.Name.Trim().Length > 120) throw new ArgumentException("Account name cannot exceed 120 characters.", nameof(account));
        ValidateCurrency(account.Currency);

        if (account.BillingDay is < 1 or > 31) throw new ArgumentOutOfRangeException(nameof(account.BillingDay));
        if (account.Type != AccountType.CreditCard && (account.CreditLimitMinor is not null || account.BillingDay is not null))
            throw new InvalidOperationException("Credit settings are valid only for credit-card accounts.");
        if (account.CreditLimitMinor is < 0)
            throw new ArgumentOutOfRangeException(nameof(account.CreditLimitMinor), "Credit limit cannot be negative.");
    }

    public static void ValidateTransaction(FinanceTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        if (transaction.AmountMinor == 0) throw new ArgumentException("Transaction amount cannot be zero.", nameof(transaction));
        if (transaction.AmountMinor == long.MinValue) throw new ArgumentOutOfRangeException(nameof(transaction.AmountMinor), "Transaction amount is outside the supported range.");
        ValidateCurrency(transaction.Currency);
        if (transaction.AccountId == Guid.Empty) throw new ArgumentException("Transaction account is required.", nameof(transaction));
        if (transaction.OccurredAtUtc == default) throw new ArgumentException("Transaction date/time is required.", nameof(transaction));

        switch (transaction.Type)
        {
            case TransactionType.Expense when transaction.AmountMinor >= 0:
                throw new InvalidOperationException("Expense amounts must be negative minor units.");
            case TransactionType.Income or TransactionType.Refund when transaction.AmountMinor <= 0:
                throw new InvalidOperationException("Income and refund amounts must be positive minor units.");
            case TransactionType.Transfer when transaction.TransferGroupId is null || transaction.CounterpartyAccountId is null:
                throw new InvalidOperationException("Transfer rows require a transfer group and counterparty account.");
            case TransactionType.Transfer when transaction.CounterpartyAccountId == transaction.AccountId:
                throw new InvalidOperationException("Transfer rows require different source and counterparty accounts.");
        }

        if (transaction.Type != TransactionType.Transfer && (transaction.TransferGroupId is not null || transaction.CounterpartyAccountId is not null))
            throw new InvalidOperationException("Only transfer rows may contain transfer linkage.");
        if (transaction.IsDeleted != (transaction.DeletedAtUtc is not null))
            throw new InvalidOperationException("Transaction deletion state and deletion timestamp must agree.");

        if (transaction.Splits.Count == 0) return;
        if (transaction.Type == TransactionType.Transfer)
            throw new InvalidOperationException("Transfer rows cannot contain category splits.");

        long splitTotal = 0;
        foreach (var split in transaction.Splits)
        {
            ValidateTransactionSplit(split, transaction.Id, transaction.AmountMinor);
            splitTotal = checked(splitTotal + split.AmountMinor);
        }

        if (splitTotal != transaction.AmountMinor)
            throw new InvalidOperationException("Split amounts must equal the transaction amount.");
    }

    public static void ValidateTransactionSplit(TransactionSplit split, Guid? expectedTransactionId = null, long? parentAmountMinor = null)
    {
        ArgumentNullException.ThrowIfNull(split);
        if (split.TransactionId == Guid.Empty) throw new ArgumentException("Transaction split must reference a transaction.", nameof(split));
        if (expectedTransactionId is Guid transactionId && transactionId != Guid.Empty && split.TransactionId != transactionId)
            throw new InvalidOperationException("Transaction split does not belong to the expected transaction.");
        if (split.AmountMinor is 0 or long.MinValue)
            throw new ArgumentOutOfRangeException(nameof(split.AmountMinor), "Split amount is outside the supported range.");
        if (parentAmountMinor is long parentAmount && parentAmount != 0 && Math.Sign(split.AmountMinor) != Math.Sign(parentAmount))
            throw new InvalidOperationException("Split amount sign must match the parent transaction.");
    }

    public static void ValidateCategory(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);
        if (string.IsNullOrWhiteSpace(category.Name)) throw new ArgumentException("Category name is required.", nameof(category));
        if (category.Name.Trim().Length > 120) throw new ArgumentException("Category name cannot exceed 120 characters.", nameof(category));
        if (category.ParentId == category.Id) throw new InvalidOperationException("A category cannot be its own parent.");
        if (category.SortOrder < 0) throw new ArgumentOutOfRangeException(nameof(category.SortOrder), "Category sort order cannot be negative.");
        if (string.IsNullOrWhiteSpace(category.Icon) || category.Icon.Trim().Length > 80)
            throw new ArgumentException("Category icon identifier must contain 1–80 characters.", nameof(category));
    }

    public static void ValidateTag(Tag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        if (string.IsNullOrWhiteSpace(tag.Name)) throw new ArgumentException("Tag name is required.", nameof(tag));
        if (tag.Name.Trim().Length > 80) throw new ArgumentException("Tag name cannot exceed 80 characters.", nameof(tag));
        if (tag.ColorLabel?.Trim().Length > 32) throw new ArgumentException("Tag color label cannot exceed 32 characters.", nameof(tag));
    }

    public static void ValidateTransactionTag(TransactionTag link)
    {
        ArgumentNullException.ThrowIfNull(link);
        if (link.TransactionId == Guid.Empty || link.TagId == Guid.Empty)
            throw new ArgumentException("Transaction-tag links require both identifiers.", nameof(link));
    }

    public static void ValidateBudget(Budget budget)
    {
        ArgumentNullException.ThrowIfNull(budget);
        if (string.IsNullOrWhiteSpace(budget.Name)) throw new ArgumentException("Budget name is required.", nameof(budget));
        if (budget.Name.Trim().Length > 120) throw new ArgumentException("Budget name cannot exceed 120 characters.", nameof(budget));
        if (budget.LimitMinor <= 0) throw new ArgumentOutOfRangeException(nameof(budget.LimitMinor), "Budget limit must be positive.");
        ValidateCurrency(budget.Currency);
        if (budget.WarningThresholdPercent is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(budget.WarningThresholdPercent));
        if (budget.Kind == BudgetKind.Overall && budget.CategoryId is not null)
            throw new InvalidOperationException("Overall budgets cannot target a category.");
        if (budget.Kind is BudgetKind.Category or BudgetKind.Subcategory && budget.CategoryId is null)
            throw new InvalidOperationException("Category and subcategory budgets require a category.");

        foreach (var period in budget.Periods)
            ValidateBudgetPeriod(period, budget.Id);
        var orderedPeriods = budget.Periods.OrderBy(x => x.StartsOn).ThenBy(x => x.EndsOn).ToList();
        for (var index = 1; index < orderedPeriods.Count; index++)
        {
            if (orderedPeriods[index].StartsOn <= orderedPeriods[index - 1].EndsOn)
                throw new InvalidOperationException("Budget periods cannot overlap.");
        }
    }

    public static void ValidateBudgetPeriod(BudgetPeriod period, Guid? expectedBudgetId = null)
    {
        ArgumentNullException.ThrowIfNull(period);
        if (period.BudgetId == Guid.Empty) throw new ArgumentException("Budget period must reference a budget.", nameof(period));
        if (expectedBudgetId is Guid budgetId && budgetId != Guid.Empty && period.BudgetId != budgetId)
            throw new InvalidOperationException("Budget period does not belong to the expected budget.");
        if (period.StartsOn == default || period.EndsOn == default || period.EndsOn < period.StartsOn)
            throw new InvalidOperationException("Budget period dates are invalid.");
        if (period.PlannedMinor <= 0) throw new ArgumentOutOfRangeException(nameof(period.PlannedMinor), "Budget period planned amount must be positive.");
        if (period.RolloverMinor == long.MinValue) throw new ArgumentOutOfRangeException(nameof(period.RolloverMinor), "Budget rollover amount is outside the supported range.");
    }

    public static void ValidateSavingsGoal(SavingsGoal goal)
    {
        ArgumentNullException.ThrowIfNull(goal);
        if (string.IsNullOrWhiteSpace(goal.Name)) throw new ArgumentException("Savings goal name is required.", nameof(goal));
        if (goal.Name.Trim().Length > 120) throw new ArgumentException("Savings goal name cannot exceed 120 characters.", nameof(goal));
        if (goal.TargetMinor <= 0) throw new ArgumentOutOfRangeException(nameof(goal.TargetMinor), "Savings target must be positive.");
        if (goal.StartingMinor < 0 || goal.StartingMinor > goal.TargetMinor)
            throw new ArgumentOutOfRangeException(nameof(goal.StartingMinor), "Starting amount must be between zero and the target amount.");
        ValidateCurrency(goal.Currency);
        if (string.IsNullOrWhiteSpace(goal.Icon) || goal.Icon.Trim().Length > 80)
            throw new ArgumentException("Savings goal icon identifier must contain 1–80 characters.", nameof(goal));
    }

    public static void ValidateGoalContribution(GoalContribution contribution)
    {
        ArgumentNullException.ThrowIfNull(contribution);
        if (contribution.SavingsGoalId == Guid.Empty) throw new ArgumentException("Savings goal is required.", nameof(contribution));
        if (contribution.AmountMinor is 0 or long.MinValue) throw new ArgumentOutOfRangeException(nameof(contribution.AmountMinor), "Contribution amount is outside the supported range.");
        if (contribution.OccurredAtUtc == default) throw new ArgumentException("Contribution date/time is required.", nameof(contribution));
    }

    public static void ValidateRecurrenceRule(RecurrenceRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (string.IsNullOrWhiteSpace(rule.Name)) throw new ArgumentException("Recurring rule name is required.", nameof(rule));
        if (rule.Name.Trim().Length > 120) throw new ArgumentException("Recurring rule name cannot exceed 120 characters.", nameof(rule));
        if (rule.Interval is < 1 or > 365) throw new ArgumentOutOfRangeException(nameof(rule.Interval), "Recurring interval must be between 1 and 365.");
        if (rule.AmountMinor <= 0) throw new ArgumentOutOfRangeException(nameof(rule.AmountMinor), "Recurring amount must be positive.");
        ValidateCurrency(rule.Currency);
        if (rule.AccountId == Guid.Empty) throw new ArgumentException("Recurring account is required.", nameof(rule));
        if (rule.StartsOn == default) throw new ArgumentException("Recurring start date is required.", nameof(rule));
        if (rule.EndsOn is DateOnly end && end < rule.StartsOn) throw new InvalidOperationException("Recurring end date cannot precede the start date.");
        if (rule.DayOfMonth is < 1 or > 31) throw new ArgumentOutOfRangeException(nameof(rule.DayOfMonth));
        if (rule.GracePeriodDays is < 0 or > 90) throw new ArgumentOutOfRangeException(nameof(rule.GracePeriodDays));
        if (rule.ReminderMinutesBefore is < 0 or > 10_080) throw new ArgumentOutOfRangeException(nameof(rule.ReminderMinutesBefore));
        if (rule.TransactionType is TransactionType.Adjustment)
            throw new InvalidOperationException("Adjustment transactions are not supported as recurring templates.");

        if (rule.TransactionType == TransactionType.Transfer)
        {
            if (rule.DestinationAccountId is null || rule.DestinationAccountId == Guid.Empty)
                throw new InvalidOperationException("Recurring transfers require a destination account.");
            if (rule.DestinationAccountId == rule.AccountId)
                throw new InvalidOperationException("Recurring transfers require different source and destination accounts.");
            if (rule.CategoryId is not null)
                throw new InvalidOperationException("Recurring transfers cannot use a spending category.");
        }
        else if (rule.DestinationAccountId is not null)
        {
            throw new InvalidOperationException("Only recurring transfers may specify a destination account.");
        }
    }

    public static void ValidateRecurrenceOccurrence(RecurrenceOccurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        if (occurrence.RecurrenceRuleId == Guid.Empty) throw new ArgumentException("Recurrence occurrence must reference a rule.", nameof(occurrence));
        if (occurrence.DueOn == default) throw new ArgumentException("Recurrence occurrence due date is required.", nameof(occurrence));
        if (occurrence.PaidAmountMinor is 0 or long.MinValue) throw new ArgumentOutOfRangeException(nameof(occurrence.PaidAmountMinor), "Paid amount is outside the supported range.");
        if (occurrence.PaidAmountMinor is < 0) throw new ArgumentOutOfRangeException(nameof(occurrence.PaidAmountMinor), "Paid amount cannot be negative.");
        if (occurrence.PostponedTo is DateOnly postponed && postponed <= occurrence.DueOn)
            throw new InvalidOperationException("Postponed recurrence date must be after the original due date.");

        switch (occurrence.Status)
        {
            case OccurrenceStatus.Pending or OccurrenceStatus.Skipped:
                if (occurrence.GeneratedTransactionId is not null || occurrence.PaidAmountMinor is not null || occurrence.PostponedTo is not null)
                    throw new InvalidOperationException("Pending or skipped occurrences cannot contain payment or postponement data.");
                break;
            case OccurrenceStatus.Postponed:
                if (occurrence.GeneratedTransactionId is not null || occurrence.PaidAmountMinor is not null || occurrence.PostponedTo is null)
                    throw new InvalidOperationException("Postponed occurrences require only a postponed date.");
                break;
            case OccurrenceStatus.Paid or OccurrenceStatus.PartiallyPaid:
                if (occurrence.GeneratedTransactionId is null || occurrence.PaidAmountMinor is null or <= 0 || occurrence.PostponedTo is not null)
                    throw new InvalidOperationException("Paid occurrences require generated transaction and paid amount data.");
                break;
        }
    }

    public static void ValidateAttachmentMetadata(Attachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        if (attachment.TransactionId == Guid.Empty) throw new ArgumentException("Attachment must reference a transaction.", nameof(attachment));
        if (string.IsNullOrWhiteSpace(attachment.RelativePath) || attachment.RelativePath.Length > 1024)
            throw new ArgumentException("Attachment relative path is invalid.", nameof(attachment));
        var normalized = attachment.RelativePath.Replace('\\', '/');
        if (!normalized.StartsWith("attachments/", StringComparison.Ordinal) || normalized.Contains("../", StringComparison.Ordinal) || normalized.EndsWith("/..", StringComparison.Ordinal))
            throw new InvalidOperationException("Attachment path must remain beneath Finora receipt storage.");
        if (string.IsNullOrWhiteSpace(attachment.OriginalFileName) || attachment.OriginalFileName.Trim().Length > 240)
            throw new ArgumentException("Attachment original file name must contain 1–240 characters.", nameof(attachment));
        if (string.IsNullOrWhiteSpace(attachment.ContentType) || !AllowedAttachmentContentTypes.Contains(attachment.ContentType.Trim()))
            throw new ArgumentException("Attachment content type is unsupported.", nameof(attachment));
        if (attachment.SizeBytes <= 0 || attachment.SizeBytes > 20L * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(attachment.SizeBytes), "Attachment size must be between 1 byte and 20 MB.");
        if (attachment.Sha256 is not { Length: 32 })
            throw new ArgumentException("Attachment SHA-256 metadata must contain 32 bytes.", nameof(attachment));
    }

    public static void ValidateTransactionRevision(TransactionRevision revision)
    {
        ArgumentNullException.ThrowIfNull(revision);
        if (revision.TransactionId == Guid.Empty) throw new ArgumentException("Transaction revision must reference a transaction.", nameof(revision));
        if (string.IsNullOrWhiteSpace(revision.ChangeKind) || revision.ChangeKind.Trim().Length > 80)
            throw new ArgumentException("Transaction revision change kind must contain 1–80 characters.", nameof(revision));
        if (string.IsNullOrWhiteSpace(revision.SnapshotJson) || revision.SnapshotJson.Length > 1_000_000)
            throw new ArgumentException("Transaction revision snapshot is missing or too large.", nameof(revision));
        if (revision.ChangedAtUtc == default) throw new ArgumentException("Transaction revision timestamp is required.", nameof(revision));
    }

    public static void ValidateReconciliation(AccountReconciliation reconciliation)
    {
        ArgumentNullException.ThrowIfNull(reconciliation);
        if (reconciliation.AccountId == Guid.Empty) throw new ArgumentException("Reconciliation must reference an account.", nameof(reconciliation));
        if (reconciliation.StatementDateUtc == default || reconciliation.CompletedAtUtc == default)
            throw new ArgumentException("Reconciliation statement and completion timestamps are required.", nameof(reconciliation));
        if (reconciliation.DifferenceMinor == long.MinValue)
            throw new ArgumentOutOfRangeException(nameof(reconciliation.DifferenceMinor), "Reconciliation difference is outside the supported range.");
        if (checked(reconciliation.StatementBalanceMinor - reconciliation.BookBalanceMinor) != reconciliation.DifferenceMinor)
            throw new InvalidOperationException("Reconciliation difference must equal statement balance minus book balance.");
        if (reconciliation.AdjustmentCreated != (reconciliation.AdjustmentTransactionId is not null))
            throw new InvalidOperationException("Reconciliation adjustment state and adjustment transaction link must agree.");
    }

    public static void ValidateNotificationSchedule(NotificationSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        if (string.IsNullOrWhiteSpace(schedule.Kind) || schedule.Kind.Trim().Length > 64)
            throw new ArgumentException("Notification kind must contain 1–64 characters.", nameof(schedule));
        if (string.IsNullOrWhiteSpace(schedule.Title) || schedule.Title.Trim().Length > 160)
            throw new ArgumentException("Notification title must contain 1–160 characters.", nameof(schedule));
        if (string.IsNullOrWhiteSpace(schedule.Body) || schedule.Body.Trim().Length > 500)
            throw new ArgumentException("Notification body must contain 1–500 characters.", nameof(schedule));
        if (schedule.TriggerAtUtc == default) throw new ArgumentException("Notification trigger time is required.", nameof(schedule));
        if (schedule.DedupeKey?.Trim().Length > 200) throw new ArgumentException("Notification dedupe key cannot exceed 200 characters.", nameof(schedule));
        if (schedule.DeliveredAtUtc is DateTimeOffset delivered && delivered < schedule.CreatedAtUtc)
            throw new InvalidOperationException("Notification delivery timestamp cannot precede creation.");
    }

    public static void ValidateAppSetting(AppSetting setting)
    {
        ArgumentNullException.ThrowIfNull(setting);
        if (string.IsNullOrWhiteSpace(setting.Key) || setting.Key.Trim().Length > 200)
            throw new ArgumentException("Application setting key must contain 1–200 characters.", nameof(setting));
        if (setting.Value is null || setting.Value.Length > 100_000)
            throw new ArgumentException("Application setting value is missing or too large.", nameof(setting));
    }

    public static void ValidateAuditEntry(AuditEntry audit)
    {
        ArgumentNullException.ThrowIfNull(audit);
        if (string.IsNullOrWhiteSpace(audit.EntityType) || audit.EntityType.Trim().Length > 80)
            throw new ArgumentException("Audit entity type must contain 1–80 characters.", nameof(audit));
        if (audit.EntityId == Guid.Empty) throw new ArgumentException("Audit entry entity identifier is required.", nameof(audit));
        if (string.IsNullOrWhiteSpace(audit.Action) || audit.Action.Trim().Length > 200)
            throw new ArgumentException("Audit action must contain 1–200 characters.", nameof(audit));
        if (audit.SanitizedDetailsJson?.Length > 20_000)
            throw new ArgumentException("Audit sanitized details are too large.", nameof(audit));
    }

    public static void ValidateBackupMetadata(BackupMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        if (string.IsNullOrWhiteSpace(metadata.BackupId) || metadata.BackupId.Trim().Length > 100)
            throw new ArgumentException("Backup identifier must contain 1–100 characters.", nameof(metadata));
        if (metadata.SchemaVersion <= 0) throw new ArgumentOutOfRangeException(nameof(metadata.SchemaVersion));
        if (metadata.CreatedOnUtc == default) throw new ArgumentException("Backup metadata creation timestamp is required.", nameof(metadata));
        if (metadata.Sha256Hex is { Length: > 0 } hash && (hash.Length != 64 || hash.Any(character => !Uri.IsHexDigit(character))))
            throw new ArgumentException("Backup SHA-256 metadata must be a 64-character hexadecimal string.", nameof(metadata));
    }

    public static DateOnly GetNextOccurrence(RecurrenceRule rule, DateOnly current)
    {
        ArgumentNullException.ThrowIfNull(rule);
        var interval = Math.Max(1, rule.Interval);
        return rule.Frequency switch
        {
            RecurrenceFrequency.Daily => current.AddDays(interval),
            RecurrenceFrequency.Weekly => current.AddDays(checked(7 * interval)),
            RecurrenceFrequency.Monthly => AddMonths(current, interval, rule.DayOfMonth),
            RecurrenceFrequency.Yearly => AddMonths(current, checked(12 * interval), rule.DayOfMonth),
            _ => current.AddDays(interval)
        };
    }

    public static void ValidateCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));
        var normalized = currency.Trim();
        if (normalized.Length is < 3 or > 8) throw new ArgumentException("Currency must contain 3–8 characters.", nameof(currency));
        if (normalized.Any(character => !char.IsLetterOrDigit(character)))
            throw new ArgumentException("Currency can contain only letters and digits.", nameof(currency));
    }

    private static DateOnly AddMonths(DateOnly current, int months, int? day)
    {
        var basis = current.AddMonths(months);
        var requestedDay = day ?? current.Day;
        if (requestedDay is < 1 or > 31) throw new ArgumentOutOfRangeException(nameof(day), "Day of month must be between 1 and 31.");
        return new DateOnly(basis.Year, basis.Month, Math.Min(requestedDay, DateTime.DaysInMonth(basis.Year, basis.Month)));
    }
}
