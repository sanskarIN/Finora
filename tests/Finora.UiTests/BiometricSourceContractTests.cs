namespace Finora.UiTests;

public sealed class BiometricSourceContractTests
{
    [Fact]
    public void LockScreen_AlwaysKeepsMaskedPinFallback()
    {
        var xaml = ReadContract("LockPage.xaml");

        Assert.Contains("IsPassword=\"True\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Keyboard=\"Numeric\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding UnlockCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding BiometricCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding CanUseBiometrics}\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void BiometricUnlock_IsGatedByPreferenceAndNativeAvailability()
    {
        var viewModel = ReadContract("LockViewModel.cs");

        Assert.Contains("_settings.BiometricUnlockEnabled", viewModel, StringComparison.Ordinal);
        Assert.Contains("await _biometric.GetAvailabilityAsync() == BiometricAvailability.Available", viewModel, StringComparison.Ordinal);
        Assert.Contains("if (!CanUseBiometrics)", viewModel, StringComparison.Ordinal);
        Assert.Contains("if (!result.IsSuccess)", viewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void BiometricService_UsesPlatformAuthenticationAndOffersPinFallbackOnAndroid()
    {
        var service = ReadContract("PlatformBiometricService.cs");

        Assert.Contains("BiometricPrompt.Builder", service, StringComparison.Ordinal);
        Assert.Contains("SetNegativeButton(\"Use PIN\"", service, StringComparison.Ordinal);
        Assert.Contains("LAPolicy.DeviceOwnerAuthenticationWithBiometrics", service, StringComparison.Ordinal);
        Assert.Contains("UserConsentVerifier.RequestVerificationAsync", service, StringComparison.Ordinal);
        Assert.Contains("Biometric unlock is unsupported on this platform", service, StringComparison.Ordinal);
    }

    private static string ReadContract(string fileName)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", fileName));
}
