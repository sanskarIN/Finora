using Finora.Application;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class FinanceStoreTests : IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"finora-test-{Guid.NewGuid():N}.db");
    private FinanceStore _store = null!;
    private TestFactory _factory = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<FinoraDbContext>().UseSqlite($"Data Source={_dbPath}").Options;
        _factory = new TestFactory(options);
        _store = new FinanceStore(_factory, new DatabaseInitializer(_factory));
        await _store.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        try { File.Delete(_dbPath); File.Delete(_dbPath + "-wal"); File.Delete(_dbPath + "-shm"); } catch (IOException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Transfer_IsAtomicDoubleEntryAndNetZero()
    {
        var source = new Account { Name = "Bank", Type = AccountType.Bank, Currency = "INR", OpeningBalanceMinor = 10_000 };
        var destination = new Account { Name = "Wallet", Type = AccountType.DigitalWallet, Currency = "INR", OpeningBalanceMinor = 0 };
        await _store.SaveAccountAsync(source);
        await _store.SaveAccountAsync(destination);
        await _store.RecordTransferAsync(source.Id, destination.Id, 2_500, DateTimeOffset.UtcNow, "test");

        var accounts = await _store.GetAccountsAsync();
        Assert.Equal(7_500, accounts.Single(x => x.Id == source.Id).BalanceMinor);
        Assert.Equal(2_500, accounts.Single(x => x.Id == destination.Id).BalanceMinor);
        Assert.Equal(10_000, accounts.Sum(x => x.BalanceMinor));
    }

    [Fact]
    public async Task RecurrenceProcessing_IsIdempotentAndDoesNotCreateTransactionUntilPaid()
    {
        var account = new Account { Name = "Bank", Type = AccountType.Bank, Currency = "INR" };
        await _store.SaveAccountAsync(account);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var rule = new RecurrenceRule
        {
            Name = "Rent",
            Frequency = RecurrenceFrequency.Monthly,
            StartsOn = today,
            NextDueOn = today,
            DayOfMonth = today.Day,
            TransactionType = TransactionType.Expense,
            AmountMinor = 5_000,
            Currency = "INR",
            AccountId = account.Id
        };
        await _store.SaveRecurrenceRuleAsync(rule);
        var first = await _store.ProcessDueRecurrencesAsync(today);
        var second = await _store.ProcessDueRecurrencesAsync(today);
        Assert.Equal(1, first);
        Assert.Equal(0, second);
        Assert.Empty(await _store.SearchTransactionsAsync());

        var workflow = new RecurringWorkflowService(_factory);
        var occurrence = Assert.Single(await workflow.GetOccurrencesAsync(today, today, false));
        var result = await workflow.MarkPaidAsync(occurrence.Id);
        Assert.True(result.IsSuccess);
        var transactions = await _store.SearchTransactionsAsync();
        Assert.Single(transactions);
        Assert.Equal(-5_000, transactions[0].AmountMinor);
    }

    internal sealed class TestFactory(DbContextOptions<FinoraDbContext> options) : IDbContextFactory<FinoraDbContext>
    {
        public FinoraDbContext CreateDbContext() => new(options);
        public Task<FinoraDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
}
