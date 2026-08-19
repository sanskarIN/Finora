using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class AttachmentMetadataConsistencyTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-attachment-metadata-{Guid.NewGuid():N}");
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
        try { Directory.Delete(_root, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task AddAttachment_UsesMimeTypeForInternalExtensionEvenWhenOriginalNameConflicts()
    {
        var transaction = await CreateTransactionAsync();
        var service = new AttachmentService(_factory, _root);
        await using var content = new MemoryStream([1, 2, 3, 4]);

        var added = await service.AddAttachmentAsync(transaction.Id, content, "receipt.pdf", "image/png");

        Assert.True(added.IsSuccess);
        var local = await service.GetLocalPathAsync(added.Value!.Id);
        Assert.True(local.IsSuccess);
        Assert.Equal(".png", Path.GetExtension(local.Value));
        Assert.Equal("image/png", added.Value.ContentType);
        Assert.Equal("receipt.pdf", added.Value.FileName);
    }

    [Fact]
    public async Task AddAttachment_TrimsMimeTypeBeforePersistingMetadata()
    {
        var transaction = await CreateTransactionAsync();
        var service = new AttachmentService(_factory, _root);
        await using var content = new MemoryStream([5, 6, 7]);

        var added = await service.AddAttachmentAsync(transaction.Id, content, "receipt.png", "  image/png  ");

        Assert.True(added.IsSuccess);
        Assert.Equal("image/png", added.Value!.ContentType);
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
