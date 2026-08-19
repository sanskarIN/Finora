namespace Finora.UiTests;

public sealed class LocalizedSurfaceContractTests
{
    [Theory]
    [InlineData("AccountDetailPage.xaml", "Text.AccountDetails")]
    [InlineData("BudgetsPage.xaml", "Text.Budgets")]
    [InlineData("SavingsPage.xaml", "Text.SavingsGoals")]
    [InlineData("RecurringPage.xaml", "Text.RecurringItems")]
    [InlineData("TransactionDetailPage.xaml", "Text.TransactionDetailTitle")]
    [InlineData("TransactionToolsPage.xaml", "Text.TransactionToolsTitle")]
    [InlineData("ReconciliationPage.xaml", "Text.ReconciliationTitle")]
    [InlineData("ReportsPage.xaml", "Text.Reports")]
    public void MigratedFinanceSurfaces_UseDynamicLocalizedHeadings(string fileName, string key)
    {
        var source = ReadContract(fileName);

        Assert.Contains($"{{DynamicResource {key}}}", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("AccountDetailPage.xaml")]
    [InlineData("BudgetsPage.xaml")]
    [InlineData("SavingsPage.xaml")]
    [InlineData("RecurringPage.xaml")]
    [InlineData("TransactionDetailPage.xaml")]
    [InlineData("TransactionToolsPage.xaml")]
    [InlineData("ReconciliationPage.xaml")]
    [InlineData("ReportsPage.xaml")]
    public void MigratedFinanceSurfaces_DoNotUseStaticEnglishPageTitles(string fileName)
    {
        var source = ReadContract(fileName);

        Assert.DoesNotContain("Title=\"Reports\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Title=\"Budgets\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Title=\"Savings goals\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Title=\"Recurring\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Title=\"Transaction details\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Title=\"Transaction tools\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Title=\"Reconcile account\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Title=\"Account details\"", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("SavingsViewModel.cs", "Savings planning")]
    [InlineData("TransactionDetailViewModel.cs", "Transaction detail")]
    [InlineData("TransactionToolsViewModel.cs", "Transaction tools")]
    [InlineData("ReconciliationViewModel.cs", "Reconciliation")]
    [InlineData("ReportsViewModel.cs", "Reports")]
    public void MigratedViewModels_ResolveUserVisibleWorkflowCopyThroughLocalization(string fileName, string feature)
    {
        var source = ReadContract(fileName);

        Assert.True(
            source.Contains("LocalizationResources.Get(", StringComparison.Ordinal),
            $"{feature} should resolve user-visible workflow copy through LocalizationResources.");
    }

    private static string ReadContract(string fileName)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", fileName));
}
