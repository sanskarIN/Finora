using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class MetadataPersistenceInvariantTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-metadata-invariants-{Guid.NewGuid():N}");
    private FinanceStoreTests.TestFactory _factory = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        var options = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_root, "finora.db")}")
            .Options;
        _factory = new FinanceStoreTests.TestFactory(options);
        await new DatabaseInitializer(_factory).InitializeAsync();
    }

    public Task DisposeAsync()
    {
        try { Directory.Delete(_root, true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task AttachmentTraversalMetadata_IsRejectedBeforePersistence()
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Attachments.Add(new Attachment
        {
            TransactionId = Guid.NewGuid(),
            RelativePath = "attachments/../outside.pdf",
            OriginalFileName = "receipt.pdf",
            ContentType = "application/pdf",
            SizeBytes = 10,
            Sha256 = new byte[32]
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task NotificationWithEmptyTitle_IsRejectedBeforePersistence()
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.NotificationSchedules.Add(new NotificationSchedule
        {
            Kind = "Generic",
            Title = " ",
            Body = "Open Finora.",
            TriggerAtUtc = DateTimeOffset.UtcNow.AddMinutes(10)
        });

        await Assert.ThrowsAnyAsync<ArgumentException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task PaidOccurrenceWithoutGeneratedTransaction_IsRejectedBeforePersistence()
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.RecurrenceOccurrences.Add(new RecurrenceOccurrence
        {
            RecurrenceRuleId = Guid.NewGuid(),
            DueOn = new DateOnly(2026, 8, 10),
            Status = OccurrenceStatus.Paid,
            PaidAmountMinor = 100
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task ReconciliationWithIncorrectDifference_IsRejectedBeforePersistence()
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.AccountReconciliations.Add(new AccountReconciliation
        {
            AccountId = Guid.NewGuid(),
            StatementDateUtc = DateTimeOffset.UtcNow,
            BookBalanceMinor = 100,
            StatementBalanceMinor = 150,
            DifferenceMinor = 40,
            CompletedAtUtc = DateTimeOffset.UtcNow
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task CategoryWithNegativeSortOrder_IsRejectedBeforePersistence()
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Categories.Add(new Category { Name = "Synthetic", SortOrder = -1 });

        await Assert.ThrowsAnyAsync<ArgumentException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task RevisionWithoutSnapshot_IsRejectedBeforePersistence()
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.TransactionRevisions.Add(new TransactionRevision
        {
            TransactionId = Guid.NewGuid(),
            ChangeKind = "BeforeEdit",
            SnapshotJson = " ",
            ChangedAtUtc = DateTimeOffset.UtcNow
        });

        await Assert.ThrowsAnyAsync<ArgumentException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DeletedTransactionWithoutDeletionTimestamp_IsRejectedBeforePersistence()
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Transactions.Add(new FinanceTransaction
        {
            Type = TransactionType.Expense,
            AmountMinor = -100,
            Currency = "INR",
            AccountId = Guid.NewGuid(),
            OccurredAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = true,
            DeletedAtUtc = null
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task PaidOccurrence_MayRetainValidHistoricalPostponementDate()
    {
        var account = new Account { Name = "Synthetic", Type = AccountType.Bank, Currency = "INR" };
        var rule = new RecurrenceRule
        {
            Name = "Synthetic recurring",
            Frequency = RecurrenceFrequency.Monthly,
            StartsOn = new DateOnly(2026, 8, 1),
            TransactionType = TransactionType.Expense,
            AmountMinor = 100,
            Currency = "INR",
            AccountId = account.Id
        };
        var transaction = new FinanceTransaction
        {
            Type = TransactionType.Expense,
            AmountMinor = -100,
            Currency = "INR",
            AccountId = account.Id,
            OccurredAtUtc = new DateTimeOffset(2026, 8, 12, 0, 0, 0, TimeSpan.Zero),
            RecurrenceRuleId = rule.Id
        };
        var occurrence = new RecurrenceOccurrence
        {
            RecurrenceRuleId = rule.Id,
            DueOn = new DateOnly(2026, 8, 10),
            PostponedTo = new DateOnly(2026, 8, 12),
            Status = OccurrenceStatus.Paid,
            PaidAmountMinor = 100,
            GeneratedTransactionId = transaction.Id
        };

        await using var db = await _factory.CreateDbContextAsync();
        db.Accounts.Add(account);
        db.RecurrenceRules.Add(rule);
        db.Transactions.Add(transaction);
        db.RecurrenceOccurrences.Add(occurrence);

        Assert.True(await db.SaveChangesAsync() > 0);
    }
}
