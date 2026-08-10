using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class RecurringRuleLifecycleTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-rule-lifecycle-{Guid.NewGuid():N}");
    private FinanceStoreTests.TestFactory _factory = null!;
    private FinanceStore _store = null!;
    private Account _account = null!;
    private DateOnly _today;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        var options = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_root, "finora.db")}")
            .Options;
        _factory = new FinanceStoreTests.TestFactory(options);
        _store = new FinanceStore(_factory, new DatabaseInitializer(_factory));
        await _store.InitializeAsync();
        _account = new Account { Name = "Bank", Type = AccountType.Bank, Currency = "INR" };
        await _store.SaveAccountAsync(_account);
        _today = DateOnly.FromDateTime(DateTime.Today);
    }

    public Task DisposeAsync()
    {
        try { Directory.Delete(_root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task PausedRule_DoesNotGenerateUntilResumed()
    {
        var rule = await CreateRuleAsync();
        var workflow = new RecurringWorkflowService(_factory);
        Assert.True((await workflow.PauseRuleAsync(rule.Id)).IsSuccess);

        Assert.Equal(0, await _store.ProcessDueRecurrencesAsync(_today));
        Assert.Empty(await workflow.GetOccurrencesAsync(_today, _today, true));

        Assert.True((await workflow.ResumeRuleAsync(rule.Id)).IsSuccess);
        Assert.Equal(1, await _store.ProcessDueRecurrencesAsync(_today));
        Assert.Single(await workflow.GetOccurrencesAsync(_today, _today, true));
    }

    [Fact]
    public async Task ArchiveRule_HidesRuleButPreservesExistingOccurrenceHistory()
    {
        var rule = await CreateRuleAsync();
        Assert.Equal(1, await _store.ProcessDueRecurrencesAsync(_today));
        var workflow = new RecurringWorkflowService(_factory);
        Assert.Single(await workflow.GetOccurrencesAsync(_today, _today, true));

        Assert.True((await workflow.ArchiveRuleAsync(rule.Id)).IsSuccess);

        Assert.DoesNotContain(await _store.GetRecurrenceRulesAsync(), x => x.Id == rule.Id);
        Assert.Single(await workflow.GetOccurrencesAsync(_today, _today, true));
    }

    [Fact]
    public async Task ResumeRule_FailsWhenSourceAccountWasArchivedWhilePaused()
    {
        var rule = await CreateRuleAsync();
        var workflow = new RecurringWorkflowService(_factory);
        Assert.True((await workflow.PauseRuleAsync(rule.Id)).IsSuccess);
        Assert.True((await new AccountManagementService(_factory).ArchiveAsync(_account.Id)).IsSuccess);

        var result = await workflow.ResumeRuleAsync(rule.Id);

        Assert.False(result.IsSuccess);
        await using var db = await _factory.CreateDbContextAsync();
        Assert.Equal(RecurrenceStatus.Paused, (await db.RecurrenceRules.AsNoTracking().SingleAsync(x => x.Id == rule.Id)).Status);
    }

    [Fact]
    public async Task ResumeRule_FailsAfterConfiguredEndDate()
    {
        var rule = await CreateRuleAsync();
        var workflow = new RecurringWorkflowService(_factory);
        Assert.True((await workflow.PauseRuleAsync(rule.Id)).IsSuccess);
        await using (var db = await _factory.CreateDbContextAsync())
        {
            await db.RecurrenceRules.Where(x => x.Id == rule.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.EndsOn, _today.AddDays(-1)));
        }

        var result = await workflow.ResumeRuleAsync(rule.Id);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CompletedOrArchivedRule_CannotBeResumed()
    {
        var rule = await CreateRuleAsync();
        await using (var db = await _factory.CreateDbContextAsync())
        {
            await db.RecurrenceRules.Where(x => x.Id == rule.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, RecurrenceStatus.Completed));
        }
        var workflow = new RecurringWorkflowService(_factory);

        Assert.False((await workflow.ResumeRuleAsync(rule.Id)).IsSuccess);
        Assert.True((await workflow.ArchiveRuleAsync(rule.Id)).IsSuccess);
        Assert.True((await workflow.ArchiveRuleAsync(rule.Id)).IsSuccess);
        Assert.False((await workflow.ResumeRuleAsync(rule.Id)).IsSuccess);
    }

    private async Task<RecurrenceRule> CreateRuleAsync()
    {
        var rule = new RecurrenceRule
        {
            Name = "Rent",
            Frequency = RecurrenceFrequency.Monthly,
            Interval = 1,
            StartsOn = _today,
            NextDueOn = _today,
            DayOfMonth = _today.Day,
            TransactionType = TransactionType.Expense,
            AmountMinor = 1_000,
            Currency = "INR",
            AccountId = _account.Id
        };
        await _store.SaveRecurrenceRuleAsync(rule);
        return rule;
    }
}
