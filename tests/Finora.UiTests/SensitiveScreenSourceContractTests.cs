namespace Finora.UiTests;

public sealed class SensitiveScreenSourceContractTests
{
    [Fact]
    public void SensitiveScreen_UsesNativeAndroidAndWindowsProtectionApis()
    {
        var service = ReadContract("SensitiveScreenService.cs");

        Assert.Contains("WindowManagerFlags.Secure", service, StringComparison.Ordinal);
        Assert.Contains("SetWindowDisplayAffinity", service, StringComparison.Ordinal);
        Assert.Contains("WdaExcludeFromCapture", service, StringComparison.Ordinal);
        Assert.Contains("#if ANDROID || WINDOWS", service, StringComparison.Ordinal);
    }

    [Fact]
    public void SensitiveScreen_DoesNotOverclaimUnsupportedPlatforms()
    {
        var service = ReadContract("SensitiveScreenService.cs");

        Assert.Contains("return false;", service, StringComparison.Ordinal);
        Assert.Contains("does not provide a supported API that can reliably block screenshots", service, StringComparison.Ordinal);
        Assert.Contains("avoid claiming otherwise", service, StringComparison.Ordinal);
    }

    [Fact]
    public void SensitiveScreen_IsReappliedAtStartupAndActivation()
    {
        var app = ReadContract("App.xaml.cs");

        Assert.True(CountOccurrences(app, "_settings.SensitiveScreenProtectionEnabled") >= 2);
        Assert.True(CountOccurrences(app, "_sensitiveScreen.SetProtectionAsync(true)") >= 2);
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
