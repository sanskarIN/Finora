using Finora.Application;

namespace Finora.App;

public partial class SettingsPage
{
    private readonly ISampleDataService _sampleData = ServiceHelper.Get<ISampleDataService>();

    private async void OnResetSampleDataClicked(object? sender, EventArgs e)
    {
        var confirmation = await DisplayPromptAsync(
            "Reset to synthetic sample data",
            "This developer action permanently deletes all current local finance data, then creates a deterministic synthetic demo dataset. App preferences and app-lock settings remain. Type RESET SAMPLE to continue.",
            "Reset",
            "Cancel");

        if (!string.Equals(confirmation?.Trim(), "RESET SAMPLE", StringComparison.Ordinal)) return;

        var result = await _sampleData.ResetToSyntheticSampleDataAsync(_settings.DefaultCurrency);
        if (!result.IsSuccess || result.Value is null)
        {
            await DisplayAlertAsync("Sample reset failed", result.Error ?? "Finora could not create the synthetic sample dataset safely.", "OK");
            return;
        }

        await _attachments.CleanupOrphanedFilesAsync();
        if (_settings.NotificationsEnabled) await _reminders.SyncAsync();
        var sample = result.Value;
        await DisplayAlertAsync(
            "Synthetic sample data ready",
            $"Created {sample.AccountsCreated} accounts, {sample.TransactionsCreated} transaction rows, {sample.BudgetsCreated} budget, {sample.GoalsCreated} savings goal, and {sample.RecurrenceRulesCreated} recurring rule. All values are synthetic.",
            "OK");

        await LoadSurfaceAsync();
    }
}
