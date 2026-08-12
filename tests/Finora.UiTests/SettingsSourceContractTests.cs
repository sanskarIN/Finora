namespace Finora.UiTests;

public sealed class SettingsSourceContractTests
{
    [Fact]
    public void Settings_ExposesRequiredMasterPromptControlsAndIdentity()
    {
        var xaml = ReadContract("SettingsPage.xaml");

        string[] requiredText =
        [
            "Revisit onboarding",
            "Default currency",
            "Financial month start day",
            "Privacy mode",
            "Hide amounts on launch",
            "Default account",
            "Default transaction type",
            "Weekly backup reminder",
            "Preferred receipt image quality",
            "Export sanitized diagnostic log",
            "Delete all local finance data",
            "Made by the Sanskar",
            ".NET MAUI · C# · XAML · SQLite · MVVM",
            "Support development · Buy Me a Coffee",
            "sanskarin@outlook.in",
            "supportramsandesh@gmail.com",
            "Apache-2.0",
            "Contributing",
            "Security",
            "Support guide"
        ];

        foreach (var value in requiredText) Assert.Contains(value, xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding AppVersion}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Clicked=\"OnDeleteAllFinanceDataClicked\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Clicked=\"OnDeleteAllClicked\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Clicked=\"OnOnboardingClicked\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Clicked=\"OnBuyMeACoffeeClicked\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Settings_AboutVersionComesFromPackagedMetadata()
    {
        var viewModel = ReadContract("SettingsViewModel.cs");

        Assert.Contains("AppInfo.Current.VersionString", viewModel, StringComparison.Ordinal);
        Assert.Contains("AppInfo.Current.BuildString", viewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void Settings_SecretInputsRemainMasked()
    {
        var xaml = ReadContract("SettingsPage.xaml");

        Assert.Contains("x:Name=\"BackupPasswordEntry\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"NewPinEntry\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ConfirmPinEntry\"", xaml, StringComparison.Ordinal);
        Assert.Equal(3, CountOccurrences(xaml, "IsPassword=\"True\""));
    }

    [Fact]
    public void Settings_DestructiveAndSecurityHandlersUseHardenedPartialImplementations()
    {
        var reset = ReadContract("SettingsPage.Reset.cs");
        var security = ReadContract("SettingsPage.Security.cs");
        var about = ReadContract("SettingsPage.About.cs");

        Assert.Contains("DeleteAllFinanceDataAsync", reset, StringComparison.Ordinal);
        Assert.Contains("Type DELETE", reset, StringComparison.Ordinal);
        Assert.Contains("ClearPinAsync", security, StringComparison.Ordinal);
        Assert.Contains("Settings.PinRemovalFailed", security, StringComparison.Ordinal);
        Assert.Contains("AppConstants.BuyMeACoffeeUrl", about, StringComparison.Ordinal);
        Assert.Contains("Settings.BuyMeACoffeeOpenFailed", about, StringComparison.Ordinal);
        Assert.Contains("CONTRIBUTING.md", about, StringComparison.Ordinal);
        Assert.Contains("SECURITY.md", about, StringComparison.Ordinal);
        Assert.Contains("SUPPORT.md", about, StringComparison.Ordinal);
    }

    private static string ReadContract(string fileName)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", fileName));

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }
}
