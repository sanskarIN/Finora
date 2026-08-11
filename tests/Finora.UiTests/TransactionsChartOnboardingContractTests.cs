namespace Finora.UiTests;

public sealed class TransactionsChartOnboardingContractTests
{
    [Fact]
    public void TransactionHistory_ExposesSortAndBoundedLoadMoreBehavior()
    {
        var xaml = ReadContract("TransactionsPage.xaml");
        var viewModel = ReadContract("TransactionsViewModel.cs");

        Assert.Contains("ItemsSource=\"{Binding SortOrders}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedItem=\"{Binding SortOrder}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding LoadMoreCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding HasMore}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("const int PageSize = 50", viewModel, StringComparison.Ordinal);
        Assert.Contains("LocalDateRange.ToUtc", viewModel, StringComparison.Ordinal);
        Assert.Contains("Take(PageSize)", viewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportChart_UsesSignedZeroBaselineInsteadOfAbsoluteMagnitudeBars()
    {
        var source = ReadContract("ReportBarChartView.cs");

        Assert.Contains("Math.Min(0L", source, StringComparison.Ordinal);
        Assert.Contains("Math.Max(0L", source, StringComparison.Ordinal);
        Assert.Contains("zeroY", source, StringComparison.Ordinal);
        Assert.Contains("item.ValueMinor / span", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Math.Abs(item.ValueMinor)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Onboarding_ExposesPrivacyTermsAndRevisitGuidance()
    {
        var xaml = ReadContract("OnboardingPage.xaml");
        var links = ReadContract("OnboardingPage.Links.cs");

        Assert.Contains("Nothing is uploaded automatically", xaml, StringComparison.Ordinal);
        Assert.Contains("revisit onboarding from Settings", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Clicked=\"OnOnboardingPrivacyClicked\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Clicked=\"OnOnboardingTermsClicked\"", xaml, StringComparison.Ordinal);
        Assert.Contains("document=terms", links, StringComparison.Ordinal);
    }

    private static string ReadContract(string fileName)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", fileName));
}
