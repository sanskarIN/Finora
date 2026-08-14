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
        try { Directory.Delete(_root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
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
        await using (var db = await _factory.CreateDbContextAsync())
        {
            await db.Accounts.Where(x => x.Id == _account.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.State, AccountState.Archived));
        }
        var workflow = new RecurringWorkflowService(_factory);

        var result = await workflow.MarkPaidAsync(occurrence.Id);

        Assert.False(result.IsSuccess);
        Assert.Empty(await _store.SearchTransactionsAsync());
        Assert.Equal(OccurrenceStatus.Pending, Assert.Single(await workflow.GetOccurrencesAsync(_today, _today, true)).Status);
    }

    [Fact]
    public async Task ArchivedCategory_BlocksNewRecurringPayment()
    {
        var category = new Category { Name = "Rent", Icon = "home" };
        await _store.SaveCategoryAsync(category);
        var occurrence = await PrepareOccurrenceAsync(category.Id);
        await using (var db = await _factory.CreateDbContextAsync())
        {
            var storedCategory = await db.Categories.SingleAsync(x => x.Id == category.Id);
            storedCategory.IsArchived = true;
            await db.SaveChangesAsync();
        }

        var result = await new RecurringWorkflowService(_factory).MarkPaidAsync(occurrence.Id);

        Assert.False(result.IsSuccess);
        Assert.Empty(await _store.SearchTransactionsAsync());
    }

    [Fact]
    public async Task PartialPayment_LinkDriftBlocksFurtherMutation()
    {
        var occurrence = await PrepareOccurrenceAsync();
        var workflow = new RecurringWorkflowService(_factory);
        Assert.True((await workflow.MarkPaidAsync(occurrence.Id, 1_000)).IsSuccess);
        var generated = Assert.Single(await _store.SearchTransactionsAsync());
        Assert.Equal(-1_000, generated.AmountMinor);

        await using (var db = await _factory.CreateDbContextAsync())
        {
            var stored = await db.Transactions.SingleAsync(x => x.Id == generated.Id);
            stored.RecurrenceRuleId = null;
            await db.SaveChangesAsync();
        }

        var result = await workflow.MarkPaidAsync(occurrence.Id, 2_500);

        Assert.False(result.IsSuccess);
        Assert.Equal(-1_000, Assert.Single(await _store.SearchTransactionsAsync()).AmountMinor);
    }

    [Fact]
    public async Task PartialRecurringTransfer_PairDriftBlocksFurtherMutation()
    {
        var destination = new Account { Name = "Destination", Type = AccountType.Savings, Currency = "INR" };
        await _store.SaveAccountAsync(destination);
        var rule = new RecurrenceRule
        {
            Name = "Move savings",
            Frequency = RecurrenceFrequency.Monthly,
            StartsOn = _today,
            NextDueOn = _today,
            DayOfMonth = _today.Day,
            TransactionType = TransactionType.Transfer,
            AmountMinor = 2_500,
            Currency = "INR",
            AccountId = _account.Id,
            DestinationAccountId = destination.Id
        };
        await _store.SaveRecurrenceRuleAsync(rule);
        Assert.Equal(1, await _store.ProcessDueRecurrencesAsync(_today));
        var workflow = new RecurringWorkflowService(_factory);
        var occurrence = Assert.Single(await workflow.GetOccurrencesAsync(_today, _today, true));
        Assert.True((await workflow.MarkPaidAsync(occurrence.Id, 1_000)).IsSuccess);

        await using (var db = await _factory.CreateDbContextAsync())
        {
            var pair = await db.Transactions.Where(x => x.TransferGroupId != null).ToListAsync();
            Assert.Equal(2, pair.Count);
            pair.Single(x => x.AccountId == destination.Id).CounterpartyAccountId = Guid.NewGuid();
            await db.SaveChangesAsync();
        }

        var result = await workflow.MarkPaidAsync(occurrence.Id, 2_500);

        Assert.False(result.IsSuccess);
        await using var verify = await _factory.CreateDbContextAsync();
        var amounts = await verify.Transactions.AsNoTracking().Where(x => x.TransferGroupId != null).Select(x => x.AmountMinor).OrderBy(x => x).ToListAsync();
        Assert.Equal(new[] { -1_000L, 1_000L }, amounts);
    }

    private async Task<RecurrenceOccurrenceInfo> PrepareOccurrenceAsync(Guid? categoryId = null)
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
            AccountId = _account.Id,
            CategoryId = categoryId
        };
        await _store.SaveRecurrenceRuleAsync(rule);
        Assert.Equal(1, await _store.ProcessDueRecurrencesAsync(_today));
        var workflow = new RecurringWorkflowService(_factory);
        return Assert.Single(await workflow.GetOccurrencesAsync(_today, _today, true));
    }
}
