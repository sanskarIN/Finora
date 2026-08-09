using Finora.Application;

namespace Finora.App;

public sealed class LockViewModel : ViewModelBase
{
    private readonly IAppLockService _lock; private readonly IAppSettingsService _settings; private readonly IBiometricService _biometric; private string _pin = string.Empty; private string _status = "Enter your Finora PIN."; private bool _canUseBiometrics;
    public LockViewModel(IAppLockService appLock, IAppSettingsService settings, IBiometricService biometric) { _lock = appLock; _settings = settings; _biometric = biometric; UnlockCommand = new AsyncCommand(UnlockAsync); BiometricCommand = new AsyncCommand(BiometricAsync); UpdateStatus(); _ = LoadBiometricAvailabilityAsync(); }
    public string Pin { get => _pin; set => SetProperty(ref _pin, value); } public string Status { get => _status; private set => SetProperty(ref _status, value); } public bool CanUseBiometrics { get => _canUseBiometrics; private set => SetProperty(ref _canUseBiometrics, value); }
    public System.Windows.Input.ICommand UnlockCommand { get; } public System.Windows.Input.ICommand BiometricCommand { get; }
    private async Task LoadBiometricAvailabilityAsync() => CanUseBiometrics = _settings.BiometricUnlockEnabled && await _biometric.GetAvailabilityAsync() == BiometricAvailability.Available;
    private Task BiometricAsync() => RunAsync(async () => { if (!CanUseBiometrics) throw new InvalidOperationException("Biometric unlock is not available or enabled."); var result = await _biometric.AuthenticateAsync("Unlock your local Finora finance data."); if (!result.IsSuccess) { Status = result.Error ?? "Biometric verification was not completed."; return; } await Shell.Current.GoToAsync(_settings.OnboardingComplete ? "//dashboard" : "//onboarding"); });
    private Task UnlockAsync() => RunAsync(async () => { if (_lock.RemainingLockout > TimeSpan.Zero) { UpdateStatus(); return; } if (await _lock.VerifyPinAsync(Pin)) { Pin = string.Empty; await Shell.Current.GoToAsync(_settings.OnboardingComplete ? "//dashboard" : "//onboarding"); return; } Pin = string.Empty; UpdateStatus(); if (_lock.RemainingLockout == TimeSpan.Zero) Status = "Incorrect PIN. Try again."; });
    private void UpdateStatus() { var lockout = _lock.RemainingLockout; Status = lockout > TimeSpan.Zero ? $"Too many attempts. Try again in {Math.Ceiling(lockout.TotalMinutes)} minute(s)." : "Enter your Finora PIN."; }
}
