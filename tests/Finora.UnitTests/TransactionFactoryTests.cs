using Finora.Application;
using Finora.Domain;

namespace Finora.UnitTests;

public sealed class TransactionFactoryTests
{
    [Theory]
    [InlineData(TransactionType.Expense, -2500)]
    [InlineData(TransactionType.Income, 2500)]
    [InlineData(TransactionType.Refund, 2500)]
    [InlineData(TransactionType.Adjustment, 2500)]
    public void Create_AssignsExpectedSign(TransactionType type, long expected)
    {
        var tx = TransactionFactory.Create(type, 2500, "INR", Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Equal(expected, tx.AmountMinor);
    }

    [Fact]
    public void Create_RejectsTransferBecauseBalancedPairRequiresTransferWorkflow()
        => Assert.Throws<NotSupportedException>(() =>
            TransactionFactory.Create(TransactionType.Transfer, 2500, "INR", Guid.NewGuid(), DateTimeOffset.UtcNow));

    [Fact]
    public void Create_RejectsMissingAccountInsteadOfReturningInvalidEntity()
        => Assert.Throws<ArgumentException>(() =>
            TransactionFactory.Create(TransactionType.Expense, 2500, "INR", Guid.Empty, DateTimeOffset.UtcNow));

    [Fact]
    public void Create_RejectsMissingTimestampInsteadOfReturningInvalidEntity()
        => Assert.Throws<ArgumentException>(() =>
            TransactionFactory.Create(TransactionType.Income, 2500, "INR", Guid.NewGuid(), default));
}
