namespace Finora.App;

public partial class SettingsPage
{
    private async void OnContributingClicked(object? sender, EventArgs e)
        => await OpenProjectDocumentAsync("CONTRIBUTING.md", "Contributing guide");

    private async void OnSecurityGuideClicked(object? sender, EventArgs e)
        => await OpenProjectDocumentAsync("SECURITY.md", "Security reporting guide");

    private async void OnSupportGuideClicked(object? sender, EventArgs e)
        => await OpenProjectDocumentAsync("SUPPORT.md", "Support guide");

    private async Task OpenProjectDocumentAsync(string fileName, string title)
    {
        try
        {
            var uri = new Uri($"https://github.com/sanskarIN/Finora/blob/main/{Uri.EscapeDataString(fileName)}");
            var opened = await Launcher.Default.OpenAsync(uri);
            if (!opened) await DisplayAlertAsync(title, "The document could not be opened with the available browser or system handler.", "OK");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Settings.ProjectDocumentOpenFailed");
            await DisplayAlertAsync(title, "The document could not be opened right now.", "OK");
        }
    }
}
