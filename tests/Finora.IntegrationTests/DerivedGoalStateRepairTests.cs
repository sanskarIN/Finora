using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class DerivedGoalStateRepairTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-goal-repair-{Guid.NewGuid():N}");

    public DerivedGoalStateRepairTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try { Directory.Delete(_root, true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    [Fact]
    public async Task Initialize_RepairsCompletionFlag_WhenUnderlyingHistoryIsValid()
    {
        var factory = CreateFactory();
        var initializer = new DatabaseInitializer(factory);
        await initializer.InitializeAsync();

        var goal = new SavingsGoal
        {
            Name = "Synthetic complete goal",
            TargetMinor = 1_000,
            StartingMinor = 1_000,
            Currency = "INR",
            IsCompleted = false
        };
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.SavingsGoals.Add(goal);
            await db.SaveChangesAsync();
        }

        await initializer.InitializeAsync();

        await using var verify = await factory.CreateDbContextAsync();
        Assert.True((await verify.SavingsGoals.AsNoTracking().SingleAsync(x => x.Id == goal.Id)).IsCompleted);
    }

    [Fact]
    public async Task Initialize_DoesNotMaskNegativeRunningContributionHistory()
    {
        var factory = CreateFactory();
        var initializer = new DatabaseInitializer(factory);
        await initializer.InitializeAsync();

        var goal = new SavingsGoal
        {
            Name = "Synthetic corrupt goal",
            TargetMinor = 1_000,
            StartingMinor = 100,
            Currency = "INR",
            IsCompleted = false
        };
        var contribution = new GoalContribution
        {
            SavingsGoalId = goal.Id,
            AmountMinor = -200,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.SavingsGoals.Add(goal);
            db.GoalContributions.Add(contribution);
            await db.SaveChangesAsync();
        }

        await initializer.InitializeAsync();

        await using var verify = await factory.CreateDbContextAsync();
        Assert.False((await verify.SavingsGoals.AsNoTracking().SingleAsync(x => x.Id == goal.Id)).IsCompleted);
        var report = await new DataIntegrityService(factory, _root).CheckAsync();
        Assert.Contains(report.Issues, issue => issue.Code == "GOAL_CONTRIBUTION_INVALID");
    }

    private FinanceStoreTests.TestFactory CreateFactory()
    {
        var options = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_root, "finora.db")}")
            .Options;
        return new FinanceStoreTests.TestFactory(options);
    }
}
