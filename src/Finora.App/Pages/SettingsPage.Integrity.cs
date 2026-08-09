using Finora.Application;

namespace Finora.App;

public partial class SettingsPage
{
    private readonly IDataIntegrityService _integrity = ServiceHelper.Get<IDataIntegrityService>();

    private async void OnIntegrityCheckClicked(object? sender, EventArgs e)
    {
        try
        {
            var report = await _integrity.CheckAsync();
            var message = report.IsHealthy
                ? $"Local integrity checks passed. Accounts checked: {report.AccountsChecked}. Transactions checked: {report.TransactionsChecked}. Attachments checked: {report.AttachmentsChecked}."
                : $"Finora found {report.Issues.Count} integrity issue(s). No private finance contents are included in the report.";

            var export = await DisplayAlertAsync(
                report.IsHealthy ? "Integrity check passed" : "Integrity check needs attention",
                message,
                "Export sanitized report",
                "Close");

            if (!export)
                return;

            var path = Path.Combine(FileSystem.CacheDirectory, $"Finora-integrity-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt");
            await File.WriteAllTextAsync(path, report.ToSanitizedText());
            await Share.Default.RequestAsync(new ShareFileRequest("Finora sanitized integrity report", new ShareFile(path)));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "integrity_check_failed");
            await DisplayAlertAsync("Integrity check failed", "Finora could not complete the local integrity check. A sanitized diagnostic event was recorded.", "OK");
        }
    }
}
