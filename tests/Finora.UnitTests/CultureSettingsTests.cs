using System.Globalization;
using Finora.Shared;

namespace Finora.UnitTests;

[Collection(CultureTestCollection.Name)]
public sealed class CultureSettingsTests
{
    [Theory]
    [InlineData("en-IN", "en-IN")]
    [InlineData("hi-IN", "hi-IN")]
    [InlineData("en-US", "en-US")]
    public void NormalizeOrFallback_ReturnsCanonicalCultureName(string input, string expected)
    {
        Assert.Equal(expected, CultureSettings.NormalizeOrFallback(input, "en-US"));
    }

    [Fact]
    public void NormalizeOrFallback_InvalidLocale_UsesValidFallback()
    {
        Assert.Equal("en-IN", CultureSettings.NormalizeOrFallback("not-a-real-locale-xyz", "en-IN"));
    }

    [Fact]
    public void TryResolve_InvalidLocale_ReturnsFalse()
    {
        Assert.False(CultureSettings.TryResolve("not-a-real-locale-xyz", out _));
    }

    [Fact]
    public void TryApply_ChangesCurrentFormattingCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        var originalDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        var originalDefaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;

        try
        {
            Assert.True(CultureSettings.TryApply("en-IN"));
            Assert.Equal("en-IN", CultureInfo.CurrentCulture.Name);
            Assert.Equal("en-IN", CultureInfo.CurrentUICulture.Name);
        }
        finally
        {
            CultureInfo.DefaultThreadCurrentCulture = originalDefaultCulture;
            CultureInfo.DefaultThreadCurrentUICulture = originalDefaultUiCulture;
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
