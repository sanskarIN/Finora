using System.Globalization;
using Finora.Application;

namespace Finora.App;

public sealed class LockViewModel : ViewModelBase
{
    private readonly IAppLockService _lock;
    private readonly IAppSettingsService _settings;
    private readonly IBiometricService _biometric;
    private string _pin = string.Empty;
    private string _status = string.Empty;
    private bool _canUseBiometrics;

    public LockViewModel(IAppLockService appLock, IAppSettingsService settings, IBiometricService biometric)
    {
        _lock = appLock;
        _settings = settings;
        _biometric = biometric;
        UnlockCommand = new AsyncCommand(UnlockAsync);
        BiometricCommand = new AsyncCommand(BiometricAsync);
        UpdateStatus();
        _ = LoadBiometricAvailabilityAsync();
    }

    public string Pin { get => _pin; set => SetProperty(ref _pin, value); }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public bool CanUseBiometrics { get => _canUseBiometrics; private set => SetProperty(ref _canUseBiometrics, value); }
    public System.Windows.Input.ICommand UnlockCommand { get; }
    public System.Windows.Input.ICommand BiometricCommand { get; }

    private async Task LoadBiometricAvailabilityAsync()
        => CanUseBiometrics = _settings.BiometricUnlockEnabled && await _biometric.GetAvailabilityAsync() == BiometricAvailability.Available;

    private Task BiometricAsync() => RunAsync(async () =>
    {
        if (!CanUseBiometrics) throw new InvalidOperationException(LocalizationResources.Get("BiometricUnavailable"));
        var result = await _biometric.AuthenticateAsync(LocalizationResources.Get("BiometricPrompt"));
        if (!result.IsSuccess)
        {
            Status = LocalizationResources.Get("BiometricNotCompleted");
            return;
        }

        await Shell.Current.GoToAsync(_settings.OnboardingComplete ? AppRoutes.DashboardRoot : "//onboarding");
    });

    private Task UnlockAsync() => RunAsync(async () =>
    {
        if (_lock.RemainingLockout > TimeSpan.Zero)
        {
            Pin = string.Empty;
            UpdateStatus();
            return;
        }

        if (await _lock.VerifyPinAsync(Pin))
        {
            Pin = string.Empty;
            await Shell.Current.GoToAsync(_settings.OnboardingComplete ? AppRoutes.DashboardRoot : "//onboarding");
            return;
        }

        Pin = string.Empty;
        UpdateStatus();
        if (_lock.RemainingLockout == TimeSpan.Zero) Status = LocalizationResources.Get("IncorrectPin");
    });

    private void UpdateStatus()
    {
        var lockout = _lock.RemainingLockout;
        Status = lockout > TimeSpan.Zero
            ? string.Format(CultureInfo.CurrentCulture, LocalizationResources.Get("LockoutMinutes"), Math.Ceiling(lockout.TotalMinutes))
            : LocalizationResources.Get("EnterFinoraPin");
    }
}
