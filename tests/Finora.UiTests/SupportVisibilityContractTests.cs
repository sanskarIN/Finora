namespace Finora.UiTests;

public sealed class SupportVisibilityContractTests
{
    [Fact]
    public void BuyMeACoffeeBranding_RemainsVisibleAcrossPrimaryDiscoverySurfaces()
    {
        var shell = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", "AppShell.xaml"));
        var onboarding = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", "OnboardingPage.xaml"));
        var settings = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", "SettingsPage.xaml"));

        Assert.Contains("bmc_support.svg", shell, StringComparison.Ordinal);
        Assert.Contains("bmc_support.svg", onboarding, StringComparison.Ordinal);
        Assert.Contains("bmc_support.svg", settings, StringComparison.Ordinal);
        Assert.Contains("Buy Me a Coffee", onboarding, StringComparison.Ordinal);
        Assert.Contains("Buy Me a Coffee", settings, StringComparison.Ordinal);
    }

    [Fact]
    public void OnboardingSupportAction_UsesCentralBmcUrlAndKeepsContributionOptional()
    {
        var onboarding = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", "OnboardingPage.xaml"));
        var links = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", "OnboardingPage.Links.cs"));

        Assert.Contains("OnOnboardingBuyMeACoffeeClicked", onboarding, StringComparison.Ordinal);
        Assert.Contains("AppConstants.BuyMeACoffeeUrl", links, StringComparison.Ordinal);
        Assert.Contains("optional external", onboarding, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("never unlocks app features", onboarding, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AdaptiveFlyoutSupportArtwork_IsActionableAndUsesCanonicalBmcUrl()
    {
        var shell = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", "AppShell.xaml"));
        var shellCodeBehind = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", "AppShell.xaml.cs"));

        Assert.Contains("OnShellBuyMeACoffeeTapped", shell, StringComparison.Ordinal);
        Assert.Contains("AppConstants.BuyMeACoffeeUrl", shellCodeBehind, StringComparison.Ordinal);
        Assert.Contains("Launcher.Default.OpenAsync", shellCodeBehind, StringComparison.Ordinal);
    }
}
