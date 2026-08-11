namespace Finora.UiTests;

public sealed class PrivacyAmountSurfaceContractTests
{
    [Fact]
    public void SharedConverter_UsesPrivacyAndCurrencyAwareMoneyFormatting()
    {
        var source = ReadContract("PrivacyMoneyConverter.cs");

        Assert.Contains("settings.PrivacyMode || settings.HideAmountsOnLaunch", source, StringComparison.Ordinal);
        Assert.Contains("new Money(minor, currency).Format(culture)", source, StringComparison.Ordinal);
        Assert.Contains("••••", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("AccountsPage.xaml")]
    [InlineData("AccountDetailPage.xaml")]
    [InlineData("TransactionsPage.xaml")]
    [InlineData("TransactionToolsPage.xaml")]
    [InlineData("BudgetsPage.xaml")]
    [InlineData("SavingsPage.xaml")]
    [InlineData("RecurringPage.xaml")]
    public void PassiveMoneyCards_UsePrivacyMoneyConverter(string fileName)
    {
        var xaml = ReadContract(fileName);

        Assert.Contains("PrivacyMoneyConverter", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("StringFormat='{0} minor'", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("StringFormat='Amount: {0} minor'", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void TransactionDetail_SplitRowsUsePrivacyDisplayValue()
    {
        var xaml = ReadContract("TransactionDetailPage.xaml");
        var viewModel = ReadContract("TransactionDetailViewModel.cs");

        Assert.Contains("Text=\"{Binding DisplayAmount}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding AmountMajor}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("DisplayMagnitude", viewModel, StringComparison.Ordinal);
        Assert.Contains("SafeMagnitude", viewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_MaskRowsAndHideQuantitativeCharts()
    {
        var xaml = ReadContract("ReportsPage.xaml");
        var viewModel = ReadContract("ReportsViewModel.cs");

        Assert.Contains("IsVisible=\"{Binding AmountsVisible}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AmountsHidden = _settings.PrivacyMode || _settings.HideAmountsOnLaunch", viewModel, StringComparison.Ordinal);
        Assert.Contains("=> AmountsHidden ? \"••••\"", viewModel, StringComparison.Ordinal);
        Assert.Contains("Array.Empty<ReportPoint>()", viewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void SavingsForecast_DoesNotRevealEstimatedMoneyWhileHidden()
    {
        var source = ReadContract("SavingsViewModel.cs");

        Assert.Contains("_settings.PrivacyMode || _settings.HideAmountsOnLaunch", source, StringComparison.Ordinal);
        Assert.Contains("monthly contribution estimate is hidden by privacy mode", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Reconciliation_UsesPrivacyDisplayAndSafeLocalDateBoundary()
    {
        var source = ReadContract("ReconciliationViewModel.cs");
        var xaml = ReadContract("ReconciliationPage.xaml");

        Assert.Contains("DisplayMoney", source, StringComparison.Ordinal);
        Assert.Contains("LocalDateRange.ToUtc", source, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding Difference, StringFormat='Difference: {0}'}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("DifferenceMinor", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void AccountDetail_UsesCurrencyPrecisionInsteadOfFixedTwoDecimals()
    {
        var source = ReadContract("AccountDetailViewModel.cs");

        Assert.Contains("money.DecimalPlaces", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ToString(\"0.00\"", source, StringComparison.Ordinal);
    }

    private static string ReadContract(string fileName)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", fileName));
}
