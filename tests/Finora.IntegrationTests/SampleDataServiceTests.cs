using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class SampleDataServiceTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-sample-{Guid.NewGuid():N}");
    private FinanceStoreTests.TestFactory _factory = null!;
    private FinanceStore _store = null!;
    private DatabaseInitializer _initializer = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        var options = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_root, "finora.db")}")
            .Options;
        _factory = new FinanceStoreTests.TestFactory(options);
        _initializer = new DatabaseInitializer(_factory);
        _store = new FinanceStore(_factory, _initializer);
        await _store.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        try { Directory.Delete(_root, true); } catch (IOException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Reset_ReplacesExistingFinanceDataWithSyntheticDataset()
    {
        var userAccount = new Account { Name = "User data", Type = AccountType.Cash, Currency = "INR" };
        await _store.SaveAccountAsync(userAccount);

        var reset = new FinanceDataResetService(_factory);
        var samples = new SampleDataService(reset, _store, _initializer);
        var result = await samples.ResetToSyntheticSampleDataAsync("INR");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.AccountsCreated);
        Assert.Equal(6, result.Value.TransactionsCreated);

        var accounts = await _store.GetAccountsAsync();
        Assert.Equal(2, accounts.Count);
        Assert.DoesNotContain(accounts, account => account.Name == "User data");
        Assert.Contains(accounts, account => account.Name == "Sample bank");
        Assert.Contains(accounts, account => account.Name == "Sample wallet");

        var transactions = await _store.SearchTransactionsAsync();
        Assert.Equal(6, transactions.Count);
        Assert.Equal(2, transactions.Count(transaction => transaction.Type == TransactionType.Transfer));
        Assert.Contains(transactions, transaction => transaction.Merchant == "Sample employer");
        Assert.Contains(transactions, transaction => transaction.Merchant == "Sample grocery");

        var categories = await _store.GetCategoriesAsync();
        Assert.Contains(categories, category => category.IsSystem && category.Name == "Food");
        Assert.Contains(categories, category => category.IsSystem && category.Name == "Housing");

        Assert.Single(await _store.GetBudgetsAsync(DateOnly.FromDateTime(DateTime.Today)));
        Assert.Single(await _store.GetSavingsGoalsAsync());
        Assert.Single(await _store.GetRecurrenceRulesAsync());
    }

    [Fact]
    public async Task Reset_NormalizesCurrencyAndPreservesTransferConservation()
    {
        var reset = new FinanceDataResetService(_factory);
        var samples = new SampleDataService(reset, _store, _initializer);
        var result = await samples.ResetToSyntheticSampleDataAsync(" inr ");
        Assert.True(result.IsSuccess);

        var accounts = await _store.GetAccountsAsync();
        Assert.All(accounts, account => Assert.Equal("INR", account.Currency));

        var total = accounts.Sum(account => account.BalanceMinor);
        var nonTransferNet = (await _store.SearchTransactionsAsync())
            .Where(transaction => transaction.Type != TransactionType.Transfer)
            .Sum(transaction => transaction.AmountMinor);
        var expected = 5_500_000L + nonTransferNet;
        Assert.Equal(expected, total);
    }
}
