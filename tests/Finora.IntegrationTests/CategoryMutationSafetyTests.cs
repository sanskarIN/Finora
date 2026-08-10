using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class CategoryMutationSafetyTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-category-safety-{Guid.NewGuid():N}");
    private FinanceStoreTests.TestFactory _factory = null!;
    private FinanceStore _store = null!;
    private CategoryTagService _service = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        var options = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_root, "finora.db")}")
            .Options;
        _factory = new FinanceStoreTests.TestFactory(options);
        _store = new FinanceStore(_factory, new DatabaseInitializer(_factory));
        await _store.InitializeAsync();
        _service = new CategoryTagService(_factory);
    }

    public Task DisposeAsync()
    {
        try { Directory.Delete(_root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ArchiveCategory_RejectsRootReplacementForSubcategoryBudget()
    {
        var root = (await _service.SaveCategoryAsync(null, "Root", "folder", null)).Value!.Value;
        var source = (await _service.SaveCategoryAsync(null, "Source child", "tag", root)).Value!.Value;
        var replacementRoot = (await _service.SaveCategoryAsync(null, "Replacement root", "folder", null)).Value!.Value;
        var budget = new Budget
        {
            Name = "Child budget",
            Kind = BudgetKind.Subcategory,
            Cadence = BudgetCadence.Monthly,
            CategoryId = source,
            LimitMinor = 1_000,
            Currency = "INR"
        };
        await _store.SaveBudgetAsync(budget);

        var result = await _service.ArchiveCategoryAsync(source, replacementRoot);

        Assert.False(result.IsSuccess);
        var categories = await _service.GetCategoriesAsync(true);
        Assert.False(categories.Single(x => x.Id == source).IsArchived);
        await using var db = await _factory.CreateDbContextAsync();
        Assert.Equal(source, (await db.Budgets.AsNoTracking().SingleAsync(x => x.Id == budget.Id)).CategoryId);
    }

    [Fact]
    public async Task MergeCategory_RejectsRootTargetForSubcategoryBudget()
    {
        var root = (await _service.SaveCategoryAsync(null, "Root", "folder", null)).Value!.Value;
        var source = (await _service.SaveCategoryAsync(null, "Source child", "tag", root)).Value!.Value;
        var targetRoot = (await _service.SaveCategoryAsync(null, "Target root", "folder", null)).Value!.Value;
        var budget = new Budget
        {
            Name = "Child budget",
            Kind = BudgetKind.Subcategory,
            Cadence = BudgetCadence.Monthly,
            CategoryId = source,
            LimitMinor = 1_000,
            Currency = "INR"
        };
        await _store.SaveBudgetAsync(budget);

        var result = await _service.MergeCategoryAsync(source, targetRoot);

        Assert.False(result.IsSuccess);
        Assert.False((await _service.GetCategoriesAsync(true)).Single(x => x.Id == source).IsArchived);
    }

    [Fact]
    public async Task TagReport_RejectsInvalidRange()
    {
        var now = DateTimeOffset.UtcNow;
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetTagReportAsync(now, now));
    }

    [Fact]
    public async Task TagReport_RejectsUnsupportedExtremeStoredAmount()
    {
        var account = new Account { Name = "Bank", Type = AccountType.Bank, Currency = "INR" };
        await _store.SaveAccountAsync(account);
        var transaction = new FinanceTransaction
        {
            Type = TransactionType.Expense,
            AmountMinor = -100,
            Currency = "INR",
            AccountId = account.Id,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
        await _store.SaveTransactionAsync(transaction);
        var tagId = (await _service.SaveTagAsync(null, "Test", null)).Value!.Value;
        await using (var db = await _factory.CreateDbContextAsync())
        {
            db.TransactionTags.Add(new TransactionTag { TransactionId = transaction.Id, TagId = tagId });
            await db.SaveChangesAsync();
            await db.Transactions.Where(x => x.Id == transaction.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.AmountMinor, long.MinValue));
        }

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            _service.GetTagReportAsync(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1)));
    }
}
