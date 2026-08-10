using Finora.Application;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class AccountLifecycleDependencyTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-account-lifecycle-{Guid.NewGuid():N}");
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
    public async Task ArchiveAction_IsBlockedByActiveRecurringRule()
    {
        var account = await CreateAccountAsync();
        await CreateRuleAsync(account.Id);

        var result = await new AccountManagementService(_factory).ArchiveAsync(account.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(AccountState.Active, (await _store.GetAccountsAsync()).Single(x => x.Id == account.Id).State);
    }

    [Fact]
    public async Task StatePickerArchive_IsBlockedByActiveRecurringRule()
    {
        var account = await CreateAccountAsync();
        await CreateRuleAsync(account.Id);
        var request = new AccountUpdateRequest(
            account.Id,
            account.Name,
            account.Type,
            account.Icon,
            account.ColorLabel,
            account.OpeningBalanceMinor,
            account.CreditLimitMinor,
            account.BillingDay,
            AccountState.Archived);

        var result = await new AccountManagementService(_factory).UpdateAccountAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(AccountState.Active, (await _store.GetAccountsAsync()).Single(x => x.Id == account.Id).State);
    }

    [Fact]
    public async Task PausedRecurringRule_AllowsAccountArchival()
    {
        var account = await CreateAccountAsync();
        var rule = await CreateRuleAsync(account.Id);
        await using (var db = await _factory.CreateDbContextAsync())
        {
            var storedRule = await db.RecurrenceRules.SingleAsync(x => x.Id == rule.Id);
            storedRule.Status = RecurrenceStatus.Paused;
            await db.SaveChangesAsync();
        }

        var result = await new AccountManagementService(_factory).ArchiveAsync(account.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(AccountState.Archived, (await _store.GetAccountsAsync()).Single(x => x.Id == account.Id).State);
    }

    private async Task<Account> CreateAccountAsync()
    {
        var account = new Account { Name = "Bank", Type = AccountType.Bank, Currency = "INR" };
        await _store.SaveAccountAsync(account);
        return account;
    }

    private async Task<RecurrenceRule> CreateRuleAsync(Guid accountId)
    {
        var rule = new RecurrenceRule
        {
            Name = "Rent",
            Frequency = RecurrenceFrequency.Monthly,
            Interval = 1,
            StartsOn = new DateOnly(2026, 8, 1),
            TransactionType = TransactionType.Expense,
            AmountMinor = 1_000,
            Currency = "INR",
            AccountId = accountId
        };
        await _store.SaveRecurrenceRuleAsync(rule);
        return rule;
    }
}
