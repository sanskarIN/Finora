using System.Globalization;
using Finora.Application;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class ReportCurrencyPrecisionTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-report-precision-{Guid.NewGuid():N}");
    private FinanceStoreTests.TestFactory _factory = null!;
    private FinanceStore _store = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        var options = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_root, "finora.db")}")
            .Options;
        _factory = new FinanceStoreTests.TestFactory(options);
        _store = new FinanceStore(_factory, new DatabaseInitializer(_factory), TimeZoneInfo.Utc);
        await _store.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        try { Directory.Delete(_root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        return Task.CompletedTask;
    }

    [Theory]
    [InlineData("JPY", "1234.6", 1235L)]
    [InlineData("INR", "12.345", 1235L)]
    [InlineData("KWD", "12.3456", 12346L)]
    [InlineData("CLF", "1.23456", 12346L)]
    public async Task IncomeExpenseReport_PreservesExactCurrencyMinorUnits(string currency, string majorText, long expectedMinor)
    {
        var converted = Money.FromMajorUnits(decimal.Parse(majorText, CultureInfo.InvariantCulture), currency);
        Assert.Equal(expectedMinor, converted.MinorUnits);

        var account = new Account
        {
            Name = $"{currency} report account",
            Type = AccountType.Bank,
            Currency = currency
        };
        await _store.SaveAccountAsync(account);
        var occurredAt = new DateTimeOffset(2026, 8, 16, 9, 0, 0, TimeSpan.Zero);
        await _store.SaveTransactionAsync(TransactionFactory.Create(
            TransactionType.Income,
            converted.MinorUnits,
            currency,
            account.Id,
            occurredAt,
            merchant: "Synthetic report income"));
        await _store.SaveTransactionAsync(TransactionFactory.Create(
            TransactionType.Expense,
            checked(converted.MinorUnits * 2),
            currency,
            account.Id,
            occurredAt.AddMinutes(1),
            merchant: "Synthetic report expense"));

        var report = await new AdvancedReportService(_factory).GetIncomeExpenseAsync(
            occurredAt.AddHours(-1),
            occurredAt.AddHours(1),
            currency);

        Assert.Equal(currency, report.Currency);
        Assert.Equal(expectedMinor, report.Points.Single(point => point.Label == "Income").ValueMinor);
        Assert.Equal(checked(expectedMinor * 2), report.Points.Single(point => point.Label == "Expense").ValueMinor);
    }
}
