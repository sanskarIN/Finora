namespace Finora.App;
[QueryProperty(nameof(Document), "document")]
public partial class LegalPage : ContentPage
{
    private string _document = "privacy";
    public string Document { get => _document; set { _document = string.IsNullOrWhiteSpace(value) ? "privacy" : Uri.UnescapeDataString(value).Trim().ToLowerInvariant(); _ = LoadAsync(); } }
    public LegalPage() => InitializeComponent();
    protected override void OnAppearing() { base.OnAppearing(); _ = LoadAsync(); }
    private async Task LoadAsync() { var (title, asset) = Document switch { "terms" => ("Terms", "terms.txt"), "notices" => ("Third-party notices", "third-party-notices.txt"), _ => ("Privacy", "privacy.txt") }; try { await using var stream = await FileSystem.Current.OpenAppPackageFileAsync(asset); using var reader = new StreamReader(stream); DocumentTitle.Text = title; DocumentText.Text = await reader.ReadToEndAsync(); } catch (Exception ex) { DocumentTitle.Text = title; DocumentText.Text = $"Unable to open the packaged document: {ex.Message}"; } }
}
