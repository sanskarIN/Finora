using Finora.Application;

namespace Finora.App;

public partial class SettingsPage
{
    private readonly IFinanceDataResetService _financeReset = ServiceHelper.Get<IFinanceDataResetService>();

    private async void OnDeleteAllFinanceDataClicked(object? sender, EventArgs e)
    {
        var confirmation = await DisplayPromptAsync(
            "Delete all local finance data",
            "Type DELETE to permanently remove accounts, transactions, categories, tags, budgets, goals, recurring items, receipts, reconciliation history, reminder records, and other finance data from this app. App preferences and app-lock settings are kept.",
            "Delete",
            "Cancel");

        if (!string.Equals(confirmation, "DELETE", StringComparison.Ordinal)) return;

        var result = await _financeReset.DeleteAllFinanceDataAsync();
        if (!result.IsSuccess || result.Value is null)
        {
            await DisplayAlertAsync("Delete failed", result.Error ?? "Finora could not safely delete all finance data.", "OK");
            return;
        }

        var orphanedFiles = await _attachments.CleanupOrphanedFilesAsync();
        var deleted = result.Value;
        await DisplayAlertAsync(
            "Local finance data deleted",
            $"Deleted {deleted.Accounts} account(s), {deleted.Transactions} transaction(s), {deleted.Categories} category/categories, {deleted.Tags} tag(s), {deleted.Budgets} budget(s), {deleted.SavingsGoals} goal(s), {deleted.RecurrenceRules} recurring rule(s), and {deleted.Attachments} attachment record(s). Removed {orphanedFiles} orphaned receipt file(s). App preferences and app-lock settings were kept.",
            "OK");

        await LoadSurfaceAsync();
    }
}
