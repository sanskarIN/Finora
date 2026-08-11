namespace Finora.UiTests;

public sealed class ReportDashboardSourceContractTests
{
    [Fact]
    public void Dashboard_ExposesPeriodSelectorAndCurrentCurrencyScopeBinding()
    {
        var xaml = ReadContract("DashboardPage.xaml");
        var viewModel = ReadContract("DashboardViewModel.cs");

        Assert.Contains("ItemsSource=\"{Binding PeriodChoices}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedItem=\"{Binding SelectedPeriod}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding PeriodRange}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding CurrencyScope}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("ReportingCurrencyNotice", xaml, StringComparison.Ordinal);
        Assert.Contains("DashboardPeriodPolicy.Resolve", viewModel, StringComparison.Ordinal);
        Assert.Contains("LocalDateRange.ToUtc", viewModel, StringComparison.Ordinal);
        Assert.Contains("GetAccountsAsync", viewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("GetDashboardAsync(", viewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_ExposeEveryMasterPromptReportSection()
    {
        var xaml = ReadContract("ReportsPage.xaml");
        var viewModel = ReadContract("ReportsViewModel.cs");

        string[] requiredBindings =
        [
            "CategoryPoints",
            "IncomeExpensePoints",
            "MonthlyNetRows",
            "YearlyNetRows",
            "MerchantRows",
            "BudgetRows",
            "RecurringRows",
            "SavingsRows",
            "AccountTrendRows"
        ];

        foreach (var binding in requiredBindings)
        {
            Assert.Contains($"{{Binding {binding}}}", xaml, StringComparison.Ordinal);
            Assert.Contains(binding, viewModel, StringComparison.Ordinal);
        }

        Assert.Contains("GetYearlyComparisonAsync", viewModel, StringComparison.Ordinal);
        Assert.Contains("GetRecurringObligationsAsync", viewModel, StringComparison.Ordinal);
        Assert.Contains("GetSavingsProgressAsync", viewModel, StringComparison.Ordinal);
        Assert.Contains("LocalDateRange.ToUtc", viewModel, StringComparison.Ordinal);
    }

    private static string ReadContract(string fileName)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", fileName));
}
