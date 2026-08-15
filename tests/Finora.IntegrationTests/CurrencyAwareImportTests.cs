using System.Text;
using Finora.Application;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class CurrencyAwareImportTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-import-currency-{Guid.NewGuid():N}");
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

    [Theory]
    [InlineData("JPY", "1234.6", -1235L)]
    [InlineData("KWD", "12.3456", -12346L)]
    [InlineData("CLF", "1.23456", -12346L)]
    public async Task MajorUnitImport_UsesCurrencySpecificPrecision(string currency, string amount, long expectedMinor)
    {
        var account = new Account { Name = "Import account", Type = AccountType.Bank, Currency = currency };
        await _store.SaveAccountAsync(account);
        var csv = $"Date,Type,Amount,Account,Currency\n2026-08-01,Expense,{amount},Import account,{currency}\n";
        var mapping = new CsvColumnMapping("Date", "Type", "Amount", "Account", "Currency", null, null, null, null, null, null, null, null, false);
        var service = new CsvImportService(_factory);

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await service.ImportAsync(stream, new CsvImportOptions(mapping, null, false, true, currency));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.ImportedRows);
        Assert.Equal(0, result.Value.InvalidRows);
        Assert.Equal(expectedMinor, Assert.Single(await _store.SearchTransactionsAsync()).AmountMinor);
    }

    [Fact]
    public async Task LongMinMinorUnitRow_IsRejectedWithoutOverflow()
    {
        var account = new Account { Name = "Import account", Type = AccountType.Bank, Currency = "INR" };
        await _store.SaveAccountAsync(account);
        var csv = $"Date,Type,AmountMinor,Account,Currency\n2026-08-01,Expense,{long.MinValue},Import account,INR\n";
        var mapping = new CsvColumnMapping("Date", "Type", "AmountMinor", "Account", "Currency", null, null, null, null, null, null, null, null, true);
        var service = new CsvImportService(_factory);

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await service.ImportAsync(stream, new CsvImportOptions(mapping, null, false, true, "INR"));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.ImportedRows);
        Assert.Equal(1, result.Value.InvalidRows);
        Assert.Empty(await _store.SearchTransactionsAsync());
    }

    [Fact]
    public async Task ParseErrors_AreCountedExactlyOnce()
    {
        var account = new Account { Name = "Import account", Type = AccountType.Bank, Currency = "INR" };
        await _store.SaveAccountAsync(account);
        const string csv = "Date,Type,Amount,Account,Currency\nnot-a-date,Expense,10.00,Import account,INR\n2026-08-01,Expense,10.00,Import account,INR\n";
        var mapping = new CsvColumnMapping("Date", "Type", "Amount", "Account", "Currency", null, null, null, null, null, null, null, null, false);
        var service = new CsvImportService(_factory);

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await service.ImportAsync(stream, new CsvImportOptions(mapping, null, false, true, "INR"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.ImportedRows);
        Assert.Equal(1, result.Value.InvalidRows);
        Assert.Single(result.Value.Errors);
    }
}
