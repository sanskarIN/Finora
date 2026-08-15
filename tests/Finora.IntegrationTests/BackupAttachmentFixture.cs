using System.Text;
using Finora.Application;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

internal sealed class BackupAttachmentFixture : IAsyncDisposable
{
    public const string Password = "receipt-password-123";

    private BackupAttachmentFixture(
        string root,
        FinanceStoreTests.TestFactory factory,
        FinanceStore store,
        BackupService backup,
        AttachmentService attachments,
        Guid accountId,
        Guid transactionId)
    {
        Root = root;
        Factory = factory;
        Store = store;
        Backup = backup;
        Attachments = attachments;
        AccountId = accountId;
        TransactionId = transactionId;
    }

    public string Root { get; }
    public FinanceStoreTests.TestFactory Factory { get; }
    public FinanceStore Store { get; }
    public BackupService Backup { get; }
    public AttachmentService Attachments { get; }
    public Guid AccountId { get; }
    public Guid TransactionId { get; }

    public static async Task<BackupAttachmentFixture> CreateAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), $"finora-backup-receipt-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var options = new DbContextOptionsBuilder<FinoraDbContext>()
                .UseSqlite($"Data Source={Path.Combine(root, "finora.db")}")
                .Options;
            var factory = new FinanceStoreTests.TestFactory(options);
            var store = new FinanceStore(factory, new DatabaseInitializer(factory));
            await store.InitializeAsync();

            var account = new Account { Name = "Receipt account", Type = AccountType.Cash, Currency = "INR" };
            await store.SaveAccountAsync(account);
            var transaction = TransactionFactory.Create(
                TransactionType.Expense,
                725,
                "INR",
                account.Id,
                DateTimeOffset.UtcNow,
                merchant: "Receipt test");
            await store.SaveTransactionAsync(transaction);

            var attachments = new AttachmentService(factory, root);
            await using var receipt = new MemoryStream(Encoding.UTF8.GetBytes("%PDF-1.4 authenticated receipt fixture"));
            var added = await attachments.AddAttachmentAsync(transaction.Id, receipt, "receipt.pdf", "application/pdf");
            if (!added.IsSuccess)
                throw new InvalidOperationException(added.Error ?? "Receipt fixture could not be created.");

            return new BackupAttachmentFixture(
                root,
                factory,
                store,
                new BackupService(factory, root),
                attachments,
                account.Id,
                transaction.Id);
        }
        catch
        {
            try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            throw;
        }
    }

    public async Task<byte[]> CreateBackupAsync() => await Backup.CreateEncryptedBackupAsync(Password);

    public async Task AssertCurrentDataIntactAsync()
    {
        Assert.Contains(await Store.GetAccountsAsync(), account => account.Id == AccountId);
        Assert.Contains(await Store.SearchTransactionsAsync(), transaction => transaction.Id == TransactionId);
        Assert.Single(await Attachments.GetAttachmentsAsync(TransactionId));
    }

    public ValueTask DisposeAsync()
    {
        try { Directory.Delete(Root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        return ValueTask.CompletedTask;
    }
}
