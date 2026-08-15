using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class IntegrityCategoryCycleRegressionTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-integrity-category-cycle-{Guid.NewGuid():N}");
    private FinanceStoreTests.TestFactory _factory = null!;
    private CategoryTagService _categories = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        var options = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_root, "finora.db")}")
            .Options;
        _factory = new FinanceStoreTests.TestFactory(options);
        await new DatabaseInitializer(_factory).InitializeAsync();
        _categories = new CategoryTagService(_factory);
    }

    public Task DisposeAsync()
    {
        try { Directory.Delete(_root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task IntegrityCheck_DetectsCategoryParentCycleAfterStorageCorruption()
    {
        var first = (await _categories.SaveCategoryAsync(null, "Cycle A", "tag", null)).Value!;
        var second = (await _categories.SaveCategoryAsync(null, "Cycle B", "tag", first)).Value!;

        await using (var db = await _factory.CreateDbContextAsync())
        {
            await db.Categories.Where(category => category.Id == first)
                .ExecuteUpdateAsync(setters => setters.SetProperty(category => category.ParentId, second));
        }

        var report = await new DataIntegrityService(_factory, _root).CheckAsync();

        Assert.False(report.IsHealthy);
        Assert.Contains(report.Issues, issue =>
            issue.Code == "CATEGORY_CYCLE" &&
            issue.Severity == Finora.Application.IntegritySeverity.Error &&
            issue.AffectedRecords >= 2);
    }
}
