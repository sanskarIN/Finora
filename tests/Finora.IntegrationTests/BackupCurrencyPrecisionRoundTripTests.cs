using Finora.Application;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class BackupCurrencyPrecisionRoundTripTests : IAsyncLifetime
{
    private const string Password = "currency-roundtrip-password-123";
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-backup-currency-{Guid.NewGuid():N}");
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
    public async Task EncryptedBackup_RestoresExactValuesAcrossCurrencyPrecisionClasses()
    {
        var fixtures = new[]
        {
            (Currency: "JPY", Major: 1234.6m, ExpectedMinor: 1235L),
            (Currency: "INR", Major: 12.345m, ExpectedMinor: 1235L),
            (Currency: "KWD", Major: 12.3456m, ExpectedMinor: 12346L),
            (Currency: "CLF", Major: 1.23456m, ExpectedMinor: 12346L)
        };
        var occurredAt = new DateTimeOffset(2026, 8, 16, 8, 0, 0, TimeSpan.Zero);

        foreach (var fixture in fixtures)
        {
            var converted = Money.FromMajorUnits(fixture.Major, fixture.Currency);
            Assert.Equal(fixture.ExpectedMinor, converted.MinorUnits);

            var account = new Account
            {
                Name = $"{fixture.Currency} backup account",
                Type = AccountType.Bank,
                Currency = fixture.Currency
            };
            await _store.SaveAccountAsync(account);
            await _store.SaveTransactionAsync(TransactionFactory.Create(
                TransactionType.Income,
                converted.MinorUnits,
                fixture.Currency,
                account.Id,
                occurredAt.AddMinutes(Array.IndexOf(fixtures, fixture)),
                merchant: $"{fixture.Currency} synthetic backup"));
        }

        var backup = new BackupService(_factory, _root);
        var encrypted = await backup.CreateEncryptedBackupAsync(Password);
        Assert.NotEmpty(encrypted);

        await using (var previewStream = new MemoryStream(encrypted, writable: false))
        {
            var preview = await backup.PreviewEncryptedBackupAsync(previewStream, Password);
            Assert.True(preview.IsSuccess, preview.Error);
            Assert.Equal(4, preview.Value!.AccountCount);
            Assert.Equal(4, preview.Value.TransactionCount);
        }

        var reset = await new FinanceDataResetService(_factory).DeleteAllFinanceDataAsync();
        Assert.True(reset.IsSuccess, reset.Error);
        Assert.Empty(await _store.GetAccountsAsync());
        Assert.Empty(await _store.SearchTransactionsAsync());

        await using (var restoreStream = new MemoryStream(encrypted, writable: false))
        {
            var restored = await backup.RestoreEncryptedBackupAsync(restoreStream, Password);
            Assert.True(restored.IsSuccess, restored.Error);
        }

        var accounts = await _store.GetAccountsAsync();
        var transactions = await _store.SearchTransactionsAsync();
        Assert.Equal(4, accounts.Count);
        Assert.Equal(4, transactions.Count);

        foreach (var fixture in fixtures)
        {
            var account = Assert.Single(accounts, item => item.Currency == fixture.Currency);
            Assert.Equal($"{fixture.Currency} backup account", account.Name);

            var transaction = Assert.Single(transactions, item => item.Currency == fixture.Currency);
            Assert.Equal(fixture.ExpectedMinor, transaction.AmountMinor);
            Assert.Equal(account.Name, transaction.AccountName);
        }

        var integrity = await new DataIntegrityService(_factory, _root).CheckAsync();
        Assert.True(integrity.IsHealthy, integrity.ToSanitizedText());
        Assert.True(integrity.DatabaseIntegrityPassed);
        Assert.True(integrity.ForeignKeysPassed);
        Assert.Equal(4, integrity.AccountsChecked);
        Assert.Equal(4, integrity.TransactionsChecked);
    }
}
