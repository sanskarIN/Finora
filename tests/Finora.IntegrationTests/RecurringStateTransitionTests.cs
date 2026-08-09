using Finora.Application;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class RecurringStateTransitionTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-recurring-state-{Guid.NewGuid():N}");
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
        _account = new Account { Name = "Synthetic bank", Type = AccountType.Bank, Currency = "INR" };
        await _store.SaveAccountAsync(_account);
        _today = DateOnly.FromDateTime(DateTime.Today);
    }

    public Task DisposeAsync()
    {
        try { Directory.Delete(_root, true); } catch (IOException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task SkippedOccurrence_CanReopenThenPay_WithoutDuplicatePayment()
    {
        var occurrence = await PrepareOccurrenceAsync();
        var workflow = new RecurringWorkflowService(_factory);

        Assert.True((await workflow.SkipAsync(occurrence.Id)).IsSuccess);
        var skipped = Assert.Single(await workflow.GetOccurrencesAsync(_today, _today, true));
        Assert.Equal(OccurrenceStatus.Skipped, skipped.Status);

        Assert.False((await workflow.MarkPaidAsync(occurrence.Id)).IsSuccess);
        Assert.True((await workflow.ReopenAsync(occurrence.Id)).IsSuccess);
        Assert.Equal(OccurrenceStatus.Pending, Assert.Single(await workflow.GetOccurrencesAsync(_today, _today, true)).Status);

        Assert.True((await workflow.MarkPaidAsync(occurrence.Id)).IsSuccess);
        Assert.True((await workflow.MarkPaidAsync(occurrence.Id)).IsSuccess);
        var transactions = await _store.SearchTransactionsAsync();
        Assert.Single(transactions);
        Assert.Equal(-2_500, transactions[0].AmountMinor);
    }

    [Fact]
    public async Task SkippedOccurrence_MustReopenBeforePostpone()
    {
        var occurrence = await PrepareOccurrenceAsync();
        var workflow = new RecurringWorkflowService(_factory);
        Assert.True((await workflow.SkipAsync(occurrence.Id)).IsSuccess);

        var rejected = await workflow.PostponeAsync(occurrence.Id, _today.AddDays(2));
        Assert.False(rejected.IsSuccess);

        Assert.True((await workflow.ReopenAsync(occurrence.Id)).IsSuccess);
        Assert.True((await workflow.PostponeAsync(occurrence.Id, _today.AddDays(2))).IsSuccess);
        var postponed = Assert.Single(await workflow.GetOccurrencesAsync(_today, _today, true));
        Assert.Equal(OccurrenceStatus.Postponed, postponed.Status);
        Assert.Equal(_today.AddDays(2), postponed.PostponedTo);
    }

    [Fact]
    public async Task ArchivedAccount_BlocksRecurringPaymentGeneration()
    {
        var occurrence = await PrepareOccurrenceAsync();
        await _store.ArchiveAccountAsync(_account.Id);
        var workflow = new RecurringWorkflowService(_factory);

        var result = await workflow.MarkPaidAsync(occurrence.Id);

        Assert.False(result.IsSuccess);
        Assert.Empty(await _store.SearchTransactionsAsync());
        Assert.Equal(OccurrenceStatus.Pending, Assert.Single(await workflow.GetOccurrencesAsync(_today, _today, true)).Status);
    }

    private async Task<RecurrenceOccurrenceInfo> PrepareOccurrenceAsync()
    {
        var rule = new RecurrenceRule
        {
            Name = "Synthetic rent",
            Frequency = RecurrenceFrequency.Monthly,
            StartsOn = _today,
            NextDueOn = _today,
            DayOfMonth = _today.Day,
            TransactionType = TransactionType.Expense,
            AmountMinor = 2_500,
            Currency = "INR",
            AccountId = _account.Id
        };
        await _store.SaveRecurrenceRuleAsync(rule);
        Assert.Equal(1, await _store.ProcessDueRecurrencesAsync(_today));
        var workflow = new RecurringWorkflowService(_factory);
        return Assert.Single(await workflow.GetOccurrencesAsync(_today, _today, true));
    }
}
