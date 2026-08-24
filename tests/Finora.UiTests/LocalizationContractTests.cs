using System.Xml.Linq;

namespace Finora.UiTests;

public sealed class LocalizationContractTests
{
    [Fact]
    public void EveryNeutralResourceBundle_HasMatchingHindiKeySet()
    {
        var contracts = Path.Combine(AppContext.BaseDirectory, "Contracts");
        var neutralFiles = Directory.GetFiles(contracts, "*Resources.resx", SearchOption.TopDirectoryOnly);

        Assert.NotEmpty(neutralFiles);
        foreach (var neutralPath in neutralFiles)
        {
            var hindiPath = Path.Combine(
                contracts,
                Path.GetFileNameWithoutExtension(neutralPath) + ".hi.resx");

            Assert.True(File.Exists(hindiPath), $"Missing Hindi resource bundle for {Path.GetFileName(neutralPath)}.");
            var neutral = ReadKeys(neutralPath);
            var hindi = ReadKeys(hindiPath);
            Assert.NotEmpty(neutral);
            Assert.Equal(neutral.Order(StringComparer.Ordinal), hindi.Order(StringComparer.Ordinal));
        }
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

    [Fact]
    public void LocalizedFormatting_CachesCompositeTemplates_AndLockoutUsesSharedFormatter()
    {
        var localization = ReadContract("LocalizationResources.cs");
        var lockViewModel = ReadContract("LockViewModel.cs");

        Assert.Contains("CompositeFormat", localization, StringComparison.Ordinal);
        Assert.Contains("FormatCache.GetOrAdd", localization, StringComparison.Ordinal);
        Assert.Contains(
            "LocalizationResources.Format(\"LockoutMinutes\"",
            lockViewModel,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "string.Format(CultureInfo.CurrentCulture, LocalizationResources.Get(\"LockoutMinutes\")",
            lockViewModel,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("AppShell.xaml", "Text.Dashboard")]
    [InlineData("DashboardPage.xaml", "Text.Dashboard")]
    [InlineData("TransactionsPage.xaml", "Text.Transactions")]
    [InlineData("AccountsPage.xaml", "Text.Accounts")]
    [InlineData("BudgetsPage.xaml", "Text.Budgets")]
    [InlineData("SavingsPage.xaml", "Text.SavingsGoals")]
    [InlineData("RecurringPage.xaml", "Text.RecurringItems")]
    [InlineData("OnboardingPage.xaml", "Text.WelcomeToFinora")]
    public void PrimaryLocalizedSurfaces_UseDynamicResources(string fileName, string resourceKey)
    {
        var source = ReadContract(fileName);
        Assert.Contains($"{{DynamicResource {resourceKey}}}", source, StringComparison.Ordinal);
    }

    private static HashSet<string> ReadKeys(string path)
        => XDocument.Load(path)
            .Root!
            .Elements("data")
            .Select(element => (string)element.Attribute("name")!)
            .ToHashSet(StringComparer.Ordinal);

    private static string ReadContract(string fileName)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", fileName));
}