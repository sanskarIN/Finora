using Finora.Application;

namespace Finora.App;

public partial class DashboardPage : ContentPage
{
    private DashboardViewModel ViewModel => (DashboardViewModel)BindingContext;
    public DashboardPage()
    {
        InitializeComponent();
        BindingContext = new DashboardViewModel(ServiceHelper.Get<IFinanceStore>(), ServiceHelper.Get<IAppSettingsService>());
    }
    protected override void OnAppearing() { base.OnAppearing(); _ = ViewModel.LoadAsync(); }
    private async void OnAccountsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(AccountsPage));
    private async void OnReportsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(ReportsPage));
    private async void OnRecurringClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(RecurringPage));
}

public partial class AccountsPage : ContentPage
{
    private AccountsViewModel ViewModel => (AccountsViewModel)BindingContext;
    public AccountsPage()
    {
        InitializeComponent();
        BindingContext = new AccountsViewModel(ServiceHelper.Get<IFinanceStore>(), ServiceHelper.Get<IAppSettingsService>());
    }
    protected override void OnAppearing() { base.OnAppearing(); _ = ViewModel.LoadAsync(); }
}

public partial class TransactionsPage : ContentPage
{
    private TransactionsViewModel ViewModel => (TransactionsViewModel)BindingContext;
    public TransactionsPage()
    {
        InitializeComponent();
        BindingContext = new TransactionsViewModel(ServiceHelper.Get<IFinanceStore>());
    }
    protected override void OnAppearing() { base.OnAppearing(); _ = ViewModel.LoadAsync(); }
}

public partial class BudgetsPage : ContentPage
{
    private BudgetsViewModel ViewModel => (BudgetsViewModel)BindingContext;
    public BudgetsPage()
    {
        InitializeComponent();
        BindingContext = new BudgetsViewModel(ServiceHelper.Get<IFinanceStore>(), ServiceHelper.Get<IAppSettingsService>());
    }
    protected override void OnAppearing() { base.OnAppearing(); _ = ViewModel.LoadAsync(); }
}

public partial class SavingsPage : ContentPage
{
    private SavingsViewModel ViewModel => (SavingsViewModel)BindingContext;
    public SavingsPage()
    {
        InitializeComponent();
        BindingContext = new SavingsViewModel(ServiceHelper.Get<IFinanceStore>(), ServiceHelper.Get<IAppSettingsService>());
    }
    protected override void OnAppearing() { base.OnAppearing(); _ = ViewModel.LoadAsync(); }
}

public partial class RecurringPage : ContentPage
{
    private RecurringViewModel ViewModel => (RecurringViewModel)BindingContext;
    public RecurringPage()
    {
        InitializeComponent();
        BindingContext = new RecurringViewModel(ServiceHelper.Get<IFinanceStore>());
    }
    protected override void OnAppearing() { base.OnAppearing(); _ = ViewModel.LoadAsync(); }
}

public partial class ReportsPage : ContentPage
{
    private ReportsViewModel ViewModel => (ReportsViewModel)BindingContext;
    private readonly IExportService _export = ServiceHelper.Get<IExportService>();

    public ReportsPage()
    {
        InitializeComponent();
        BindingContext = new ReportsViewModel(ServiceHelper.Get<IFinanceStore>(), ServiceHelper.Get<IAppSettingsService>());
    }

    protected override void OnAppearing() { base.OnAppearing(); _ = ViewModel.LoadAsync(); }

    private async void OnExportCsvClicked(object? sender, EventArgs e)
    {
        try
        {
            var csv = await _export.ExportTransactionsCsvAsync();
            var path = Path.Combine(FileSystem.CacheDirectory, $"Finora-transactions-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
            await File.WriteAllTextAsync(path, csv);
            await Share.Default.RequestAsync(new ShareFileRequest("Export Finora transactions", new ShareFile(path)));
        }
        catch (Exception ex) { await DisplayAlertAsync("Export failed", ex.Message, "OK"); }
    }

    private async void OnExportPdfClicked(object? sender, EventArgs e)
    {
        try
        {
            var pdf = await _export.ExportTransactionsPdfAsync();
            var path = Path.Combine(FileSystem.CacheDirectory, $"Finora-transactions-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
            await File.WriteAllBytesAsync(path, pdf);
            await Share.Default.RequestAsync(new ShareFileRequest("Export Finora transactions", new ShareFile(path)));
        }
        catch (Exception ex) { await DisplayAlertAsync("Export failed", ex.Message, "OK"); }
    }
}

public partial class OnboardingPage : ContentPage
{
    public OnboardingPage()
    {
        InitializeComponent();
        BindingContext = new OnboardingViewModel(ServiceHelper.Get<IFinanceStore>(), ServiceHelper.Get<IAppSettingsService>());
    }
}

public partial class LockPage : ContentPage
{
    public LockPage()
    {
        InitializeComponent();
        BindingContext = new LockViewModel(ServiceHelper.Get<IAppLockService>(), ServiceHelper.Get<IAppSettingsService>());
    }
}

public partial class SettingsPage : ContentPage
{
    private int _versionTapCount;
    private readonly IFinanceStore _store = ServiceHelper.Get<IFinanceStore>();
    private readonly IBackupService _backup = ServiceHelper.Get<IBackupService>();
    private readonly IAppLockService _lock = ServiceHelper.Get<IAppLockService>();
    private readonly IPrivacyLogger _logger = ServiceHelper.Get<IPrivacyLogger>();

    public SettingsPage()
    {
        InitializeComponent();
        BindingContext = new SettingsViewModel(ServiceHelper.Get<IAppSettingsService>());
    }

    private async void OnCreateBackupClicked(object? sender, EventArgs e)
    {
        var password = await DisplayPromptAsync("Encrypted backup", "Create a strong backup password. Finora cannot recover a forgotten backup password.", "Create", "Cancel", "Password", 128, Keyboard.Default);
        if (string.IsNullOrWhiteSpace(password)) return;
        try
        {
            var bytes = await _backup.CreateEncryptedBackupAsync(password);
            var path = Path.Combine(FileSystem.CacheDirectory, $"Finora-{DateTime.Now:yyyyMMdd-HHmmss}.finora-backup");
            await File.WriteAllBytesAsync(path, bytes);
            await Share.Default.RequestAsync(new ShareFileRequest("Save encrypted Finora backup", new ShareFile(path)));
        }
        catch (Exception ex) { await DisplayAlertAsync("Backup failed", ex.Message, "OK"); }
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
            var confirm = await DisplayAlertAsync("Restore backup?", $"Schema {p.SchemaVersion}\nAccounts: {p.AccountCount}\nTransactions: {p.TransactionCount}\nBudgets: {p.BudgetCount}\nGoals: {p.SavingsGoalCount}\n\nCurrent local finance data will be replaced only if validation succeeds.", "Restore", "Cancel");
            if (!confirm) return;
            await using var restoreStream = await picked.OpenReadAsync();
            var result = await _backup.RestoreEncryptedBackupAsync(restoreStream, password);
            await DisplayAlertAsync(result.IsSuccess ? "Restore complete" : "Restore failed", result.IsSuccess ? "The encrypted backup was restored." : result.Error ?? "Restore failed.", "OK");
        }
        catch (Exception ex) { await DisplayAlertAsync("Restore failed", ex.Message, "OK"); }
    }

    private async void OnExportLogClicked(object? sender, EventArgs e)
    {
        try
        {
            var path = await _logger.ExportSanitizedLogAsync();
            await Share.Default.RequestAsync(new ShareFileRequest("Export sanitized Finora diagnostics", new ShareFile(path)));
        }
        catch (Exception ex) { await DisplayAlertAsync("Export failed", ex.Message, "OK"); }
    }

    private async void OnSetPinClicked(object? sender, EventArgs e)
    {
        var pin = await DisplayPromptAsync("App lock PIN", "Enter a 4–12 digit PIN.", "Next", "Cancel", "PIN", 12, Keyboard.Numeric);
        if (string.IsNullOrWhiteSpace(pin)) return;
        var again = await DisplayPromptAsync("Confirm PIN", "Enter the same PIN again.", "Save", "Cancel", "PIN", 12, Keyboard.Numeric);
        if (pin != again) { await DisplayAlertAsync("PIN not changed", "The PIN entries did not match.", "OK"); return; }
        var result = await _lock.SetPinAsync(pin);
        await DisplayAlertAsync(result.IsSuccess ? "PIN saved" : "PIN not changed", result.IsSuccess ? "App lock is enabled." : result.Error ?? "PIN was not accepted.", "OK");
    }

    private async void OnRemovePinClicked(object? sender, EventArgs e)
    {
        if (!await DisplayAlertAsync("Remove app lock?", "Anyone with access to this device session may open Finora after the PIN is removed.", "Remove", "Cancel")) return;
        await _lock.ClearPinAsync();
        await DisplayAlertAsync("App lock removed", "The local PIN has been removed.", "OK");
    }

    private async void OnDeleteAllClicked(object? sender, EventArgs e)
    {
        var confirmation = await DisplayPromptAsync("Delete all local finance data", "Type DELETE to permanently remove accounts, transactions, budgets, goals and other finance data from this app.", "Delete", "Cancel");
        if (!string.Equals(confirmation, "DELETE", StringComparison.Ordinal)) return;
        await _store.DeleteAllDataAsync();
        await DisplayAlertAsync("Local data deleted", "Finora finance data was deleted from this app. Settings and app-lock preferences are kept.", "OK");
    }

    private void OnVersionTapped(object? sender, TappedEventArgs e)
    {
        _versionTapCount++;
        if (_versionTapCount >= 7) DeveloperPanel.IsVisible = true;
    }

    private async void OnSchemaVersionClicked(object? sender, EventArgs e)
        => await DisplayAlertAsync("Database schema", $"Schema version: {Finora.Shared.AppConstants.DatabaseSchemaVersion}", "OK");

    private async void OnFeatureFlagsClicked(object? sender, EventArgs e)
    {
        var vm = (SettingsViewModel)BindingContext;
        await DisplayAlertAsync("Feature flags", $"LocalPremiumDemoEnabled={vm.LocalPremiumDemoEnabled}\nCloudSync=false\nTelemetry=false\nNotificationsIntegration=false", "OK");
    }

    private async void OnOnboardingClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//onboarding");
    private async void OnRepositoryClicked(object? sender, EventArgs e) => await Launcher.Default.OpenAsync("https://github.com/sanskarIN/Finora");
    private async void OnProfileClicked(object? sender, EventArgs e) => await Launcher.Default.OpenAsync("https://www.github.com/sanskarIN");
    private async void OnBusinessEmailClicked(object? sender, EventArgs e) => await ComposeEmailAsync("sanskarin@outlook.in", "Finora business inquiry");
    private async void OnSupportEmailClicked(object? sender, EventArgs e) => await ComposeEmailAsync("supportramsandesh@gmail.com", "Finora support");

    private async Task ComposeEmailAsync(string address, string subject)
    {
        try { await Email.Default.ComposeAsync(new EmailMessage { To = [address], Subject = subject }); }
        catch (Exception ex) { await DisplayAlertAsync("Email unavailable", ex.Message, "OK"); }
    }
}
