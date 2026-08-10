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

    [Fact]
    public void TransferRows_RequirePairLinkage()
    {
        var transaction = NewTransaction(TransactionType.Transfer, -1_000);
        Assert.Throws<InvalidOperationException>(() => DomainRules.ValidateTransaction(transaction));

        transaction.TransferGroupId = Guid.NewGuid();
        transaction.CounterpartyAccountId = Guid.NewGuid();
        DomainRules.ValidateTransaction(transaction);
    }

    [Fact]
    public void NonTransferRows_CannotCarryTransferLinkage()
    {
        var transaction = NewTransaction(TransactionType.Expense, -100);
        transaction.TransferGroupId = Guid.NewGuid();
        transaction.CounterpartyAccountId = Guid.NewGuid();
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

    [Fact]
    public void OverallBudget_CannotTargetCategory()
    {
        var budget = NewBudget(BudgetKind.Overall);
        budget.CategoryId = Guid.NewGuid();
        Assert.Throws<InvalidOperationException>(() => DomainRules.ValidateBudget(budget));
    }

    [Theory]
    [InlineData(BudgetKind.Category)]
    [InlineData(BudgetKind.Subcategory)]
    public void CategoryBudget_RequiresCategory(BudgetKind kind)
    {
        var budget = NewBudget(kind);
        Assert.Throws<InvalidOperationException>(() => DomainRules.ValidateBudget(budget));
    }

    [Fact]
    public void SavingsGoal_StartingAmountCannotExceedTarget()
    {
        var goal = new SavingsGoal { Name = "Emergency", TargetMinor = 10_000, StartingMinor = 10_001, Currency = "INR" };
        Assert.Throws<ArgumentOutOfRangeException>(() => DomainRules.ValidateSavingsGoal(goal));
    }

    [Fact]
    public void GoalContribution_RejectsUnsupportedExtreme()
    {
        var contribution = new GoalContribution
        {
            SavingsGoalId = Guid.NewGuid(),
            AmountMinor = long.MinValue,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
        Assert.Throws<ArgumentOutOfRangeException>(() => DomainRules.ValidateGoalContribution(contribution));
    }

    [Fact]
    public void RecurringTransfer_RequiresDifferentDestinationAndNoCategory()
    {
        var accountId = Guid.NewGuid();
        var rule = NewRecurrence(TransactionType.Transfer, accountId);
        rule.DestinationAccountId = accountId;
        Assert.Throws<InvalidOperationException>(() => DomainRules.ValidateRecurrenceRule(rule));

        rule.DestinationAccountId = Guid.NewGuid();
        rule.CategoryId = Guid.NewGuid();
        Assert.Throws<InvalidOperationException>(() => DomainRules.ValidateRecurrenceRule(rule));
    }

    [Fact]
    public void NonTransferRecurrence_CannotSpecifyDestination()
    {
        var rule = NewRecurrence(TransactionType.Expense, Guid.NewGuid());
        rule.DestinationAccountId = Guid.NewGuid();
        Assert.Throws<InvalidOperationException>(() => DomainRules.ValidateRecurrenceRule(rule));
    }

    private static Budget NewBudget(BudgetKind kind) => new()
    {
        Name = "Plan",
        Kind = kind,
        Cadence = BudgetCadence.Monthly,
        LimitMinor = 10_000,
        Currency = "INR"
    };

    private static RecurrenceRule NewRecurrence(TransactionType type, Guid accountId) => new()
    {
        Name = "Rule",
        Frequency = RecurrenceFrequency.Monthly,
        Interval = 1,
        StartsOn = new DateOnly(2026, 8, 1),
        TransactionType = type,
        AmountMinor = 1_000,
        Currency = "INR",
        AccountId = accountId
    };

    private static FinanceTransaction NewTransaction(TransactionType type, long amountMinor) => new()
    {
        Type = type,
        AmountMinor = amountMinor,
        Currency = "INR",
        AccountId = Guid.NewGuid(),
        OccurredAtUtc = DateTimeOffset.UtcNow
    };
}
