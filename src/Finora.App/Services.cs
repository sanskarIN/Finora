using System.Security.Cryptography;
using Finora.Application;
using Finora.Domain;
using Finora.Shared;

namespace Finora.App;

public sealed class MauiAppSettingsService : IAppSettingsService
{
    public string DefaultCurrency
    {
        get
        {
            var value = Preferences.Get(nameof(DefaultCurrency), "INR").Trim().ToUpperInvariant();
            try { DomainRules.ValidateCurrency(value); return value; }
            catch (ArgumentException) { return "INR"; }
        }
        set
        {
            var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
            DomainRules.ValidateCurrency(normalized);
            Preferences.Set(nameof(DefaultCurrency), normalized);
        }
    }

    public string Locale
    {
        get => CultureSettings.NormalizeOrFallback(Preferences.Get(nameof(Locale), System.Globalization.CultureInfo.CurrentCulture.Name));
        set => Preferences.Set(nameof(Locale), CultureSettings.NormalizeOrFallback(value));
    }

    public int FinancialMonthStartDay { get => Preferences.Get(nameof(FinancialMonthStartDay), 1); set => Preferences.Set(nameof(FinancialMonthStartDay), Math.Clamp(value, 1, 28)); }
    public bool PrivacyMode { get => Preferences.Get(nameof(PrivacyMode), false); set => Preferences.Set(nameof(PrivacyMode), value); }
    public bool HideAmountsOnLaunch { get => Preferences.Get(nameof(HideAmountsOnLaunch), false); set => Preferences.Set(nameof(HideAmountsOnLaunch), value); }
    public ThemePreference Theme { get => Enum.TryParse<ThemePreference>(Preferences.Get(nameof(Theme), ThemePreference.System.ToString()), out var value) ? value : ThemePreference.System; set => Preferences.Set(nameof(Theme), value.ToString()); }
    public bool ReducedMotion { get => Preferences.Get(nameof(ReducedMotion), false); set => Preferences.Set(nameof(ReducedMotion), value); }
    public bool BackupRemindersEnabled { get => Preferences.Get(nameof(BackupRemindersEnabled), true); set => Preferences.Set(nameof(BackupRemindersEnabled), value); }
    public bool OnboardingComplete { get => Preferences.Get(nameof(OnboardingComplete), false); set => Preferences.Set(nameof(OnboardingComplete), value); }
    public int AutoLockMinutes { get => Math.Clamp(Preferences.Get(nameof(AutoLockMinutes), 5), 1, 60); set => Preferences.Set(nameof(AutoLockMinutes), Math.Clamp(value, 1, 60)); }
    public bool LocalPremiumDemoEnabled { get => Preferences.Get(nameof(LocalPremiumDemoEnabled), false); set => Preferences.Set(nameof(LocalPremiumDemoEnabled), value); }
    public bool NotificationsEnabled { get => Preferences.Get(nameof(NotificationsEnabled), false); set => Preferences.Set(nameof(NotificationsEnabled), value); }
    public bool BiometricUnlockEnabled { get => Preferences.Get(nameof(BiometricUnlockEnabled), false); set => Preferences.Set(nameof(BiometricUnlockEnabled), value); }
    public bool SensitiveScreenProtectionEnabled { get => Preferences.Get(nameof(SensitiveScreenProtectionEnabled), true); set => Preferences.Set(nameof(SensitiveScreenProtectionEnabled), value); }
    public int ReceiptImageQuality { get => Math.Clamp(Preferences.Get(nameof(ReceiptImageQuality), 85), 40, 100); set => Preferences.Set(nameof(ReceiptImageQuality), Math.Clamp(value, 40, 100)); }
    public bool LargerInterface { get => Preferences.Get(nameof(LargerInterface), false); set => Preferences.Set(nameof(LargerInterface), value); }
    public Guid? DefaultAccountId { get => Guid.TryParse(Preferences.Get(nameof(DefaultAccountId), string.Empty), out var value) ? value : null; set { if (value is Guid id) Preferences.Set(nameof(DefaultAccountId), id.ToString()); else Preferences.Remove(nameof(DefaultAccountId)); } }
    public TransactionType DefaultTransactionType { get { var raw = Preferences.Get(nameof(DefaultTransactionType), TransactionType.Expense.ToString()); return Enum.TryParse<TransactionType>(raw, out var value) && value != TransactionType.Transfer ? value : TransactionType.Expense; } set => Preferences.Set(nameof(DefaultTransactionType), value == TransactionType.Transfer ? TransactionType.Expense.ToString() : value.ToString()); }

    public DateTimeOffset? LastBackupAtUtc
    {
        get
        {
            var raw = Preferences.Get(nameof(LastBackupAtUtc), 0L);
            if (raw <= 0) return null;
            try { return DateTimeOffset.FromUnixTimeSeconds(raw); }
            catch (ArgumentOutOfRangeException) { Preferences.Remove(nameof(LastBackupAtUtc)); return null; }
        }
        set
        {
            if (value is DateTimeOffset date) Preferences.Set(nameof(LastBackupAtUtc), date.ToUnixTimeSeconds());
            else Preferences.Remove(nameof(LastBackupAtUtc));
        }
    }

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
    private const string EnabledKey = "finora.pin.enabled";
    private const string FailureKey = "finora.pin.failures";
    private const string LockUntilKey = "finora.pin.lockUntil";
    private const int Pbkdf2Iterations = 150_000;

    public TimeSpan RemainingLockout
    {
        get
        {
            var raw = Preferences.Get(LockUntilKey, 0L);
            if (raw <= 0) return TimeSpan.Zero;
            try
            {
                var until = DateTimeOffset.FromUnixTimeSeconds(raw);
                return until > DateTimeOffset.UtcNow ? until - DateTimeOffset.UtcNow : TimeSpan.Zero;
            }
            catch (ArgumentOutOfRangeException)
            {
                Preferences.Remove(LockUntilKey);
                return TimeSpan.Zero;
            }
        }
    }

    public async Task<bool> HasPinAsync()
    {
        if (Preferences.Get(EnabledKey, false)) return true;
        try
        {
            var hash = await SecureStorage.Default.GetAsync(HashKey);
            var hasLegacyPin = !string.IsNullOrWhiteSpace(hash);
            if (hasLegacyPin) Preferences.Set(EnabledKey, true);
            return hasLegacyPin;
        }
        catch (Exception)
        {
            return Preferences.Get(EnabledKey, false);
        }
    }

    public async Task<Result> SetPinAsync(string pin)
    {
        if (pin.Length is < 4 or > 12 || pin.Any(character => !char.IsDigit(character)))
            return Result.Failure("PIN must contain 4–12 digits.");

        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, 32);
        try
        {
            await SecureStorage.Default.SetAsync(SaltKey, Convert.ToBase64String(salt));
            await SecureStorage.Default.SetAsync(HashKey, Convert.ToBase64String(hash));
            Preferences.Set(EnabledKey, true);
            Preferences.Remove(FailureKey);
            Preferences.Remove(LockUntilKey);
            return Result.Success();
        }
        catch (Exception)
        {
            SecureStorage.Default.Remove(SaltKey);
            SecureStorage.Default.Remove(HashKey);
            Preferences.Remove(EnabledKey);
            return Result.Failure("Finora could not store the PIN verifier in device secure storage.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(salt);
            CryptographicOperations.ZeroMemory(hash);
        }
    }

    public async Task<bool> VerifyPinAsync(string pin)
    {
        if (RemainingLockout > TimeSpan.Zero) return false;
        if (!await HasPinAsync()) return false;

        string? saltRaw;
        string? hashRaw;
        try
        {
            saltRaw = await SecureStorage.Default.GetAsync(SaltKey);
            hashRaw = await SecureStorage.Default.GetAsync(HashKey);
        }
        catch (Exception)
        {
            RegisterFailure();
            return false;
        }

        if (string.IsNullOrWhiteSpace(saltRaw) || string.IsNullOrWhiteSpace(hashRaw))
        {
            RegisterFailure();
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(saltRaw);
            expected = Convert.FromBase64String(hashRaw);
        }
        catch (FormatException)
        {
            RegisterFailure();
            return false;
        }

        if (salt.Length < 16 || expected.Length != 32)
        {
            CryptographicOperations.ZeroMemory(salt);
            CryptographicOperations.ZeroMemory(expected);
            RegisterFailure();
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(pin, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, 32);
        var verified = CryptographicOperations.FixedTimeEquals(actual, expected);
        CryptographicOperations.ZeroMemory(salt);
        CryptographicOperations.ZeroMemory(actual);
        CryptographicOperations.ZeroMemory(expected);

        if (verified)
        {
            Preferences.Set(EnabledKey, true);
            Preferences.Remove(FailureKey);
            Preferences.Remove(LockUntilKey);
            return true;
        }

        RegisterFailure();
        return false;
    }

    public Task ClearPinAsync()
    {
        SecureStorage.Default.Remove(SaltKey);
        SecureStorage.Default.Remove(HashKey);
        Preferences.Remove(EnabledKey);
        Preferences.Remove(FailureKey);
        Preferences.Remove(LockUntilKey);
        return Task.CompletedTask;
    }

    private static void RegisterFailure()
    {
        var previous = Math.Clamp(Preferences.Get(FailureKey, 0), 0, 999);
        var failures = previous + 1;
        Preferences.Set(FailureKey, failures);
        if (failures < 5) return;

        var exponent = Math.Clamp(failures - 5, 0, 5);
        var minutes = Math.Min(30, 1 << exponent);
        Preferences.Set(LockUntilKey, DateTimeOffset.UtcNow.AddMinutes(minutes).ToUnixTimeSeconds());
    }
}
