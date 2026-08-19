using System.Xml.Linq;

namespace Finora.UiTests;

public sealed class LocalizationContractTests
{
    [Theory]
    [InlineData("AppResources.resx", "AppResources.hi.resx")]
    [InlineData("TransactionsResources.resx", "TransactionsResources.hi.resx")]
    public void HindiResourceBundles_MatchNeutralKeySets(string neutralFile, string hindiFile)
    {
        var neutral = ReadKeys(neutralFile);
        var hindi = ReadKeys(hindiFile);

        Assert.NotEmpty(neutral);
        Assert.Equal(neutral.Order(StringComparer.Ordinal), hindi.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void AppProject_DeclaresNeutralLanguageAndRuntimeResourceRefresh()
    {
        var project = ReadContract("Finora.App.csproj");
        var localization = ReadContract("LocalizationResources.cs");
        var settings = ReadContract("SettingsViewModel.cs");

        Assert.Contains("<NeutralLanguage>en-US</NeutralLanguage>", project, StringComparison.Ordinal);
        Assert.Contains("GetManifestResourceNames", localization, StringComparison.Ordinal);
        Assert.Contains("Duplicate Finora localization key", localization, StringComparison.Ordinal);
        Assert.Contains("LocalizationResources.Apply(resources)", settings, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("AppShell.xaml", "Text.Dashboard")]
    [InlineData("DashboardPage.xaml", "Text.Dashboard")]
    [InlineData("TransactionsPage.xaml", "Text.Transactions")]
    [InlineData("AccountsPage.xaml", "Text.Accounts")]
    [InlineData("OnboardingPage.xaml", "Text.WelcomeToFinora")]
    public void PrimaryLocalizedSurfaces_UseDynamicResources(string fileName, string resourceKey)
    {
        var source = ReadContract(fileName);
        Assert.Contains($"{{DynamicResource {resourceKey}}}", source, StringComparison.Ordinal);
    }

    private static HashSet<string> ReadKeys(string fileName)
        => XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Contracts", fileName))
            .Root!
            .Elements("data")
            .Select(element => (string)element.Attribute("name")!)
            .ToHashSet(StringComparer.Ordinal);

    private static string ReadContract(string fileName)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", fileName));
}