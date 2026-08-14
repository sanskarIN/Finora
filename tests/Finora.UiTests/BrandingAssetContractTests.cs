using System.Xml.Linq;

namespace Finora.UiTests;

public sealed class BrandingAssetContractTests
{
    [Fact]
    public void BuyMeACoffeeSupportArtwork_IsPackagedAsValidFinoraBrandedSvg()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Contracts", "bmc_support.svg");
        Assert.True(File.Exists(path), "The Buy Me a Coffee support artwork must be included in UI contract inputs.");

        var svg = File.ReadAllText(path);
        Assert.False(string.IsNullOrWhiteSpace(svg));
        Assert.Contains("SUPPORT FINORA", svg, StringComparison.Ordinal);
        Assert.Contains("BUY ME A COFFEE", svg, StringComparison.Ordinal);
        Assert.Contains("#102A43", svg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("#1F6F78", svg, StringComparison.OrdinalIgnoreCase);

        var document = XDocument.Parse(svg);
        Assert.Equal("svg", document.Root?.Name.LocalName);
        Assert.Equal("0 0 720 180", document.Root?.Attribute("viewBox")?.Value);
    }

    [Fact]
    public void BuyMeACoffeeSupportArtwork_RemainsConnectedToSettingsAbout()
    {
        var settings = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", "SettingsPage.xaml"));
        var about = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", "SettingsPage.About.cs"));

        Assert.Contains("bmc_support.svg", settings, StringComparison.Ordinal);
        Assert.Contains("OnBuyMeACoffeeTapped", settings, StringComparison.Ordinal);
        Assert.Contains("OpenBuyMeACoffeeAsync", about, StringComparison.Ordinal);
        Assert.Contains("AppConstants.BuyMeACoffeeUrl", about, StringComparison.Ordinal);
    }
}
