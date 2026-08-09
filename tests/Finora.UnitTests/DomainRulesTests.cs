using Finora.Domain;

namespace Finora.UnitTests;

public sealed class DomainRulesTests
{
    [Fact]
    public void MonthlyRecurrence_ClampsToEndOfMonth()
    {
        var rule = new RecurrenceRule
        {
            Name = "Month end",
            Frequency = RecurrenceFrequency.Monthly,
            DayOfMonth = 31,
            StartsOn = new DateOnly(2026, 1, 31),
            TransactionType = TransactionType.Expense,
            AmountMinor = 100,
            AccountId = Guid.NewGuid()
        };

        Assert.Equal(new DateOnly(2026, 2, 28), DomainRules.GetNextOccurrence(rule, rule.StartsOn));
    }

    [Fact]
    public void TransactionSplits_MustEqualTransactionAmount()
    {
        var transaction = NewTransaction(TransactionType.Expense, -1_000);
        transaction.Splits = [new TransactionSplit { AmountMinor = -900 }];

        Assert.Throws<InvalidOperationException>(() => DomainRules.ValidateTransaction(transaction));
    }

    [Theory]
    [InlineData(TransactionType.Expense, 100)]
    [InlineData(TransactionType.Income, -100)]
    [InlineData(TransactionType.Refund, -100)]
    public void TransactionSign_MustMatchSemanticType(TransactionType type, long amountMinor)
    {
        var transaction = NewTransaction(type, amountMinor);
        Assert.Throws<InvalidOperationException>(() => DomainRules.ValidateTransaction(transaction));
    }

    [Fact]
    public void Transaction_RejectsLongMinValue()
    {
        var transaction = NewTransaction(TransactionType.Adjustment, long.MinValue);
        Assert.Throws<ArgumentOutOfRangeException>(() => DomainRules.ValidateTransaction(transaction));
    }

    [Fact]
    public void Splits_MustUseParentSign()
    {
        var transaction = NewTransaction(TransactionType.Expense, -1_000);
        transaction.Splits =
        [
            new TransactionSplit { AmountMinor = -1_100 },
            new TransactionSplit { AmountMinor = 100 }
        ];

        Assert.Throws<InvalidOperationException>(() => DomainRules.ValidateTransaction(transaction));
    }

    [Theory]
    [InlineData("")]
    [InlineData("IN")]
    [InlineData("INR!")]
    [InlineData("123456789")]
    public void Currency_RejectsInvalidCodes(string currency)
    {
        Assert.Throws<ArgumentException>(() => DomainRules.ValidateCurrency(currency));
    }

    [Theory]
    [InlineData("INR")]
    [InlineData("USD")]
    [InlineData("USDT")]
    [InlineData("X123")]
    public void Currency_AcceptsSupportedCodes(string currency)
    {
        DomainRules.ValidateCurrency(currency);
    }

    [Fact]
    public void Account_RejectsNegativeCreditLimit()
    {
        var account = new Account
        {
            Name = "Card",
            Type = AccountType.CreditCard,
            Currency = "INR",
            CreditLimitMinor = -1
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => DomainRules.ValidateAccount(account));
    }

    private static FinanceTransaction NewTransaction(TransactionType type, long amountMinor) => new()
    {
        Type = type,
        AmountMinor = amountMinor,
        Currency = "INR",
        AccountId = Guid.NewGuid(),
        OccurredAtUtc = DateTimeOffset.UtcNow
    };
}
