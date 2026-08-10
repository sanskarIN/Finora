using Finora.Application;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class AccountReconciliationSafetyTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-reconcile-safety-{Guid.NewGuid():N}");
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
    public async Task ReconciliationPreview_ReturnsFailureInsteadOfOverflowing()
    {
        var account = new Account
        {
            Name = "Extreme",
            Type = AccountType.Bank,
            Currency = "INR",
            OpeningBalanceMinor = long.MaxValue
        };
        await _store.SaveAccountAsync(account);

        var result = await new ReconciliationService(_factory)
            .PreviewAsync(account.Id, long.MinValue, DateTimeOffset.UtcNow);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ReconciliationComplete_ReturnsFailureWithoutWritingExtremeAdjustment()
    {
        var account = new Account
        {
            Name = "Extreme",
            Type = AccountType.Bank,
            Currency = "INR",
            OpeningBalanceMinor = long.MaxValue
        };
        await _store.SaveAccountAsync(account);

        var result = await new ReconciliationService(_factory)
            .CompleteAsync(account.Id, long.MinValue, DateTimeOffset.UtcNow, true, null);

        Assert.False(result.IsSuccess);
        Assert.Empty(await _store.SearchTransactionsAsync());
        Assert.Empty(await new ReconciliationService(_factory).GetHistoryAsync(account.Id));
    }

    [Fact]
    public async Task AccountUpdate_RejectsOpeningBalanceChangeAfterReconciliation()
    {
        var account = new Account
        {
            Name = "Bank",
            Type = AccountType.Bank,
            Currency = "INR",
            OpeningBalanceMinor = 10_000
        };
        await _store.SaveAccountAsync(account);
        var reconciliation = await new ReconciliationService(_factory)
            .CompleteAsync(account.Id, 10_000, DateTimeOffset.UtcNow, false, null);
        Assert.True(reconciliation.IsSuccess);

        var update = new AccountUpdateRequest(
            account.Id,
            "Bank",
            AccountType.Bank,
            "wallet",
            null,
            20_000,
            null,
            null,
            AccountState.Active);
        var result = await new AccountManagementService(_factory).UpdateAccountAsync(update);

        Assert.False(result.IsSuccess);
        var detail = await new AccountManagementService(_factory).GetAccountAsync(account.Id);
        Assert.True(detail.IsSuccess);
        Assert.Equal(10_000, detail.Value!.OpeningBalanceMinor);
    }

    [Fact]
    public async Task AccountUpdate_UsesDomainBillingDayRange()
    {
        var account = new Account { Name = "Card", Type = AccountType.CreditCard, Currency = "INR" };
        await _store.SaveAccountAsync(account);
        var request = new AccountUpdateRequest(
            account.Id,
            "Card",
            AccountType.CreditCard,
            "card",
            null,
            0,
            50_000,
            31,
            AccountState.Active);

        var result = await new AccountManagementService(_factory).UpdateAccountAsync(request);

        Assert.True(result.IsSuccess);
        var detail = await new AccountManagementService(_factory).GetAccountAsync(account.Id);
        Assert.Equal(31, detail.Value!.BillingDay);
    }
}
