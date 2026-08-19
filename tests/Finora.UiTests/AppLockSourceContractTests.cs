namespace Finora.UiTests;

public sealed class AppLockSourceContractTests
{
    [Fact]
    public void AppLock_PinVerifierUsesHardenedLocalStorageAndComparison()
    {
        var services = ReadContract("Services.cs");

        Assert.Contains("private const int Pbkdf2Iterations = 150_000;", services, StringComparison.Ordinal);
        Assert.Contains("Rfc2898DeriveBytes.Pbkdf2", services, StringComparison.Ordinal);
        Assert.Contains("HashAlgorithmName.SHA256", services, StringComparison.Ordinal);
        Assert.Contains("SecureStorage.Default.SetAsync(SaltKey", services, StringComparison.Ordinal);
        Assert.Contains("SecureStorage.Default.SetAsync(HashKey", services, StringComparison.Ordinal);
        Assert.Contains("CryptographicOperations.FixedTimeEquals", services, StringComparison.Ordinal);
        Assert.Contains("CryptographicOperations.ZeroMemory", services, StringComparison.Ordinal);
        Assert.Contains("character is >= '0' and <= '9'", services, StringComparison.Ordinal);
    }

    [Fact]
    public void AppLock_SecureStorageFailureDoesNotSilentlyDisableAnEnabledLock()
    {
        var services = ReadContract("Services.cs");

        Assert.Contains("return enabledPreference;", services, StringComparison.Ordinal);
        Assert.Contains("fail closed", services, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Preferences.Set(EnabledKey, true);", services, StringComparison.Ordinal);
    }

    [Fact]
    public void AppLifecycle_RoutesToLockAndRelocksAfterConfiguredInactivity()
    {
        var app = ReadContract("App.xaml.cs");

        Assert.Contains("await _appLock.HasPinAsync()", app, StringComparison.Ordinal);
        Assert.Contains("await shell.GoToAsync(\"//lock\")", app, StringComparison.Ordinal);
        Assert.Contains("window.Deactivated", app, StringComparison.Ordinal);
        Assert.Contains("window.Activated", app, StringComparison.Ordinal);
        Assert.Contains("TimeSpan.FromMinutes(_settings.AutoLockMinutes)", app, StringComparison.Ordinal);
        Assert.Contains("await Shell.Current.GoToAsync(\"//lock\")", app, StringComparison.Ordinal);
    }

    private static string ReadContract(string fileName)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", fileName));
}
