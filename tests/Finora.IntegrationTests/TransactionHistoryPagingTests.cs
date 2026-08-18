using Finora.Application;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class TransactionHistoryPagingTests : IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"finora-history-{Guid.NewGuid():N}.db");
    private FinanceStore _finance = null!;
    private TransactionHistoryStore _history = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<FinoraDbContext>().UseSqlite($"Data Source={_dbPath}").Options;
        var factory = new TestFactory(options);
        await new DatabaseInitializer(factory).InitializeAsync();
        _finance = new FinanceStore(factory, new DatabaseInitializer(factory));
        _history = new TransactionHistoryStore(factory);
    }

    public Task DisposeAsync()
    {
        try
        {
            File.Delete(_dbPath);
            File.Delete(_dbPath + "-wal");
            File.Delete(_dbPath + "-shm");
        }
        catch (IOException)
        {
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task PagesLargeHistoryWithoutDuplicatesOrMissingRows()
    {
        var account = new Account { Name = "Primary", Type = AccountType.Bank, Currency = "INR" };
        await _finance.SaveAccountAsync(account);
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        for (var index = 0; index < 120; index++)
        {
            var transaction = TransactionFactory.Create(
                TransactionType.Expense,
                index + 1,
                "INR",
                account.Id,
                start.AddMinutes(index),
                merchant: $"Merchant {index:D3}");
            await _finance.SaveTransactionAsync(transaction);
        }

        var first = await _history.GetPageAsync(new TransactionHistoryQuery(PageSize: 50));
        var second = await _history.GetPageAsync(new TransactionHistoryQuery(Offset: 50, PageSize: 50));
        var third = await _history.GetPageAsync(new TransactionHistoryQuery(Offset: 100, PageSize: 50));

        Assert.Equal(120, first.TotalCount);
        Assert.Equal(50, first.Items.Count);
        Assert.True(first.HasMore);
        Assert.Equal(start.AddMinutes(119), first.Items[0].OccurredAtUtc);
        Assert.Equal(start.AddMinutes(70), first.Items[^1].OccurredAtUtc);

        Assert.Equal(120, second.TotalCount);
        Assert.Equal(50, second.Items.Count);
        Assert.True(second.HasMore);
        Assert.Equal(start.AddMinutes(69), second.Items[0].OccurredAtUtc);
        Assert.Equal(start.AddMinutes(20), second.Items[^1].OccurredAtUtc);

        Assert.Equal(120, third.TotalCount);
        Assert.Equal(20, third.Items.Count);
        Assert.False(third.HasMore);
        Assert.Equal(start.AddMinutes(19), third.Items[0].OccurredAtUtc);
        Assert.Equal(start, third.Items[^1].OccurredAtUtc);

        var ids = first.Items.Concat(second.Items).Concat(third.Items).Select(item => item.Id).ToArray();
        Assert.Equal(120, ids.Length);
        Assert.Equal(120, ids.Distinct().Count());
    }

    [Fact]
    public async Task AppliesAllFiltersBeforeCountingAndPaging()
    {
        var primary = new Account { Name = "Primary", Type = AccountType.Bank, Currency = "INR" };
        var secondary = new Account { Name = "Secondary", Type = AccountType.Bank, Currency = "INR" };
        await _finance.SaveAccountAsync(primary);
        await _finance.SaveAccountAsync(secondary);
        var food = new Category { Name = "Food" };
        await _finance.SaveCategoryAsync(food);

        var start = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        await SaveAsync(TransactionType.Expense, primary.Id, start.AddDays(1), 100, "Old Cafe", food.Id);
        await SaveAsync(TransactionType.Expense, primary.Id, start.AddDays(10), 200, "Corner Cafe", food.Id);
        await SaveAsync(TransactionType.Income, primary.Id, start.AddDays(10), 300, "Cafe income", food.Id);
        await SaveAsync(TransactionType.Expense, secondary.Id, start.AddDays(10), 400, "Cafe secondary", food.Id);
        await SaveAsync(TransactionType.Expense, primary.Id, start.AddDays(10), 500, "Cafe uncategorized");

        var result = await _history.GetPageAsync(new TransactionHistoryQuery(
            SearchText: "Cafe",
            AccountId: primary.Id,
            CategoryId: food.Id,
            Type: TransactionType.Expense,
            FromUtc: start.AddDays(5),
            ToExclusiveUtc: start.AddDays(20),
            PageSize: 10));

        var match = Assert.Single(result.Items);
        Assert.Equal("Corner Cafe", match.Merchant);
        Assert.Equal(1, result.TotalCount);
        Assert.False(result.HasMore);
    }

    [Fact]
    public async Task AppliesSupportedSortOrdersDeterministically()
    {
        var account = new Account { Name = "Primary", Type = AccountType.Bank, Currency = "INR" };
        await _finance.SaveAccountAsync(account);
        var start = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

        await SaveAsync(TransactionType.Income, account.Id, start.AddDays(1), 200, "zeta");
        await SaveAsync(TransactionType.Income, account.Id, start.AddDays(2), 500, "Alpha");
        await SaveAsync(TransactionType.Income, account.Id, start.AddDays(3), 100, "beta");

        var oldest = await _history.GetPageAsync(new TransactionHistoryQuery(Sort: TransactionHistorySort.OldestFirst, PageSize: 10));
        var high = await _history.GetPageAsync(new TransactionHistoryQuery(Sort: TransactionHistorySort.AmountHighToLow, PageSize: 10));
        var low = await _history.GetPageAsync(new TransactionHistoryQuery(Sort: TransactionHistorySort.AmountLowToHigh, PageSize: 10));
        var merchant = await _history.GetPageAsync(new TransactionHistoryQuery(Sort: TransactionHistorySort.MerchantAscending, PageSize: 10));

        Assert.Equal(start.AddDays(1), oldest.Items[0].OccurredAtUtc);
        Assert.Equal(500, high.Items[0].AmountMinor);
        Assert.Equal(100, low.Items[0].AmountMinor);
        Assert.Collection(
            merchant.Items,
            item => Assert.Equal("Alpha", item.Merchant),
            item => Assert.Equal("beta", item.Merchant),
            item => Assert.Equal("zeta", item.Merchant));
    }

    [Fact]
    public async Task RejectsInvalidPagingAndDateRanges()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _history.GetPageAsync(new TransactionHistoryQuery(Offset: -1)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _history.GetPageAsync(new TransactionHistoryQuery(PageSize: 0)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _history.GetPageAsync(new TransactionHistoryQuery(PageSize: TransactionHistoryStore.MaximumPageSize + 1)));

        var instant = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);
        await Assert.ThrowsAsync<ArgumentException>(() => _history.GetPageAsync(new TransactionHistoryQuery(FromUtc: instant, ToExclusiveUtc: instant)));
    }

    [Fact]
    public async Task ExcludesSoftDeletedRowsFromTotalsAndPages()
    {
        var account = new Account { Name = "Primary", Type = AccountType.Bank, Currency = "INR" };
        await _finance.SaveAccountAsync(account);
        var occurredAt = new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
        var deleted = TransactionFactory.Create(TransactionType.Expense, 100, "INR", account.Id, occurredAt, merchant: "Deleted merchant");
        var visible = TransactionFactory.Create(TransactionType.Expense, 200, "INR", account.Id, occurredAt.AddMinutes(1), merchant: "Visible merchant");
        await _finance.SaveTransactionAsync(deleted);
        await _finance.SaveTransactionAsync(visible);
        await _finance.SoftDeleteTransactionAsync(deleted.Id);

        var page = await _history.GetPageAsync(new TransactionHistoryQuery(PageSize: 10));

        var item = Assert.Single(page.Items);
        Assert.Equal(visible.Id, item.Id);
        Assert.Equal(1, page.TotalCount);
        Assert.False(page.HasMore);
    }

    [Fact]
    public async Task SearchesPaymentLocationAccountAndCategoryBeforePaging()
    {
        var primary = new Account { Name = "Primary Ledger", Type = AccountType.Bank, Currency = "INR" };
        var secondary = new Account { Name = "Secondary Ledger", Type = AccountType.Bank, Currency = "INR" };
        await _finance.SaveAccountAsync(primary);
        await _finance.SaveAccountAsync(secondary);
        var dining = new Category { Name = "Dining Out" };
        await _finance.SaveCategoryAsync(dining);
        var occurredAt = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

        var target = TransactionFactory.Create(TransactionType.Expense, 750, "INR", primary.Id, occurredAt, dining.Id, "Target merchant");
        target.PaymentMethod = "UPI Special";
        target.ManualLocation = "Lucknow Center";
        await _finance.SaveTransactionAsync(target);
        await SaveAsync(TransactionType.Expense, secondary.Id, occurredAt.AddMinutes(1), 500, "Decoy merchant");

        foreach (var searchText in new[] { "UPI Special", "Lucknow Center", "Primary Ledger", "Dining Out" })
        {
            var page = await _history.GetPageAsync(new TransactionHistoryQuery(SearchText: searchText, PageSize: 10));
            var item = Assert.Single(page.Items);
            Assert.Equal(target.Id, item.Id);
            Assert.Equal(1, page.TotalCount);
            Assert.False(page.HasMore);
        }
    }

    private async Task SaveAsync(TransactionType type, Guid accountId, DateTimeOffset occurredAtUtc, long amountMinor, string merchant, Guid? categoryId = null)
    {
        var transaction = TransactionFactory.Create(type, amountMinor, "INR", accountId, occurredAtUtc, categoryId, merchant);
        await _finance.SaveTransactionAsync(transaction);
    }

    private sealed class TestFactory(DbContextOptions<FinoraDbContext> options) : IDbContextFactory<FinoraDbContext>
    {
        public FinoraDbContext CreateDbContext() => new(options);
        public Task<FinoraDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
}