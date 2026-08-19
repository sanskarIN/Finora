namespace Finora.UiTests;

public sealed class LocalPremiumSourceContractTests
{
    [Fact]
    public void LocalPremium_RemainsAnExplicitHiddenDeveloperDemoFlag()
    {
        var xaml = ReadContract("SettingsPage.xaml");

        Assert.Contains("x:Name=\"DeveloperPanel\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"False\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Local premium demo flag", xaml, StringComparison.Ordinal);
        Assert.Contains("IsToggled=\"{Binding LocalPremiumDemoEnabled}\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void LocalPremium_IsStoredOnlyAsLocalPreferenceState()
    {
        var settings = ReadContract("Services.cs");
        var viewModel = ReadContract("SettingsViewModel.cs");

        Assert.Contains("Preferences.Get(nameof(LocalPremiumDemoEnabled), false)", settings, StringComparison.Ordinal);
        Assert.Contains("Preferences.Set(nameof(LocalPremiumDemoEnabled), value)", settings, StringComparison.Ordinal);
        Assert.Contains("_settings.LocalPremiumDemoEnabled = value", viewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void LocalPremium_AndSupportCopyRejectCommercialEntitlementClaims()
    {
        var xaml = ReadContract("SettingsPage.xaml");

        Assert.Contains("not tamper-proof commercial licensing", xaml, StringComparison.Ordinal);
        Assert.Contains("future store/server integration is required for reliable paid entitlement validation", xaml, StringComparison.Ordinal);
        Assert.Contains("Buy Me a Coffee support is optional and does not unlock Finora features", xaml, StringComparison.Ordinal);
    }

    private static string ReadContract(string fileName)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", fileName));
}
