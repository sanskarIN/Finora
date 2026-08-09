using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class PersistenceInvariantTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-invariants-{Guid.NewGuid():N}");
    private FinanceStoreTests.TestFactory _factory = null!;
    private Guid _accountId;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        var options = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_root, "finora.db")}")
            .Options;
        _factory = new FinanceStoreTests.TestFactory(options);
        var initializer = new DatabaseInitializer(_factory);
        await initializer.InitializeAsync();

        await using var db = await _factory.CreateDbContextAsync();
        var account = new Account { Name = "Synthetic account", Type = AccountType.Bank, Currency = "inr" };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();
        _accountId = account.Id;
        Assert.Equal("INR", account.Currency);
    }

    public Task DisposeAsync()
    {
        try { Directory.Delete(_root, true); } catch (IOException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task DirectEfWrite_RejectsPositiveExpense()
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Transactions.Add(new FinanceTransaction
        {
            Type = TransactionType.Expense,
            AmountMinor = 100,
            Currency = "INR",
            AccountId = _accountId,
            OccurredAtUtc = DateTimeOffset.UtcNow
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DirectEfWrite_RejectsInvalidCurrency()
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Transactions.Add(new FinanceTransaction
        {
            Type = TransactionType.Adjustment,
            AmountMinor = 100,
            Currency = "INR!",
            AccountId = _accountId,
            OccurredAtUtc = DateTimeOffset.UtcNow
        });

        await Assert.ThrowsAsync<ArgumentException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DirectEfWrite_NormalizesValidCurrency()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var transaction = new FinanceTransaction
        {
            Type = TransactionType.Income,
            AmountMinor = 100,
            Currency = " usd ",
            AccountId = _accountId,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        Assert.Equal("USD", transaction.Currency);
    }
}
