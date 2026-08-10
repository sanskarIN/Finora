using Finora.Application;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class FinanceRelationInvariantTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-relations-{Guid.NewGuid():N}");
    private FinanceStoreTests.TestFactory _factory = null!;
    private FinanceStore _store = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        var options = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_root, "finora.db")}")
            .Options;
        _factory = new FinanceStoreTests.TestFactory(options);
        _store = new FinanceStore(_factory, new DatabaseInitializer(_factory));
        await _store.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        try { Directory.Delete(_root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task SaveTransaction_RejectsAccountCurrencyMismatch()
    {
        var account = await CreateAccountAsync("INR");
        var transaction = NewTransaction(account.Id, "USD", TransactionType.Income, 100);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _store.SaveTransactionAsync(transaction));
        Assert.Empty(await _store.SearchTransactionsAsync());
    }

    [Fact]
    public async Task SaveAccount_RejectsCurrencyChangeAfterTransactionsExist()
    {
        var account = await CreateAccountAsync("INR");
        await _store.SaveTransactionAsync(NewTransaction(account.Id, "INR", TransactionType.Income, 100));
        account.Currency = "USD";

        await Assert.ThrowsAsync<InvalidOperationException>(() => _store.SaveAccountAsync(account));
        Assert.Equal("INR", (await _store.GetAccountsAsync()).Single(x => x.Id == account.Id).Currency);
    }

    [Fact]
    public async Task GoalContribution_RejectsLinkedTransactionInAnotherCurrency()
    {
        var inrAccount = await CreateAccountAsync("INR");
        var usdAccount = await CreateAccountAsync("USD");
        var goal = new SavingsGoal { Name = "Emergency", TargetMinor = 10_000, StartingMinor = 0, Currency = "INR" };
        await _store.SaveSavingsGoalAsync(goal);
        var usdTransaction = NewTransaction(usdAccount.Id, "USD", TransactionType.Income, 500);
        await _store.SaveTransactionAsync(usdTransaction);

        var contribution = new GoalContribution
        {
            SavingsGoalId = goal.Id,
            AmountMinor = 500,
            OccurredAtUtc = DateTimeOffset.UtcNow,
            TransactionId = usdTransaction.Id
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _store.AddGoalContributionAsync(contribution));
        Assert.Equal(0, (await _store.GetSavingsGoalsAsync()).Single(x => x.Id == goal.Id).CurrentMinor);
        _ = inrAccount;
    }

    [Fact]
    public async Task RecurringTransfer_RejectsDestinationCurrencyMismatch()
    {
        var source = await CreateAccountAsync("INR");
        var destination = await CreateAccountAsync("USD");
        var rule = new RecurrenceRule
        {
            Name = "Move funds",
            Frequency = RecurrenceFrequency.Monthly,
            Interval = 1,
            StartsOn = new DateOnly(2026, 8, 1),
            TransactionType = TransactionType.Transfer,
            AmountMinor = 1_000,
            Currency = "INR",
            AccountId = source.Id,
            DestinationAccountId = destination.Id
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _store.SaveRecurrenceRuleAsync(rule));
        Assert.Empty(await _store.GetRecurrenceRulesAsync());
    }

    [Fact]
    public async Task LegacyDashboard_FailsClosedForMultipleCurrencies()
    {
        await CreateAccountAsync("INR");
        await CreateAccountAsync("USD");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _store.GetDashboardAsync(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31)));
    }

    [Fact]
    public async Task MaintenanceEdit_RejectsAccountCurrencyMismatch()
    {
        var account = await CreateAccountAsync("INR");
        var transaction = NewTransaction(account.Id, "INR", TransactionType.Expense, -100);
        await _store.SaveTransactionAsync(transaction);
        var service = new TransactionMaintenanceService(_factory);
        var request = new TransactionEditRequest(
            transaction.Id,
            TransactionType.Expense,
            -100,
            "USD",
            account.Id,
            null,
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            null,
            [],
            []);

        var result = await service.UpdateTransactionAsync(request);

        Assert.False(result.IsSuccess);
        var stored = Assert.Single(await _store.SearchTransactionsAsync());
        Assert.Equal("INR", stored.Currency);
    }

    [Fact]
    public async Task MaintenanceTransferEdit_RejectsUnbalancedCorruptPair()
    {
        var source = await CreateAccountAsync("INR");
        var destination = await CreateAccountAsync("INR");
        var group = Guid.NewGuid();
        var outgoing = NewTransfer(source.Id, destination.Id, group, -100);
        var incoming = NewTransfer(destination.Id, source.Id, group, 90);
        await using (var db = await _factory.CreateDbContextAsync())
        {
            db.Transactions.AddRange(outgoing, incoming);
            await db.SaveChangesAsync();
        }

        var result = await new TransactionMaintenanceService(_factory)
            .UpdateTransferAsync(outgoing.Id, 200, DateTimeOffset.UtcNow, null);

        Assert.False(result.IsSuccess);
        await using var verify = await _factory.CreateDbContextAsync();
        var pair = await verify.Transactions.AsNoTracking().Where(x => x.TransferGroupId == group).OrderBy(x => x.AmountMinor).ToListAsync();
        Assert.Equal([-100L, 90L], pair.Select(x => x.AmountMinor).ToArray());
    }

    [Fact]
    public async Task DuplicateReview_DoesNotPairDifferentTransactionTypes()
    {
        var account = await CreateAccountAsync("INR");
        var occurred = DateTimeOffset.UtcNow;
        await _store.SaveTransactionAsync(NewTransaction(account.Id, "INR", TransactionType.Income, 100, occurred, "Same"));
        await _store.SaveTransactionAsync(NewTransaction(account.Id, "INR", TransactionType.Refund, 100, occurred, "Same"));

        var candidates = await new TransactionMaintenanceService(_factory).FindLikelyDuplicatesAsync();

        Assert.Empty(candidates);
    }

    private async Task<Account> CreateAccountAsync(string currency)
    {
        var account = new Account { Name = $"{currency}-{Guid.NewGuid():N}", Type = AccountType.Bank, Currency = currency };
        await _store.SaveAccountAsync(account);
        return account;
    }

    private static FinanceTransaction NewTransaction(
        Guid accountId,
        string currency,
        TransactionType type,
        long amountMinor,
        DateTimeOffset? occurredAt = null,
        string? merchant = null) => new()
        {
            Type = type,
            AmountMinor = amountMinor,
            Currency = currency,
            AccountId = accountId,
            OccurredAtUtc = occurredAt ?? DateTimeOffset.UtcNow,
            Merchant = merchant
        };

    private static FinanceTransaction NewTransfer(Guid accountId, Guid counterpartyId, Guid group, long amountMinor) => new()
    {
        Type = TransactionType.Transfer,
        AmountMinor = amountMinor,
        Currency = "INR",
        AccountId = accountId,
        CounterpartyAccountId = counterpartyId,
        TransferGroupId = group,
        OccurredAtUtc = DateTimeOffset.UtcNow
    };
}
