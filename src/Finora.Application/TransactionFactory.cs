using Finora.Domain;

namespace Finora.Application;

public static class TransactionFactory
{
    public static FinanceTransaction Create(
        TransactionType type,
        long positiveAmountMinor,
        string currency,
        Guid accountId,
        DateTimeOffset occurredAtUtc,
        Guid? categoryId = null,
        string? merchant = null,
        string? note = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(positiveAmountMinor);
        if (type == TransactionType.Transfer)
            throw new NotSupportedException("Use the transfer workflow to create the required balanced pair of transfer rows.");

        var signed = type == TransactionType.Expense ? checked(-positiveAmountMinor) : positiveAmountMinor;
        var transaction = new FinanceTransaction
        {
            Type = type,
            AmountMinor = signed,
            Currency = currency,
            AccountId = accountId,
            OccurredAtUtc = occurredAtUtc,
            CategoryId = categoryId,
            Merchant = merchant,
            Note = note
        };
        DomainRules.ValidateTransaction(transaction);
        return transaction;
    }
}
