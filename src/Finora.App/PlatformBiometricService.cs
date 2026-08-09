using Finora.Application;
using Finora.Shared;

#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using Microsoft.Maui.ApplicationModel;
#elif IOS || MACCATALYST
using LocalAuthentication;
#elif WINDOWS
using Windows.Security.Credentials.UI;
#endif

namespace Finora.App;

public sealed class PlatformBiometricService : IBiometricService
{
    public Task<BiometricAvailability> GetAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
#if ANDROID
        if (Build.VERSION.SdkInt < BuildVersionCodes.P) return Task.FromResult(BiometricAvailability.Unsupported);
        var activity = Platform.CurrentActivity; if (activity is null) return Task.FromResult(BiometricAvailability.NotAvailable);
        var keyguard = (KeyguardManager?)activity.GetSystemService(Context.KeyguardService);
        if (keyguard?.IsDeviceSecure != true) return Task.FromResult(BiometricAvailability.NotEnrolled);
        return Task.FromResult(BiometricAvailability.Available);
#elif IOS || MACCATALYST
        using var context = new LAContext();
        return Task.FromResult(context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out _) ? BiometricAvailability.Available : BiometricAvailability.NotEnrolled);
#elif WINDOWS
        return GetWindowsAvailabilityAsync(cancellationToken);
#else
        return Task.FromResult(BiometricAvailability.Unsupported);
#endif
    }

    public async Task<Result> AuthenticateAsync(string reason, CancellationToken cancellationToken = default)
    {
        reason = string.IsNullOrWhiteSpace(reason) ? "Confirm your identity to unlock Finora." : reason.Trim();
#if ANDROID
        if (Build.VERSION.SdkInt < BuildVersionCodes.P) return Result.Failure("Biometric unlock requires Android 9 or later on this build.");
        var activity = Platform.CurrentActivity; if (activity is null) return Result.Failure("The Android activity is unavailable.");
        var tcs = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var signal = new CancellationSignal(); using var registration = cancellationToken.Register(signal.Cancel);
        var callback = new AndroidAuthenticationCallback(tcs); var negative = new AndroidNegativeButtonListener(tcs);
        try
        {
            var prompt = new BiometricPrompt.Builder(activity).SetTitle("Unlock Finora").SetSubtitle(reason).SetNegativeButton("Use PIN", activity.MainExecutor, negative).Build();
            prompt.Authenticate(signal, activity.MainExecutor, callback); return await tcs.Task.ConfigureAwait(false);
        }
        catch (Exception) { return Result.Failure("Biometric authentication is unavailable on this device."); }
#elif IOS || MACCATALYST
        using var context = new LAContext();
        if (!context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out _)) return Result.Failure("Biometric authentication is not enrolled or available.");
        try { var success = await context.EvaluatePolicyAsync(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, reason).ConfigureAwait(false); cancellationToken.ThrowIfCancellationRequested(); return success ? Result.Success() : Result.Failure("Biometric authentication was not completed."); }
        catch (Exception) { return Result.Failure("Biometric authentication was not completed."); }
#elif WINDOWS
        try
        {
            var availability = await UserConsentVerifier.CheckAvailabilityAsync(); if (availability != UserConsentVerifierAvailability.Available) return Result.Failure("Windows Hello is not available or configured.");
            var result = await UserConsentVerifier.RequestVerificationAsync(reason); cancellationToken.ThrowIfCancellationRequested(); return result == UserConsentVerificationResult.Verified ? Result.Success() : Result.Failure("Windows Hello verification was not completed.");
        }
        catch (Exception) { return Result.Failure("Windows Hello verification is unavailable."); }
#else
        await Task.CompletedTask; return Result.Failure("Biometric unlock is unsupported on this platform.");
#endif
    }

#if ANDROID
    private sealed class AndroidAuthenticationCallback(TaskCompletionSource<Result> tcs) : BiometricPrompt.AuthenticationCallback
    {
        private readonly TaskCompletionSource<Result> _tcs = tcs;
        public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult? result) => _tcs.TrySetResult(Result.Success());
        public override void OnAuthenticationError([Android.Runtime.GeneratedEnum] BiometricErrorCode errorCode, Java.Lang.ICharSequence? errString) => _tcs.TrySetResult(Result.Failure(errString?.ToString() ?? "Biometric authentication failed."));
        public override void OnAuthenticationFailed() { }
    }
    private sealed class AndroidNegativeButtonListener(TaskCompletionSource<Result> tcs) : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        private readonly TaskCompletionSource<Result> _tcs = tcs;
        public void OnClick(IDialogInterface? dialog, int which) => _tcs.TrySetResult(Result.Failure("Use the Finora PIN instead."));
    }
#elif WINDOWS
    private static async Task<BiometricAvailability> GetWindowsAvailabilityAsync(CancellationToken cancellationToken)
    {
        var availability = await UserConsentVerifier.CheckAvailabilityAsync(); cancellationToken.ThrowIfCancellationRequested();
        return availability switch { UserConsentVerifierAvailability.Available => BiometricAvailability.Available, UserConsentVerifierAvailability.DeviceNotPresent => BiometricAvailability.NotAvailable, UserConsentVerifierAvailability.NotConfiguredForUser => BiometricAvailability.NotEnrolled, _ => BiometricAvailability.NotAvailable };
    }
#endif
}
