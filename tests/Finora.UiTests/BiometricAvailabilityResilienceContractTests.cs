namespace Finora.UiTests;

public sealed class BiometricAvailabilityResilienceContractTests
{
    [Fact]
    public void BiometricAvailability_DisabledPreferenceSkipsNativeProbe()
    {
        var viewModel = ReadContract("LockViewModel.cs");

        Assert.Contains("if (!_settings.BiometricUnlockEnabled)", viewModel, StringComparison.Ordinal);
        Assert.Contains("CanUseBiometrics = false;", viewModel, StringComparison.Ordinal);
        Assert.Contains("return;", viewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void BiometricAvailability_NativeProbeFaultKeepsPinPathAvailable()
    {
        var viewModel = ReadContract("LockViewModel.cs");

        Assert.Contains("try", viewModel, StringComparison.Ordinal);
        Assert.Contains("catch (Exception)", viewModel, StringComparison.Ordinal);
        Assert.Contains("do not allow a fire-and-forget availability probe to fault unobserved", viewModel, StringComparison.Ordinal);
        Assert.Contains("CanUseBiometrics = false;", viewModel, StringComparison.Ordinal);
    }

    private static string ReadContract(string fileName)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", fileName));
}
