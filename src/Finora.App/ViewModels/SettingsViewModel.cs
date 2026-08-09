using System.Collections.ObjectModel;
using Finora.Application;
using Finora.Domain;
using Finora.Shared;

namespace Finora.App;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly IAppSettingsService _settings;
    private readonly IFinanceStore _store;
    private bool _privacyMode;
    private bool _hideAmounts;
    private bool _reducedMotion;
    private bool _backupReminders;
    private bool _notificationsEnabled;
    private bool _biometricUnlock;
    private bool _sensitiveScreenProtection;
    private bool _largerInterface;
    private ThemePreference _theme;
    private string _currency;
    private string _locale;
    private int _monthStartDay;
    private int _autoLockMinutes;
    private int _receiptImageQuality;
    private bool _localPremiumDemoEnabled;
    private AccountSummary? _defaultAccount;
    private TransactionType _defaultTransactionType;

    public SettingsViewModel(IAppSettingsService settings, IFinanceStore store)
    {
        _settings = settings;
        _store = store;
        _privacyMode = settings.PrivacyMode;
        _hideAmounts = settings.HideAmountsOnLaunch;
        _reducedMotion = settings.ReducedMotion;
        _backupReminders = settings.BackupRemindersEnabled;
        _notificationsEnabled = settings.NotificationsEnabled;
        _biometricUnlock = settings.BiometricUnlockEnabled;
        _sensitiveScreenProtection = settings.SensitiveScreenProtectionEnabled;
        _largerInterface = settings.LargerInterface;
        _theme = settings.Theme;
        _currency = settings.DefaultCurrency;
        _locale = CultureSettings.NormalizeOrFallback(settings.Locale);
        _monthStartDay = settings.FinancialMonthStartDay;
        _autoLockMinutes = settings.AutoLockMinutes;
        _receiptImageQuality = settings.ReceiptImageQuality;
        _localPremiumDemoEnabled = settings.LocalPremiumDemoEnabled;
        _defaultTransactionType = settings.DefaultTransactionType;
        LoadCommand = new AsyncCommand(LoadAsync);
    }

    public ObservableCollection<AccountSummary> Accounts { get; } = [];
    public IReadOnlyList<ThemePreference> Themes { get; } = Enum.GetValues<ThemePreference>();
    public IReadOnlyList<TransactionType> TransactionTypes { get; } = [TransactionType.Expense, TransactionType.Income, TransactionType.Refund, TransactionType.Adjustment];
    public bool PrivacyMode { get => _privacyMode; set { if (SetProperty(ref _privacyMode, value)) _settings.PrivacyMode = value; } }
    public bool HideAmounts { get => _hideAmounts; set { if (SetProperty(ref _hideAmounts, value)) _settings.HideAmountsOnLaunch = value; } }
    public bool ReducedMotion { get => _reducedMotion; set { if (SetProperty(ref _reducedMotion, value)) _settings.ReducedMotion = value; } }
    public bool BackupReminders { get => _backupReminders; set { if (SetProperty(ref _backupReminders, value)) _settings.BackupRemindersEnabled = value; } }
    public bool NotificationsEnabled { get => _notificationsEnabled; set { if (SetProperty(ref _notificationsEnabled, value)) _settings.NotificationsEnabled = value; } }
    public bool BiometricUnlock { get => _biometricUnlock; set { if (SetProperty(ref _biometricUnlock, value)) _settings.BiometricUnlockEnabled = value; } }
    public bool SensitiveScreenProtection { get => _sensitiveScreenProtection; set { if (SetProperty(ref _sensitiveScreenProtection, value)) _settings.SensitiveScreenProtectionEnabled = value; } }
    public bool LargerInterface { get => _largerInterface; set { if (SetProperty(ref _largerInterface, value)) { _settings.LargerInterface = value; ApplyLargerInterface(value); } } }
    public ThemePreference Theme { get => _theme; set { if (SetProperty(ref _theme, value)) { _settings.Theme = value; ApplyTheme(value); } } }

    public string Currency
    {
        get => _currency;
        set
        {
            var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
            try { DomainRules.ValidateCurrency(normalized); }
            catch (ArgumentException) { return; }
            if (SetProperty(ref _currency, normalized)) _settings.DefaultCurrency = normalized;
        }
    }

    public string Locale
    {
        get => _locale;
        set
        {
            var normalized = CultureSettings.NormalizeOrFallback(value, _locale);
            if (!SetProperty(ref _locale, normalized)) return;
            _settings.Locale = normalized;
            CultureSettings.TryApply(normalized);
            OnPropertyChanged(nameof(NumberFormatPreview));
        }
    }

    public string NumberFormatPreview => $"{new Money(1234567, Currency).Format()} · {DateTime.Today.ToString("d", System.Globalization.CultureInfo.CurrentCulture)}";
    public int MonthStartDay { get => _monthStartDay; set { var clamped = Math.Clamp(value, 1, 28); if (SetProperty(ref _monthStartDay, clamped)) _settings.FinancialMonthStartDay = clamped; } }
    public int AutoLockMinutes { get => _autoLockMinutes; set { var clamped = Math.Clamp(value, 1, 60); if (SetProperty(ref _autoLockMinutes, clamped)) _settings.AutoLockMinutes = clamped; } }
    public int ReceiptImageQuality { get => _receiptImageQuality; set { var clamped = Math.Clamp(value, 40, 100); if (SetProperty(ref _receiptImageQuality, clamped)) _settings.ReceiptImageQuality = clamped; } }
    public bool LocalPremiumDemoEnabled { get => _localPremiumDemoEnabled; set { if (SetProperty(ref _localPremiumDemoEnabled, value)) _settings.LocalPremiumDemoEnabled = value; } }
    public AccountSummary? DefaultAccount { get => _defaultAccount; set { if (SetProperty(ref _defaultAccount, value)) _settings.DefaultAccountId = value?.Id; } }
    public TransactionType DefaultTransactionType { get => _defaultTransactionType; set { if (SetProperty(ref _defaultTransactionType, value)) _settings.DefaultTransactionType = value; } }
    public System.Windows.Input.ICommand LoadCommand { get; }

    public Task LoadAsync() => RunAsync(async () =>
    {
        Accounts.Clear();
        foreach (var account in await _store.GetAccountsAsync()) Accounts.Add(account);
        DefaultAccount = Accounts.FirstOrDefault(x => x.Id == _settings.DefaultAccountId) ?? Accounts.FirstOrDefault();
        OnPropertyChanged(nameof(NumberFormatPreview));
    });

    public static void ApplyTheme(ThemePreference theme)
    {
        if (Application.Current is null) return;
        Application.Current.UserAppTheme = theme switch
        {
            ThemePreference.Light => AppTheme.Light,
            ThemePreference.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }

    public static void ApplyLargerInterface(bool enabled)
    {
        if (Application.Current?.Resources is null) return;
        Application.Current.Resources["FinoraBodyFontSize"] = enabled ? 18d : 14d;
        Application.Current.Resources["FinoraControlHeight"] = enabled ? 56d : 48d;
    }
}
