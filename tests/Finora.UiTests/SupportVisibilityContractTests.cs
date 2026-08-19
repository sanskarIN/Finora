using System.Xml.Linq;

namespace Finora.UiTests;

public sealed class SupportVisibilityContractTests
{
    [Fact]
    public void BuyMeACoffeeBranding_RemainsVisibleAcrossPrimaryDiscoverySurfaces()
    {
        var shell = ReadContract("AppShell.xaml");
        var onboarding = ReadContract("OnboardingPage.xaml");
        var settings = ReadContract("SettingsPage.xaml");
        var english = ReadResx("AppResources.resx");

        Assert.Contains("bmc_support.svg", shell, StringComparison.Ordinal);
        Assert.Contains("bmc_support.svg", onboarding, StringComparison.Ordinal);
        Assert.Contains("bmc_support.svg", settings, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource Text.BuyMeCoffeeButton}", onboarding, StringComparison.Ordinal);
        Assert.Contains("Buy Me a Coffee", english["BuyMeCoffeeButton"], StringComparison.Ordinal);
        Assert.Contains("Buy Me a Coffee", settings, StringComparison.Ordinal);
    }

    [Fact]
    public void OnboardingSupportAction_UsesCentralBmcUrlAndKeepsContributionOptional()
    {
        var onboarding = ReadContract("OnboardingPage.xaml");
        var links = ReadContract("OnboardingPage.Links.cs");
        var english = ReadResx("AppResources.resx");
        var hindi = ReadResx("AppResources.hi.resx");

        Assert.Contains("OnOnboardingBuyMeACoffeeClicked", onboarding, StringComparison.Ordinal);
        Assert.Contains("AppConstants.BuyMeACoffeeUrl", links, StringComparison.Ordinal);
        Assert.Contains("optional external", english["SupportFinoraBody"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("never unlocks app features", english["SupportFinoraBody"], StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(hindi["SupportFinoraBody"]));
    }

    [Fact]
    public void AdaptiveFlyoutSupportArtwork_IsActionableAndUsesCanonicalBmcUrl()
    {
        var shell = ReadContract("AppShell.xaml");
        var shellCodeBehind = ReadContract("AppShell.xaml.cs");

        Assert.Contains("OnShellBuyMeACoffeeTapped", shell, StringComparison.Ordinal);
        Assert.Contains("AppConstants.BuyMeACoffeeUrl", shellCodeBehind, StringComparison.Ordinal);
        Assert.Contains("Launcher.Default.OpenAsync", shellCodeBehind, StringComparison.Ordinal);
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