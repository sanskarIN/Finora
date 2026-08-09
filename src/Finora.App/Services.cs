using System.Security.Cryptography;
using Finora.Application;
using Finora.Domain;
using Finora.Shared;

namespace Finora.App;

public sealed class MauiAppSettingsService : IAppSettingsService
{
    public string DefaultCurrency { get => Preferences.Get(nameof(DefaultCurrency), "INR"); set => Preferences.Set(nameof(DefaultCurrency), value); }
    public string Locale { get => Preferences.Get(nameof(Locale), System.Globalization.CultureInfo.CurrentCulture.Name); set => Preferences.Set(nameof(Locale), value); }
    public int FinancialMonthStartDay { get => Preferences.Get(nameof(FinancialMonthStartDay), 1); set => Preferences.Set(nameof(FinancialMonthStartDay), Math.Clamp(value, 1, 28)); }
    public bool PrivacyMode { get => Preferences.Get(nameof(PrivacyMode), false); set => Preferences.Set(nameof(PrivacyMode), value); }
    public bool HideAmountsOnLaunch { get => Preferences.Get(nameof(HideAmountsOnLaunch), false); set => Preferences.Set(nameof(HideAmountsOnLaunch), value); }
    public ThemePreference Theme { get => Enum.TryParse<ThemePreference>(Preferences.Get(nameof(Theme), ThemePreference.System.ToString()), out var value) ? value : ThemePreference.System; set => Preferences.Set(nameof(Theme), value.ToString()); }
    public bool ReducedMotion { get => Preferences.Get(nameof(ReducedMotion), false); set => Preferences.Set(nameof(ReducedMotion), value); }
    public bool BackupRemindersEnabled { get => Preferences.Get(nameof(BackupRemindersEnabled), true); set => Preferences.Set(nameof(BackupRemindersEnabled), value); }
    public bool OnboardingComplete { get => Preferences.Get(nameof(OnboardingComplete), false); set => Preferences.Set(nameof(OnboardingComplete), value); }
    public int AutoLockMinutes { get => Preferences.Get(nameof(AutoLockMinutes), 5); set => Preferences.Set(nameof(AutoLockMinutes), Math.Clamp(value, 1, 60)); }
    public bool LocalPremiumDemoEnabled { get => Preferences.Get(nameof(LocalPremiumDemoEnabled), false); set => Preferences.Set(nameof(LocalPremiumDemoEnabled), value); }
    public bool NotificationsEnabled { get => Preferences.Get(nameof(NotificationsEnabled), false); set => Preferences.Set(nameof(NotificationsEnabled), value); }
    public bool BiometricUnlockEnabled { get => Preferences.Get(nameof(BiometricUnlockEnabled), false); set => Preferences.Set(nameof(BiometricUnlockEnabled), value); }
    public bool SensitiveScreenProtectionEnabled { get => Preferences.Get(nameof(SensitiveScreenProtectionEnabled), true); set => Preferences.Set(nameof(SensitiveScreenProtectionEnabled), value); }
    public int ReceiptImageQuality { get => Preferences.Get(nameof(ReceiptImageQuality), 85); set => Preferences.Set(nameof(ReceiptImageQuality), Math.Clamp(value, 40, 100)); }
    public bool LargerInterface { get => Preferences.Get(nameof(LargerInterface), false); set => Preferences.Set(nameof(LargerInterface), value); }
    public Guid? DefaultAccountId { get => Guid.TryParse(Preferences.Get(nameof(DefaultAccountId), string.Empty), out var value) ? value : null; set { if (value is Guid id) Preferences.Set(nameof(DefaultAccountId), id.ToString()); else Preferences.Remove(nameof(DefaultAccountId)); } }
    public TransactionType DefaultTransactionType { get => Enum.TryParse<TransactionType>(Preferences.Get(nameof(DefaultTransactionType), TransactionType.Expense.ToString()), out var value) ? value : TransactionType.Expense; set => Preferences.Set(nameof(DefaultTransactionType), value.ToString()); }
    public DateTimeOffset? LastBackupAtUtc { get { var raw = Preferences.Get(nameof(LastBackupAtUtc), 0L); return raw > 0 ? DateTimeOffset.FromUnixTimeSeconds(raw) : null; } set { if (value is DateTimeOffset date) Preferences.Set(nameof(LastBackupAtUtc), date.ToUnixTimeSeconds()); else Preferences.Remove(nameof(LastBackupAtUtc)); } }
    public bool DashboardShowBalance { get => Preferences.Get(nameof(DashboardShowBalance), true); set => Preferences.Set(nameof(DashboardShowBalance), value); }
    public bool DashboardShowIncomeExpense { get => Preferences.Get(nameof(DashboardShowIncomeExpense), true); set => Preferences.Set(nameof(DashboardShowIncomeExpense), value); }
    public bool DashboardShowBudget { get => Preferences.Get(nameof(DashboardShowBudget), true); set => Preferences.Set(nameof(DashboardShowBudget), value); }
    public bool DashboardShowUpcoming { get => Preferences.Get(nameof(DashboardShowUpcoming), true); set => Preferences.Set(nameof(DashboardShowUpcoming), value); }
    public bool DashboardShowCategories { get => Preferences.Get(nameof(DashboardShowCategories), true); set => Preferences.Set(nameof(DashboardShowCategories), value); }
    public bool DashboardShowGoals { get => Preferences.Get(nameof(DashboardShowGoals), true); set => Preferences.Set(nameof(DashboardShowGoals), value); }
    public bool DashboardShowRecent { get => Preferences.Get(nameof(DashboardShowRecent), true); set => Preferences.Set(nameof(DashboardShowRecent), value); }
    public bool DashboardShowCashFlow { get => Preferences.Get(nameof(DashboardShowCashFlow), true); set => Preferences.Set(nameof(DashboardShowCashFlow), value); }
}

public sealed class MauiAppLockService : IAppLockService
{
    private const string SaltKey = "finora.pin.salt";
    private const string HashKey = "finora.pin.hash";
    private const string FailureKey = "finora.pin.failures";
    private const string LockUntilKey = "finora.pin.lockUntil";

    public TimeSpan RemainingLockout { get { var raw = Preferences.Get(LockUntilKey, 0L); var until = DateTimeOffset.FromUnixTimeSeconds(raw); return until > DateTimeOffset.UtcNow ? until - DateTimeOffset.UtcNow : TimeSpan.Zero; } }
    public async Task<bool> HasPinAsync() => !string.IsNullOrEmpty(await SecureStorage.Default.GetAsync(HashKey));

    public async Task<Result> SetPinAsync(string pin)
    {
        if (pin.Length is < 4 or > 12 || pin.Any(c => !char.IsDigit(c))) return Result.Failure("PIN must contain 4–12 digits.");
        var salt = RandomNumberGenerator.GetBytes(16); var hash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, 150_000, HashAlgorithmName.SHA256, 32);
        await SecureStorage.Default.SetAsync(SaltKey, Convert.ToBase64String(salt)); await SecureStorage.Default.SetAsync(HashKey, Convert.ToBase64String(hash));
        Preferences.Remove(FailureKey); Preferences.Remove(LockUntilKey); CryptographicOperations.ZeroMemory(hash); return Result.Success();
    }

    public async Task<bool> VerifyPinAsync(string pin)
    {
        if (RemainingLockout > TimeSpan.Zero) return false;
        var saltRaw = await SecureStorage.Default.GetAsync(SaltKey); var hashRaw = await SecureStorage.Default.GetAsync(HashKey); if (saltRaw is null || hashRaw is null) return true;
        var actual = Rfc2898DeriveBytes.Pbkdf2(pin, Convert.FromBase64String(saltRaw), 150_000, HashAlgorithmName.SHA256, 32); var expected = Convert.FromBase64String(hashRaw); var ok = CryptographicOperations.FixedTimeEquals(actual, expected);
        CryptographicOperations.ZeroMemory(actual); CryptographicOperations.ZeroMemory(expected);
        if (ok) { Preferences.Remove(FailureKey); Preferences.Remove(LockUntilKey); return true; }
        var failures = Preferences.Get(FailureKey, 0) + 1; Preferences.Set(FailureKey, failures);
        if (failures >= 5) { var minutes = Math.Min(30, 1 << Math.Min(5, failures - 5)); Preferences.Set(LockUntilKey, DateTimeOffset.UtcNow.AddMinutes(minutes).ToUnixTimeSeconds()); }
        return false;
    }

    public Task ClearPinAsync() { SecureStorage.Default.Remove(SaltKey); SecureStorage.Default.Remove(HashKey); Preferences.Remove(FailureKey); Preferences.Remove(LockUntilKey); return Task.CompletedTask; }
}
