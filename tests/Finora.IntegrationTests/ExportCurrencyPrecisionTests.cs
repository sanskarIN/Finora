using System.Text;
using Finora.Application;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class ExportCurrencyPrecisionTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-export-currency-{Guid.NewGuid():N}");
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
    public async Task CsvExport_PreservesExactMinorUnitsAcrossSupportedPrecisionClasses()
    {
        var fixtures = new[]
        {
            (Currency: "JPY", Major: 1234.6m),
            (Currency: "INR", Major: 12.345m),
            (Currency: "KWD", Major: 12.3456m),
            (Currency: "CLF", Major: 1.23456m)
        };

        var expected = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        var occurredAt = new DateTimeOffset(2026, 8, 16, 6, 30, 0, TimeSpan.Zero);
        foreach (var fixture in fixtures)
        {
            var account = new Account
            {
                Name = $"{fixture.Currency} export account",
                Type = AccountType.Bank,
                Currency = fixture.Currency
            };
            await _store.SaveAccountAsync(account);

            var money = Money.FromMajorUnits(fixture.Major, fixture.Currency);
            expected[fixture.Currency] = money.MinorUnits;
            await _store.SaveTransactionAsync(TransactionFactory.Create(
                TransactionType.Income,
                money.MinorUnits,
                fixture.Currency,
                account.Id,
                occurredAt.AddMinutes(expected.Count),
                merchant: $"{fixture.Currency} synthetic export"));
        }

        var service = new ExportService(_factory);
        var csv = await service.ExportTransactionsCsvAsync();

        foreach (var pair in expected)
        {
            Assert.Contains($",{pair.Value},\"{pair.Key}\",", csv, StringComparison.Ordinal);
            Assert.Contains($"\"{pair.Key} export account\"", csv, StringComparison.Ordinal);
        }

        await using var previewStream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var preview = await service.PreviewCsvAsync(previewStream);

        Assert.Equal(4, preview.Count);
        Assert.All(preview, row => Assert.True(row.IsValid, row.Error));
        Assert.Equal(
            expected.Values.OrderBy(value => value),
            preview.Select(row => long.Parse(row.Amount!, System.Globalization.CultureInfo.InvariantCulture)).OrderBy(value => value));
    }

    [Fact]
    public async Task CsvExport_ReimportsWithoutChangingMinorUnitValues()
    {
        var sourceFixtures = new[]
        {
            (Currency: "JPY", Minor: 1235L),
            (Currency: "INR", Minor: 1235L),
            (Currency: "KWD", Minor: 12346L),
            (Currency: "CLF", Minor: 12346L)
        };
        var occurredAt = new DateTimeOffset(2026, 8, 16, 7, 0, 0, TimeSpan.Zero);

        foreach (var fixture in sourceFixtures)
        {
            var account = new Account
            {
                Name = $"{fixture.Currency} roundtrip account",
                Type = AccountType.Bank,
                Currency = fixture.Currency
            };
            await _store.SaveAccountAsync(account);
            await _store.SaveTransactionAsync(TransactionFactory.Create(
                TransactionType.Income,
                fixture.Minor,
                fixture.Currency,
                account.Id,
                occurredAt.AddMinutes(Array.IndexOf(sourceFixtures, fixture))));
        }

        var csv = await new ExportService(_factory).ExportTransactionsCsvAsync();

        var destinationRoot = Path.Combine(_root, "destination");
        Directory.CreateDirectory(destinationRoot);
        var destinationOptions = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseSqlite($"Data Source={Path.Combine(destinationRoot, "finora.db")}")
            .Options;
        var destinationFactory = new FinanceStoreTests.TestFactory(destinationOptions);
        var destinationStore = new FinanceStore(destinationFactory, new DatabaseInitializer(destinationFactory));
        await destinationStore.InitializeAsync();

        foreach (var fixture in sourceFixtures)
        {
            await destinationStore.SaveAccountAsync(new Account
            {
                Name = $"{fixture.Currency} roundtrip account",
                Type = AccountType.Bank,
                Currency = fixture.Currency
            });
        }

        var mapping = new CsvColumnMapping(
            "Date",
            "Type",
            "AmountMinor",
            "Account",
            "Currency",
            "Category",
            "Merchant",
            "Note",
            "PaymentMethod",
            "ManualLocation",
            "TransferGroupId",
            "CounterpartyAccount",
            "Tags",
            true);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var import = await new CsvImportService(destinationFactory).ImportAsync(
            stream,
            new CsvImportOptions(mapping, null, false, false, "INR"));

        Assert.True(import.IsSuccess, import.Error);
        Assert.Equal(4, import.Value!.ImportedRows);
        Assert.Equal(0, import.Value.InvalidRows);

        var restored = await destinationStore.SearchTransactionsAsync();
        Assert.Equal(4, restored.Count);
        foreach (var fixture in sourceFixtures)
        {
            var transaction = Assert.Single(restored, item => item.Currency == fixture.Currency);
            Assert.Equal(fixture.Minor, transaction.AmountMinor);
        }
    }
}
