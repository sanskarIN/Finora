namespace Finora.Domain;

public static class DomainRules
{
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

        if (transaction.Splits.Count == 0) return;
        if (transaction.Type == TransactionType.Transfer)
            throw new InvalidOperationException("Transfer rows cannot contain category splits.");

        long splitTotal = 0;
        foreach (var split in transaction.Splits)
        {
            if (split.AmountMinor == 0) throw new InvalidOperationException("Split amounts cannot be zero.");
            if (split.AmountMinor == long.MinValue) throw new InvalidOperationException("A split amount is outside the supported range.");
            if (Math.Sign(split.AmountMinor) != Math.Sign(transaction.AmountMinor))
                throw new InvalidOperationException("Split amounts must use the same sign as the parent transaction.");
            splitTotal = checked(splitTotal + split.AmountMinor);
        }

        if (splitTotal != transaction.AmountMinor)
            throw new InvalidOperationException("Split amounts must equal the transaction amount.");
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
