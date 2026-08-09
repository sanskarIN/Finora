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
        if (transaction.OccurredAtUtc == default) throw new ArgumentException("Transaction date/time is required.", nameof(transaction));

        switch (transaction.Type)
        {
            case TransactionType.Expense when transaction.AmountMinor >= 0:
                throw new InvalidOperationException("Expense amounts must be negative minor units.");
            case TransactionType.Income or TransactionType.Refund when transaction.AmountMinor <= 0:
                throw new InvalidOperationException("Income and refund amounts must be positive minor units.");
        }

        if (transaction.Splits.Count == 0) return;

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
