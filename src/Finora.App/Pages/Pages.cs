using System.Security.Cryptography;
using Finora.Application;

namespace Finora.App;

public partial class DashboardPage : ContentPage
{
    private DashboardViewModel ViewModel => (DashboardViewModel)BindingContext;
    public DashboardPage() { InitializeComponent(); BindingContext = new DashboardViewModel(ServiceHelper.Get<IFinanceStore>(), ServiceHelper.Get<IRecurringWorkflowService>(), ServiceHelper.Get<IAdvancedReportService>(), ServiceHelper.Get<IAppSettingsService>()); }
    protected override void OnAppearing() { base.OnAppearing(); _ = ViewModel.LoadAsync(); }
    private async void OnAccountsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(AccountsPage));
    private async void OnReportsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(ReportsPage));
    private async void OnRecurringClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(RecurringPage));
    private async void OnRecentTransactionSelected(object? sender, SelectionChangedEventArgs e) { if (e.CurrentSelection.FirstOrDefault() is not DashboardTransactionItem item) return; RecentList.SelectedItem = null; await Shell.Current.GoToAsync($"{nameof(TransactionDetailPage)}?transactionId={item.Id}"); }
}

public partial class AccountsPage : ContentPage
{
    private AccountsViewModel ViewModel => (AccountsViewModel)BindingContext;
    public AccountsPage() { InitializeComponent(); BindingContext = new AccountsViewModel(ServiceHelper.Get<IFinanceStore>(), ServiceHelper.Get<IAppSettingsService>()); }
    protected override void OnAppearing() { base.OnAppearing(); _ = ViewModel.LoadAsync(); }
    private async void OnAccountSelected(object? sender, SelectionChangedEventArgs e) { if (e.CurrentSelection.FirstOrDefault() is not AccountSummary item) return; AccountList.SelectedItem = null; await Shell.Current.GoToAsync($"{nameof(AccountDetailPage)}?accountId={item.Id}"); }
    private async void OnReconcileClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(ReconciliationPage));
    private async void OnAccountCategoriesClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(CategoriesTagsPage));
}

public partial class TransactionsPage : ContentPage
{
    private TransactionsViewModel ViewModel => (TransactionsViewModel)BindingContext;
    public TransactionsPage() { InitializeComponent(); BindingContext = new TransactionsViewModel(ServiceHelper.Get<IFinanceStore>(), ServiceHelper.Get<IAppSettingsService>()); }
    protected override void OnAppearing() { base.OnAppearing(); _ = ViewModel.LoadAsync(); }
    private async void OnTransactionSelected(object? sender, SelectionChangedEventArgs e) { if (e.CurrentSelection.FirstOrDefault() is not TransactionListItem item) return; if (sender is CollectionView list) list.SelectedItem = null; await Shell.Current.GoToAsync($"{nameof(TransactionDetailPage)}?transactionId={item.Id}"); }
    private async void OnImportClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(ImportPage));
    private async void OnToolsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(TransactionToolsPage));
    private async void OnCategoriesClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(CategoriesTagsPage));
    private void OnCalculatorToken(object? sender, EventArgs e) { if (sender is Button button) ViewModel.AppendCalculatorToken(button.Text); }
    private void OnCalculatorBackspace(object? sender, EventArgs e) => ViewModel.BackspaceCalculator();
    private void OnCalculatorClear(object? sender, EventArgs e) => ViewModel.ClearCalculator();
    private void OnCalculatorEquals(object? sender, EventArgs e) => ViewModel.EvaluateCalculator();
}

public partial class BudgetsPage : ContentPage
{
    private BudgetsViewModel ViewModel => (BudgetsViewModel)BindingContext;
    public BudgetsPage() { InitializeComponent(); BindingContext = new BudgetsViewModel(ServiceHelper.Get<IFinanceStore>(), ServiceHelper.Get<IAppSettingsService>(), ServiceHelper.Get<ReminderCoordinator>()); }
    protected override void OnAppearing() { base.OnAppearing(); _ = ViewModel.LoadAsync(); }
}

public partial class SavingsPage : ContentPage
{
    private SavingsViewModel ViewModel => (SavingsViewModel)BindingContext;
    public SavingsPage() { InitializeComponent(); BindingContext = new SavingsViewModel(ServiceHelper.Get<IFinanceStore>(), ServiceHelper.Get<IAppSettingsService>()); }
    protected override void OnAppearing() { base.OnAppearing(); _ = ViewModel.LoadAsync(); }
}

public partial class RecurringPage : ContentPage
{
    private RecurringViewModel ViewModel => (RecurringViewModel)BindingContext;
    public RecurringPage() { InitializeComponent(); BindingContext = new RecurringViewModel(ServiceHelper.Get<IFinanceStore>(), ServiceHelper.Get<IRecurringWorkflowService>(), ServiceHelper.Get<ILocalNotificationService>(), ServiceHelper.Get<IAppSettingsService>()); }
    protected override void OnAppearing() { base.OnAppearing(); _ = ViewModel.LoadAsync(); }
}

public partial class ReportsPage : ContentPage
{
    private readonly IExportService _export = ServiceHelper.Get<IExportService>();
    private readonly IPrivacyLogger _logger = ServiceHelper.Get<IPrivacyLogger>();
    private ReportsViewModel ViewModel => (ReportsViewModel)BindingContext;
    public ReportsPage() { InitializeComponent(); BindingContext = new ReportsViewModel(ServiceHelper.Get<IAdvancedReportService>(), ServiceHelper.Get<IAppSettingsService>()); }
    protected override void OnAppearing() { base.OnAppearing(); _ = ViewModel.LoadAsync(); }
    private async void OnExportCsvClicked(object? sender, EventArgs e) => await ExportAsync(false);
    private async void OnExportPdfClicked(object? sender, EventArgs e) => await ExportAsync(true);

    private async Task ExportAsync(bool pdf)
    {
        try
        {
            var suffix = DateTime.Now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
            var path = Path.Combine(FileSystem.CacheDirectory, $"Finora-transactions-{suffix}.{(pdf ? "pdf" : "csv")}");
            if (pdf) await File.WriteAllBytesAsync(path, await _export.ExportTransactionsPdfAsync());
            else await File.WriteAllTextAsync(path, await _export.ExportTransactionsCsvAsync());
            await Share.Default.RequestAsync(new ShareFileRequest("Export Finora transactions", new ShareFile(path)));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Reports.ExportFailed");
            await DisplayAlertAsync("Export failed", "The export could not be created or shared. Review local storage/share permissions and try again.", "OK");
        }
    }
}

public partial class OnboardingPage : ContentPage
{
    public OnboardingPage() { InitializeComponent(); BindingContext = new OnboardingViewModel(ServiceHelper.Get<IFinanceStore>(), ServiceHelper.Get<IAppSettingsService>()); }
    private async void OnOnboardingPrivacyClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync($"{nameof(LegalPage)}?document=privacy");
}

public partial class LockPage : ContentPage
{
    public LockPage() { InitializeComponent(); BindingContext = new LockViewModel(ServiceHelper.Get<IAppLockService>(), ServiceHelper.Get<IAppSettingsService>(), ServiceHelper.Get<IBiometricService>()); }
}

public partial class SettingsPage : ContentPage
{
    private int _versionTapCount;
    private readonly IFinanceStore _store = ServiceHelper.Get<IFinanceStore>();
    private readonly IBackupService _backup = ServiceHelper.Get<IBackupService>();
    private readonly IAppLockService _lock = ServiceHelper.Get<IAppLockService>();
    private readonly IPrivacyLogger _logger = ServiceHelper.Get<IPrivacyLogger>();
    private readonly IAppSettingsService _settings = ServiceHelper.Get<IAppSettingsService>();
    private readonly IAttachmentService _attachments = ServiceHelper.Get<IAttachmentService>();
    private readonly ILocalNotificationService _notifications = ServiceHelper.Get<ILocalNotificationService>();
    private readonly IBiometricService _biometric = ServiceHelper.Get<IBiometricService>();
    private readonly ISensitiveScreenService _sensitiveScreen = ServiceHelper.Get<ISensitiveScreenService>();
    private readonly ReminderCoordinator _reminders = ServiceHelper.Get<ReminderCoordinator>();
    private SettingsViewModel ViewModel => (SettingsViewModel)BindingContext;

    public SettingsPage() { InitializeComponent(); BindingContext = new SettingsViewModel(_settings, _store); }
    protected override void OnAppearing() { base.OnAppearing(); _ = LoadSurfaceAsync(); }

    private async Task LoadSurfaceAsync()
    {
        await ViewModel.LoadAsync();
        StorageUsageLabel.Text = $"Receipt/attachment storage: {FormatBytes(await _attachments.GetStorageUsageBytesAsync())}";
        NotificationStatusLabel.Text = $"Notification permission: {await _notifications.GetPermissionStateAsync()}.";
        SecurityCapabilityLabel.Text = $"Biometric/Windows Hello: {await _biometric.GetAvailabilityAsync()}. Capture protection supported: {_sensitiveScreen.IsProtectionSupported}.";
    }

    private async void OnCreateBackupClicked(object? sender, EventArgs e)
    {
        var password = await DisplayPromptAsync("Encrypted backup", "Create a strong backup password. Finora cannot recover a forgotten backup password.", "Create", "Cancel", "Password", 128, Keyboard.Default);
        if (string.IsNullOrWhiteSpace(password)) return;
        byte[]? bytes = null;
        try
        {
            bytes = await _backup.CreateEncryptedBackupAsync(password);
            var path = Path.Combine(FileSystem.CacheDirectory, $"Finora-{DateTime.Now:yyyyMMdd-HHmmss}.finora-backup");
            await File.WriteAllBytesAsync(path, bytes);
            _settings.LastBackupAtUtc = DateTimeOffset.UtcNow;
            await _reminders.SyncAsync();
            await Share.Default.RequestAsync(new ShareFileRequest("Save encrypted Finora backup", new ShareFile(path)));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Settings.BackupCreateFailed");
            await DisplayAlertAsync("Backup failed", "The encrypted backup could not be created or shared. Existing finance data was not changed.", "OK");
        }
        finally
        {
            if (bytes is not null) CryptographicOperations.ZeroMemory(bytes);
        }
    }

    private async void OnRestoreBackupClicked(object? sender, EventArgs e)
    {
        var picked = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Choose a Finora backup" });
        if (picked is null) return;
        var password = await DisplayPromptAsync("Restore backup", "Enter the backup password.", "Preview", "Cancel", "Password", 128, Keyboard.Default);
        if (string.IsNullOrWhiteSpace(password)) return;
        try
        {
            await using var previewStream = await picked.OpenReadAsync();
            var preview = await _backup.PreviewEncryptedBackupAsync(previewStream, password);
            if (!preview.IsSuccess || preview.Value is null)
            {
                await DisplayAlertAsync("Cannot restore", preview.Error ?? "Backup validation failed.", "OK");
                return;
            }

            var p = preview.Value;
            if (!await DisplayAlertAsync("Restore backup?", $"Schema {p.SchemaVersion}\nAccounts: {p.AccountCount}\nTransactions: {p.TransactionCount}\nBudgets: {p.BudgetCount}\nGoals: {p.SavingsGoalCount}\n\nCurrent local finance data is replaced only if validation succeeds.", "Restore", "Cancel")) return;

            await using var restoreStream = await picked.OpenReadAsync();
            var result = await _backup.RestoreEncryptedBackupAsync(restoreStream, password);
            if (result.IsSuccess) await _attachments.CleanupOrphanedFilesAsync();
            await DisplayAlertAsync(result.IsSuccess ? "Restore complete" : "Restore failed", result.IsSuccess ? "The encrypted backup was restored." : result.Error ?? "Restore failed.", "OK");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Settings.BackupRestoreFailed");
            await DisplayAlertAsync("Restore failed", "The encrypted backup could not be restored. Existing data remains unchanged unless Finora reported restore completion.", "OK");
        }
    }

    private async void OnExportLogClicked(object? sender, EventArgs e)
    {
        try
        {
            var path = await _logger.ExportSanitizedLogAsync();
            await Share.Default.RequestAsync(new ShareFileRequest("Export sanitized Finora diagnostics", new ShareFile(path)));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Settings.DiagnosticsExportFailed");
            await DisplayAlertAsync("Export failed", "The sanitized diagnostics file could not be created or shared.", "OK");
        }
    }

    private async void OnSetPinClicked(object? sender, EventArgs e)
    {
        var pin = await DisplayPromptAsync("App lock PIN", "Enter a 4–12 digit PIN.", "Next", "Cancel", "PIN", 12, Keyboard.Numeric); if (string.IsNullOrWhiteSpace(pin)) return;
        var again = await DisplayPromptAsync("Confirm PIN", "Enter the same PIN again.", "Save", "Cancel", "PIN", 12, Keyboard.Numeric); if (pin != again) { await DisplayAlertAsync("PIN not changed", "The PIN entries did not match.", "OK"); return; }
        var result = await _lock.SetPinAsync(pin); await DisplayAlertAsync(result.IsSuccess ? "PIN saved" : "PIN not changed", result.IsSuccess ? "App lock is enabled." : result.Error ?? "PIN was not accepted.", "OK");
    }

    private async void OnRemovePinClicked(object? sender, EventArgs e)
    {
        if (!await DisplayAlertAsync("Remove app lock?", "Anyone with access to this device session may open Finora after the PIN is removed.", "Remove", "Cancel")) return;
        await _lock.ClearPinAsync(); ViewModel.BiometricUnlock = false; await DisplayAlertAsync("App lock removed", "The local PIN and biometric unlock preference have been removed.", "OK");
    }

    private async void OnDeleteAllClicked(object? sender, EventArgs e)
    {
        var confirmation = await DisplayPromptAsync("Delete all local finance data", "Type DELETE to permanently remove accounts, transactions, budgets, goals, receipts and other finance data from this app.", "Delete", "Cancel");
        if (!string.Equals(confirmation, "DELETE", StringComparison.Ordinal)) return; await _store.DeleteAllDataAsync(); await _attachments.CleanupOrphanedFilesAsync(); await DisplayAlertAsync("Local data deleted", "Finora finance data and local receipt files were deleted. Preferences and app-lock configuration are kept.", "OK"); await LoadSurfaceAsync();
    }

    private async void OnRequestNotificationsClicked(object? sender, EventArgs e)
    {
        var state = await _notifications.RequestPermissionAsync(); ViewModel.NotificationsEnabled = state == NotificationPermissionState.Granted; NotificationStatusLabel.Text = $"Notification permission: {state}."; if (state == NotificationPermissionState.Granted) await _reminders.SyncAsync();
    }

    private async void OnBiometricToggled(object? sender, ToggledEventArgs e)
    {
        if (!e.Value) { ViewModel.BiometricUnlock = false; return; }
        if (!await _lock.HasPinAsync()) { ViewModel.BiometricUnlock = false; await DisplayAlertAsync("PIN required", "Set a Finora PIN before enabling biometric unlock so a PIN fallback remains available.", "OK"); return; }
        var availability = await _biometric.GetAvailabilityAsync(); if (availability != BiometricAvailability.Available) { ViewModel.BiometricUnlock = false; await DisplayAlertAsync("Biometrics unavailable", $"Biometric/Windows Hello status: {availability}.", "OK"); return; }
        var verify = await _biometric.AuthenticateAsync("Verify biometrics before enabling Finora biometric unlock."); if (!verify.IsSuccess) { ViewModel.BiometricUnlock = false; await DisplayAlertAsync("Not enabled", verify.Error ?? "Biometric verification was not completed.", "OK"); }
    }

    private async void OnSensitiveScreenToggled(object? sender, ToggledEventArgs e)
    {
        var result = await _sensitiveScreen.SetProtectionAsync(e.Value); if (!result.IsSuccess && e.Value) { ViewModel.SensitiveScreenProtection = false; await DisplayAlertAsync("Capture protection unavailable", result.Error ?? "This platform cannot reliably block screenshots.", "OK"); }
    }

    private async void OnCleanupAttachmentsClicked(object? sender, EventArgs e) { var removed = await _attachments.CleanupOrphanedFilesAsync(); StorageUsageLabel.Text = $"Receipt/attachment storage: {FormatBytes(await _attachments.GetStorageUsageBytesAsync())}"; await DisplayAlertAsync("Receipt cleanup", $"Removed {removed} orphaned local file(s).", "OK"); }
    private async void OnSimulateNotificationsClicked(object? sender, EventArgs e) { await _reminders.SyncAsync(); await DisplayAlertAsync("Reminder sync", "Pending local reminders were reconciled with current settings. No private transaction contents were added to notifications.", "OK"); }
    private void OnVersionTapped(object? sender, TappedEventArgs e) { if (++_versionTapCount >= 7) DeveloperPanel.IsVisible = true; }
    private async void OnSchemaVersionClicked(object? sender, EventArgs e) => await DisplayAlertAsync("Database schema", $"Schema version: {Finora.Shared.AppConstants.DatabaseSchemaVersion}", "OK");
    private async void OnFeatureFlagsClicked(object? sender, EventArgs e) => await DisplayAlertAsync("Feature flags", $"LocalPremiumDemoEnabled={ViewModel.LocalPremiumDemoEnabled}\nCloudSync=false\nTelemetry=false\nNotifications={_settings.NotificationsEnabled}\nBiometricUnlock={_settings.BiometricUnlockEnabled}\nSensitiveScreenProtection={_settings.SensitiveScreenProtectionEnabled}", "OK");
    private async void OnCategoriesClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(CategoriesTagsPage));
    private async void OnOnboardingClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//onboarding");
    private async void OnPrivacyClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync($"{nameof(LegalPage)}?document=privacy");
    private async void OnTermsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync($"{nameof(LegalPage)}?document=terms");
    private async void OnNoticesClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync($"{nameof(LegalPage)}?document=notices");
    private async void OnRepositoryClicked(object? sender, EventArgs e) => await Launcher.Default.OpenAsync("https://github.com/sanskarIN/Finora");
    private async void OnProfileClicked(object? sender, EventArgs e) => await Launcher.Default.OpenAsync("https://www.github.com/sanskarIN");
    private async void OnBusinessEmailClicked(object? sender, EventArgs e) => await ComposeEmailAsync("sanskarin@outlook.in", "Finora business inquiry");
    private async void OnSupportEmailClicked(object? sender, EventArgs e) => await ComposeEmailAsync("supportramsandesh@gmail.com", "Finora support");

    private async Task ComposeEmailAsync(string address, string subject)
    {
        try
        {
            await Email.Default.ComposeAsync(new EmailMessage { To = [address], Subject = subject });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Settings.EmailComposeFailed");
            await DisplayAlertAsync("Email unavailable", "No compatible email app is available right now.", "OK");
        }
    }

    private static string FormatBytes(long bytes) { string[] units = ["B", "KB", "MB", "GB"]; decimal value = Math.Max(0, bytes); var index = 0; while (value >= 1024 && index < units.Length - 1) { value /= 1024; index++; } return $"{value:0.##} {units[index]}"; }
}
