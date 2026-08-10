using System.Security.Cryptography;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class AttachmentPathSafetyTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-attachment-path-{Guid.NewGuid():N}");
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
        try { Directory.Delete(_root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task AddAttachment_StoresAndResolvesOnlyInsideReceiptRoot()
    {
        var transaction = await CreateTransactionAsync();
        var service = new AttachmentService(_factory, _root);
        await using var content = new MemoryStream([1, 2, 3, 4]);

        var added = await service.AddAttachmentAsync(transaction.Id, content, "receipt.png", "image/png");

        Assert.True(added.IsSuccess);
        var local = await service.GetLocalPathAsync(added.Value!.Id);
        Assert.True(local.IsSuccess);
        var attachmentRoot = Path.GetFullPath(Path.Combine(_root, "attachments")) + Path.DirectorySeparatorChar;
        Assert.StartsWith(attachmentRoot, Path.GetFullPath(local.Value!), OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        Assert.True(File.Exists(local.Value));
    }

    [Fact]
    public async Task TamperedTraversalPath_FailsClosedAndIntegrityReportsUnsafePath()
    {
        var transaction = await CreateTransactionAsync();
        var attachmentId = Guid.NewGuid();
        await using (var db = await _factory.CreateDbContextAsync())
        {
            db.Attachments.Add(new Attachment
            {
                Id = attachmentId,
                TransactionId = transaction.Id,
                RelativePath = "attachments/../outside.txt",
                OriginalFileName = "receipt.txt",
                ContentType = "application/pdf",
                SizeBytes = 4,
                Sha256 = SHA256.HashData([1, 2, 3, 4])
            });
            await db.SaveChangesAsync();
        }

        var service = new AttachmentService(_factory, _root);
        var local = await service.GetLocalPathAsync(attachmentId);
        Assert.False(local.IsSuccess);

        var report = await new DataIntegrityService(_factory, _root).CheckAsync();
        Assert.Contains(report.Issues, issue => issue.Code == "ATTACHMENT_PATH_UNSAFE" && issue.Count == 1);
    }

    [Fact]
    public async Task CaseVariantReceiptPrefix_IsRejectedOnCaseSensitivePlatforms()
    {
        if (OperatingSystem.IsWindows()) return;

        var transaction = await CreateTransactionAsync();
        var attachmentId = Guid.NewGuid();
        await using (var db = await _factory.CreateDbContextAsync())
        {
            db.Attachments.Add(new Attachment
            {
                Id = attachmentId,
                TransactionId = transaction.Id,
                RelativePath = $"ATTACHMENTS/{transaction.Id:N}/{attachmentId:N}.png",
                OriginalFileName = "receipt.png",
                ContentType = "image/png",
                SizeBytes = 4,
                Sha256 = SHA256.HashData([1, 2, 3, 4])
            });
            await db.SaveChangesAsync();
        }

        var local = await new AttachmentService(_factory, _root).GetLocalPathAsync(attachmentId);
        Assert.False(local.IsSuccess);
        var report = await new DataIntegrityService(_factory, _root).CheckAsync();
        Assert.Contains(report.Issues, issue => issue.Code == "ATTACHMENT_PATH_UNSAFE");
    }

    private async Task<FinanceTransaction> CreateTransactionAsync()
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
        return transaction;
    }
}
