using Finora.Application;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class IntegrityRegressionTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-integrity-regression-{Guid.NewGuid():N}");
    private FinanceStoreTests.TestFactory _factory = null!;
    private FinanceStore _store = null!;
    private Account _account = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        var options = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_root, "finora.db")}")
            .Options;
        _factory = new FinanceStoreTests.TestFactory(options);
        _store = new FinanceStore(_factory, new DatabaseInitializer(_factory));
        await _store.InitializeAsync();
        _account = new Account { Name = "Synthetic account", Type = AccountType.Bank, Currency = "INR" };
        await _store.SaveAccountAsync(_account);
    }

    public Task DisposeAsync()
    {
        try { Directory.Delete(_root, true); } catch (IOException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task IntegrityCheck_DetectsExpenseSignCorruptedOutsideEfValidation()
    {
        var transaction = TransactionFactory.Create(TransactionType.Expense, 1_000, "INR", _account.Id, DateTimeOffset.UtcNow);
        await _store.SaveTransactionAsync(transaction);
        await using (var db = await _factory.CreateDbContextAsync())
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Transactions SET AmountMinor = {1000L} WHERE Id = {transaction.Id}");
        }

        var report = await new DataIntegrityService(_factory, _root).CheckAsync();

        Assert.Contains(report.Issues, issue => issue.Code == "TRANSACTION_SIGN_INVALID" && issue.AffectedRecords == 1);
    }

    [Fact]
    public async Task IntegrityCheck_DetectsExtremeAmountCorruptedOutsideEfValidation()
    {
        var transaction = TransactionFactory.Create(TransactionType.Adjustment, 1_000, "INR", _account.Id, DateTimeOffset.UtcNow);
        await _store.SaveTransactionAsync(transaction);
        await using (var db = await _factory.CreateDbContextAsync())
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Transactions SET AmountMinor = {long.MinValue} WHERE Id = {transaction.Id}");
        }

        var report = await new DataIntegrityService(_factory, _root).CheckAsync();

        Assert.Contains(report.Issues, issue => issue.Code == "TRANSACTION_AMOUNT_INVALID" && issue.AffectedRecords == 1);
    }

    [Fact]
    public async Task IntegrityCheck_RejectsAttachmentPathOutsidePrivateReceiptRoot()
    {
        var transaction = TransactionFactory.Create(TransactionType.Expense, 500, "INR", _account.Id, DateTimeOffset.UtcNow);
        await _store.SaveTransactionAsync(transaction);
        var attachmentId = Guid.NewGuid();
        await using (var db = await _factory.CreateDbContextAsync())
        {
            db.Attachments.Add(new Attachment
            {
                Id = attachmentId,
                TransactionId = transaction.Id,
                RelativePath = $"attachments/{transaction.Id:N}/{attachmentId:N}.pdf",
                OriginalFileName = "synthetic.pdf",
                ContentType = "application/pdf",
                SizeBytes = 1
            });
            await db.SaveChangesAsync();
            await db.Attachments.Where(x => x.Id == attachmentId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RelativePath, "finora.db"));
        }

        var report = await new DataIntegrityService(_factory, _root).CheckAsync();

        Assert.Contains(report.Issues, issue => issue.Code == "ATTACHMENT_PATH_UNSAFE" && issue.AffectedRecords == 1);
    }
}
