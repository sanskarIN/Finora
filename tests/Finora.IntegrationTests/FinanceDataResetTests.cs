using System.Text;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class FinanceDataResetTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-reset-{Guid.NewGuid():N}");
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
        try { Directory.Delete(_root, true); } catch (IOException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Reset_RemovesEveryFinanceTable_AndKeepsSchemaVersion()
    {
        var account = new Account { Name = "Cash", Type = AccountType.Cash, Currency = "INR" };
        await _store.SaveAccountAsync(account);

        var parent = new Category { Name = "Custom parent", Icon = "tag" };
        await _store.SaveCategoryAsync(parent);
        var child = new Category { Name = "Custom child", Icon = "tag", ParentId = parent.Id };
        await _store.SaveCategoryAsync(child);

        var transaction = TransactionFactory.Create(TransactionType.Expense, 1_250, "INR", account.Id, DateTimeOffset.UtcNow, child.Id, "Synthetic merchant");
        await _store.SaveTransactionAsync(transaction);

        var attachments = new AttachmentService(_factory, _root);
        await using (var receipt = new MemoryStream(Encoding.UTF8.GetBytes("%PDF-1.4 synthetic receipt")))
        {
            var added = await attachments.AddAttachmentAsync(transaction.Id, receipt, "synthetic.pdf", "application/pdf");
            Assert.True(added.IsSuccess);
        }

        await using (var db = await _factory.CreateDbContextAsync())
        {
            db.Tags.Add(new Tag { Name = "Synthetic" });
            db.Budgets.Add(new Budget { Name = "Synthetic budget", Kind = BudgetKind.Category, Cadence = BudgetCadence.Monthly, CategoryId = child.Id, LimitMinor = 10_000, Currency = "INR" });
            db.SavingsGoals.Add(new SavingsGoal { Name = "Synthetic goal", TargetMinor = 50_000, Currency = "INR" });
            db.RecurrenceRules.Add(new RecurrenceRule { Name = "Synthetic recurring", Frequency = RecurrenceFrequency.Monthly, StartsOn = DateOnly.FromDateTime(DateTime.Today), TransactionType = TransactionType.Expense, AmountMinor = 1_000, Currency = "INR", AccountId = account.Id, CategoryId = child.Id });
            db.NotificationSchedules.Add(new NotificationSchedule { Kind = "test", Title = "Finora reminder", Body = "Open Finora", TriggerAtUtc = DateTimeOffset.UtcNow.AddDays(1) });
            await db.SaveChangesAsync();
        }

        var service = new FinanceDataResetService(_factory);
        var result = await service.DeleteAllFinanceDataAsync();
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value!.Categories >= 2);
        Assert.Equal(1, result.Value.Attachments);

        await using var verify = await _factory.CreateDbContextAsync();
        Assert.Empty(await verify.Accounts.ToListAsync());
        Assert.Empty(await verify.Transactions.ToListAsync());
        Assert.Empty(await verify.TransactionSplits.ToListAsync());
        Assert.Empty(await verify.TransactionTags.ToListAsync());
        Assert.Empty(await verify.TransactionRevisions.ToListAsync());
        Assert.Empty(await verify.Attachments.ToListAsync());
        Assert.Empty(await verify.Categories.ToListAsync());
        Assert.Empty(await verify.Tags.ToListAsync());
        Assert.Empty(await verify.Budgets.ToListAsync());
        Assert.Empty(await verify.BudgetPeriods.ToListAsync());
        Assert.Empty(await verify.SavingsGoals.ToListAsync());
        Assert.Empty(await verify.GoalContributions.ToListAsync());
        Assert.Empty(await verify.RecurrenceRules.ToListAsync());
        Assert.Empty(await verify.RecurrenceOccurrences.ToListAsync());
        Assert.Empty(await verify.AccountReconciliations.ToListAsync());
        Assert.Empty(await verify.NotificationSchedules.ToListAsync());
        Assert.Empty(await verify.AuditEntries.ToListAsync());
        Assert.Empty(await verify.BackupMetadata.ToListAsync());
        Assert.Equal("2", (await verify.AppSettings.SingleAsync(x => x.Key == "schema.version")).Value);
    }
}
