using System.Text;
using Finora.Application;
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

    [Fact]
    public async Task Reset_PreservesNonFinanceAppSettings()
    {
        const string preferenceKey = "test.ui.preference";
        const string preferenceValue = "preserve-me";

        await using (var db = await _factory.CreateDbContextAsync())
        {
            db.AppSettings.Add(new AppSetting
            {
                Key = preferenceKey,
                Value = preferenceValue
            });
            await db.SaveChangesAsync();
        }

        var reset = await new FinanceDataResetService(_factory).DeleteAllFinanceDataAsync();
        Assert.True(reset.IsSuccess, reset.Error);

        await using var verify = await _factory.CreateDbContextAsync();
        var preserved = await verify.AppSettings.SingleAsync(setting => setting.Key == preferenceKey);
        Assert.Equal(preferenceValue, preserved.Value);
        Assert.Equal("2", (await verify.AppSettings.SingleAsync(setting => setting.Key == "schema.version")).Value);
    }

    [Fact]
    public async Task CompleteResetWorkflow_RemovesReceipt_AndLeavesHealthyReusableDatabase()
    {
        var account = new Account { Name = "Reset cash", Type = AccountType.Cash, Currency = "INR" };
        await _store.SaveAccountAsync(account);
        var transaction = TransactionFactory.Create(
            TransactionType.Expense,
            2_500,
            "INR",
            account.Id,
            DateTimeOffset.UtcNow,
            merchant: "Synthetic reset merchant");
        await _store.SaveTransactionAsync(transaction);

        var attachments = new AttachmentService(_factory, _root);
        AttachmentInfo attachment;
        await using (var receipt = new MemoryStream(Encoding.UTF8.GetBytes("%PDF-1.4 synthetic reset receipt")))
        {
            var added = await attachments.AddAttachmentAsync(
                transaction.Id,
                receipt,
                "reset-receipt.pdf",
                "application/pdf");
            Assert.True(added.IsSuccess, added.Error);
            attachment = Assert.IsType<AttachmentInfo>(added.Value);
        }

        var receiptPath = await attachments.GetLocalPathAsync(attachment.Id);
        Assert.True(receiptPath.IsSuccess, receiptPath.Error);
        Assert.NotNull(receiptPath.Value);
        Assert.True(File.Exists(receiptPath.Value));

        var reset = await new FinanceDataResetService(_factory).DeleteAllFinanceDataAsync();
        Assert.True(reset.IsSuccess, reset.Error);
        Assert.NotNull(reset.Value);
        Assert.Equal(1, reset.Value!.Attachments);

        var removedOrphans = await attachments.CleanupOrphanedFilesAsync();
        Assert.Equal(1, removedOrphans);
        Assert.False(File.Exists(receiptPath.Value));
        Assert.Equal(0, await attachments.GetStorageUsageBytesAsync());

        await new DatabaseInitializer(_factory).InitializeAsync();

        var freshAccount = new Account
        {
            Name = "Fresh cash after reset",
            Type = AccountType.Cash,
            Currency = "INR"
        };
        var savedAccountId = await _store.SaveAccountAsync(freshAccount);
        Assert.Equal(freshAccount.Id, savedAccountId);
        Assert.NotEqual(Guid.Empty, savedAccountId);

        var freshTransaction = TransactionFactory.Create(
            TransactionType.Income,
            10_000,
            "INR",
            freshAccount.Id,
            DateTimeOffset.UtcNow,
            merchant: "Synthetic post-reset income");
        var savedTransactionId = await _store.SaveTransactionAsync(freshTransaction);
        Assert.Equal(freshTransaction.Id, savedTransactionId);
        Assert.NotEqual(Guid.Empty, savedTransactionId);

        await using (var verify = await _factory.CreateDbContextAsync())
        {
            Assert.Equal(1, await verify.Accounts.CountAsync());
            Assert.Equal(1, await verify.Transactions.CountAsync());
            Assert.Empty(await verify.Attachments.ToListAsync());
            Assert.Equal("2", (await verify.AppSettings.SingleAsync(x => x.Key == "schema.version")).Value);
        }

        var integrity = await new DataIntegrityService(_factory, _root).CheckAsync();
        Assert.True(integrity.IsHealthy, integrity.ToSanitizedText());
        Assert.True(integrity.DatabaseIntegrityPassed);
        Assert.True(integrity.ForeignKeysPassed);
        Assert.Equal(1, integrity.AccountsChecked);
        Assert.Equal(1, integrity.TransactionsChecked);
        Assert.Equal(0, integrity.AttachmentsChecked);
    }
}
