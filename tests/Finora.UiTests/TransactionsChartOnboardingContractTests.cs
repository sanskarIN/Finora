using System.Xml.Linq;

namespace Finora.UiTests;

public sealed class TransactionsChartOnboardingContractTests
{
    [Fact]
    public void TransactionHistory_ExposesSortAndDatabaseBackedLoadMoreBehavior()
    {
        var xaml = ReadContract("TransactionsPage.xaml");
        var viewModel = ReadContract("TransactionsViewModel.cs");

        Assert.Contains("ItemsSource=\"{Binding SortOrders}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedItem=\"{Binding SortOrder}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding LoadMoreCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding HasMore}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("const int PageSize = 50", viewModel, StringComparison.Ordinal);
        Assert.Contains("LocalDateRange.ToUtc", viewModel, StringComparison.Ordinal);
        Assert.Contains("TransactionHistoryQuery", viewModel, StringComparison.Ordinal);
        Assert.Contains("GetPageAsync", viewModel, StringComparison.Ordinal);
        Assert.Contains("Offset = Transactions.Count", viewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("_allMatches", viewModel, StringComparison.Ordinal);
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
    public void Onboarding_ExposesLocalizedPrivacyTermsAndRevisitGuidance()
    {
        var xaml = ReadContract("OnboardingPage.xaml");
        var links = ReadContract("OnboardingPage.Links.cs");
        var english = ReadResx("AppResources.resx");
        var hindi = ReadResx("AppResources.hi.resx");

        Assert.Contains("{DynamicResource Text.LocalFirstPrivacyBody}", xaml, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource Text.OnboardingSharingNotice}", xaml, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource Text.Privacy}", xaml, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource Text.Terms}", xaml, StringComparison.Ordinal);
        Assert.Contains("Nothing is uploaded automatically", english["LocalFirstPrivacyBody"], StringComparison.Ordinal);
        Assert.Contains("revisit onboarding from Settings", english["OnboardingSharingNotice"], StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(hindi["LocalFirstPrivacyBody"]));
        Assert.False(string.IsNullOrWhiteSpace(hindi["OnboardingSharingNotice"]));
        Assert.Contains("Clicked=\"OnOnboardingPrivacyClicked\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Clicked=\"OnOnboardingTermsClicked\"", xaml, StringComparison.Ordinal);
        Assert.Contains("document=terms", links, StringComparison.Ordinal);
    }

    private static string ReadContract(string fileName)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", fileName));

    private static IReadOnlyDictionary<string, string> ReadResx(string fileName)
        => XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Contracts", fileName))
            .Root!
            .Elements("data")
            .ToDictionary(
                element => (string)element.Attribute("name")!,
                element => (string?)element.Element("value") ?? string.Empty,
                StringComparer.Ordinal);
}