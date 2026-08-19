namespace Finora.UiTests;

public sealed class AccessibilityRegressionContractTests
{
    [Fact]
    public void DashboardPeriodControls_ExposeSemanticDescriptions()
    {
        var source = ReadContract("DashboardPage.xaml");

        Assert.Contains("SemanticProperties.Description=\"{DynamicResource Text.DashboardPeriodPickerDescription}\"", source, StringComparison.Ordinal);
        Assert.Contains("SemanticProperties.Description=\"{DynamicResource Text.ApplyDashboardPeriodDescription}\"", source, StringComparison.Ordinal);
        Assert.Contains("SemanticProperties.Description=\"{DynamicResource Text.ToggleAmountPrivacyDescription}\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void TransactionMoneyEntryAndPaging_ExposeSemanticDescriptions()
    {
        var source = ReadContract("TransactionsPage.xaml");

        Assert.Contains("SemanticProperties.Description=\"{DynamicResource Text.TransactionAmountDescription}\"", source, StringComparison.Ordinal);
        Assert.Contains("SemanticProperties.Description=\"{DynamicResource Text.LoadMoreTransactionsDescription}\"", source, StringComparison.Ordinal);
        Assert.Contains("SemanticProperties.Description=\"{DynamicResource Text.SortHistoryDescription}\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ReconciliationStatementBalance_ExplainsMajorCurrencyUnits()
    {
        var source = ReadContract("ReconciliationPage.xaml");

        Assert.Contains("SemanticProperties.Description=\"{DynamicResource Text.ReconciliationStatementBalanceDescription}\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_ProvideChartSemanticsAndEquivalentTextOrLists()
    {
        var source = ReadContract("ReportsPage.xaml");

        Assert.Contains("SemanticProperties.Description=\"{Binding CategorySummary}\"", source, StringComparison.Ordinal);
        Assert.Contains("SemanticProperties.Description=\"{Binding IncomeExpenseSummary}\"", source, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding MonthlyNetRows}\"", source, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding YearlyNetRows}\"", source, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding CategorySummary}\"", source, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding IncomeExpenseSummary}\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Onboarding_PrivacyAndExternalSupportActionsRemainDiscoverable()
    {
        var source = ReadContract("OnboardingPage.xaml");

        Assert.Contains("SemanticProperties.Description=\"{DynamicResource Text.PrivacyDescription}\"", source, StringComparison.Ordinal);
        Assert.Contains("SemanticProperties.Description=\"{DynamicResource Text.TermsDescription}\"", source, StringComparison.Ordinal);
        Assert.Contains("SemanticProperties.Description=\"{DynamicResource Text.BuyMeCoffeeBrowserDescription}\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Settings_CriticalPrivacyControlsKeepSemanticDescriptions()
    {
        var source = ReadContract("SettingsPage.xaml");

        Assert.Contains("SemanticProperties.Description=\"Hide dashboard and report amounts\"", source, StringComparison.Ordinal);
        Assert.Contains("SemanticProperties.Description=\"Start each session with amounts masked\"", source, StringComparison.Ordinal);
        Assert.Contains("SemanticProperties.Description=\"Apply OS-level screenshot/task-preview protection where supported\"", source, StringComparison.Ordinal);
    }

    private static string ReadContract(string fileName)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", fileName));
}
