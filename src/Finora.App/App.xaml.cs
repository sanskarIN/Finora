using Finora.Application;
using Finora.Domain;

namespace Finora.App;

public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly IFinanceStore _store;
    private readonly IAppSettingsService _settings;
    private readonly IAppLockService _appLock;
    private readonly ISensitiveScreenService _sensitiveScreen;
    private readonly ReminderCoordinator _reminders;
    private readonly AppExceptionCoordinator _exceptions;
    private DateTimeOffset? _deactivatedAtUtc;

    public App(
        IFinanceStore store,
        IAppSettingsService settings,
        IAppLockService appLock,
        ISensitiveScreenService sensitiveScreen,
        ReminderCoordinator reminders,
        AppExceptionCoordinator exceptions)
    {
        InitializeComponent();
        _store = store;
        _settings = settings;
        _appLock = appLock;
        _sensitiveScreen = sensitiveScreen;
        _reminders = reminders;
        _exceptions = exceptions;
        _exceptions.Start();

        UserAppTheme = _settings.Theme switch
        {
            ThemePreference.Light => AppTheme.Light,
            ThemePreference.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
        SettingsViewModel.ApplyLargerInterface(_settings.LargerInterface);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var shell = new AppShell();
        var window = new Window(shell);
        window.Deactivated += (_, _) => _deactivatedAtUtc = DateTimeOffset.UtcNow;
        window.Activated += async (_, _) => await OnActivatedSafelyAsync();
        _ = InitializeAsync(shell);
        return window;
    }

    private async Task InitializeAsync(AppShell shell)
    {
        try
        {
            await _store.InitializeAsync().ConfigureAwait(false);
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (_settings.SensitiveScreenProtectionEnabled)
                    await _sensitiveScreen.SetProtectionAsync(true);

                if (_settings.NotificationsEnabled)
                    await _reminders.SyncAsync();

                if (await _appLock.HasPinAsync())
                    await Shell.Current.GoToAsync("//lock");
                else if (!_settings.OnboardingComplete)
                    await Shell.Current.GoToAsync("//onboarding");
                else
                    await Shell.Current.GoToAsync(AppRoutes.DashboardRoot);
            });
        }
        catch (Exception ex)
        {
            _exceptions.Report(ex, "app_initialize_failed");
            await MainThread.InvokeOnMainThreadAsync(() =>
                shell.DisplayAlertAsync(
                    "Finora",
                    $"Local data initialization failed ({ex.GetType().Name}). No financial contents were written to diagnostics.",
                    "OK"));
        }
    }

    private async Task OnActivatedSafelyAsync()
    {
        try
        {
            await OnActivatedAsync();
        }
        catch (Exception ex)
        {
            _exceptions.Report(ex, "app_activation_failed");
        }
    }

    private async Task OnActivatedAsync()
    {
        if (_settings.SensitiveScreenProtectionEnabled)
            await _sensitiveScreen.SetProtectionAsync(true);

        if (_settings.NotificationsEnabled)
            await _reminders.SyncAsync();

        await LockAfterInactivityAsync();
    }

    private async Task LockAfterInactivityAsync()
    {
        if (_deactivatedAtUtc is null)
            return;

        var elapsed = DateTimeOffset.UtcNow - _deactivatedAtUtc.Value;
        _deactivatedAtUtc = null;
        if (elapsed < TimeSpan.FromMinutes(_settings.AutoLockMinutes) || !await _appLock.HasPinAsync())
            return;

        if (Shell.Current is not null)
            await Shell.Current.GoToAsync("//lock");
    }
}
