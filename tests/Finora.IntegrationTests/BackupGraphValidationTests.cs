using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class BackupGraphValidationTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-backup-graph-{Guid.NewGuid():N}");
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
    public async Task BackupCreation_RejectsTransactionAccountCurrencyDrift()
    {
        var account = await CreateAccountAsync();
        var transaction = await CreateExpenseAsync(account.Id, -100);
        await using (var db = await _factory.CreateDbContextAsync())
        {
            await db.Transactions.Where(x => x.Id == transaction.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Currency, "USD"));
        }

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            new BackupService(_factory, _root).CreateEncryptedBackupAsync("strong-password"));
    }

    [Fact]
    public async Task BackupCreation_RejectsSplitTotalDrift()
    {
        var account = await CreateAccountAsync();
        var transaction = new FinanceTransaction
        {
            Type = TransactionType.Expense,
            AmountMinor = -1_000,
            Currency = "INR",
            AccountId = account.Id,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
        transaction.Splits = [new TransactionSplit { TransactionId = transaction.Id, AmountMinor = -1_000 }];
        await _store.SaveTransactionAsync(transaction);
        await using (var db = await _factory.CreateDbContextAsync())
        {
            await db.TransactionSplits.Where(x => x.TransactionId == transaction.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.AmountMinor, -900L));
        }

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            new BackupService(_factory, _root).CreateEncryptedBackupAsync("strong-password"));
    }

    [Fact]
    public async Task BackupCreation_RejectsActiveRuleUsingArchivedAccount()
    {
        var account = await CreateAccountAsync();
        await CreateRuleAsync(account.Id, RecurrenceStatus.Active);
        await using (var db = await _factory.CreateDbContextAsync())
        {
            await db.Accounts.Where(x => x.Id == account.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.State, AccountState.Archived));
        }

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            new BackupService(_factory, _root).CreateEncryptedBackupAsync("strong-password"));
    }

    [Fact]
    public async Task BackupCreation_AllowsPausedRuleHistoryWithArchivedAccount()
    {
        var account = await CreateAccountAsync();
        await CreateRuleAsync(account.Id, RecurrenceStatus.Paused);
        var archived = await new AccountManagementService(_factory).ArchiveAsync(account.Id);
        Assert.True(archived.IsSuccess);

        var encrypted = await new BackupService(_factory, _root).CreateEncryptedBackupAsync("strong-password");

        Assert.NotEmpty(encrypted);
    }

    private async Task<Account> CreateAccountAsync()
    {
        var account = new Account { Name = "Bank", Type = AccountType.Bank, Currency = "INR" };
        await _store.SaveAccountAsync(account);
        return account;
    }

    private async Task<FinanceTransaction> CreateExpenseAsync(Guid accountId, long amountMinor)
    {
        var transaction = new FinanceTransaction
        {
            Type = TransactionType.Expense,
            AmountMinor = amountMinor,
            Currency = "INR",
            AccountId = accountId,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
        await _store.SaveTransactionAsync(transaction);
        return transaction;
    }

    private async Task<RecurrenceRule> CreateRuleAsync(Guid accountId, RecurrenceStatus status)
    {
        var rule = new RecurrenceRule
        {
            Name = "Rent",
            Frequency = RecurrenceFrequency.Monthly,
            Interval = 1,
            StartsOn = new DateOnly(2026, 8, 1),
            TransactionType = TransactionType.Expense,
            AmountMinor = 1_000,
            Currency = "INR",
            AccountId = accountId,
            Status = status
        };
        await _store.SaveRecurrenceRuleAsync(rule);
        return rule;
    }
}
