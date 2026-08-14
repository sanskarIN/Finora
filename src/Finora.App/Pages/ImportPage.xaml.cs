using Finora.Application;
namespace Finora.App;
public partial class ImportPage : ContentPage
{
    private ImportViewModel ViewModel => (ImportViewModel)BindingContext;
    public ImportPage() { InitializeComponent(); BindingContext = new ImportViewModel(ServiceHelper.Get<ICsvImportService>(), ServiceHelper.Get<IFinanceStore>(), ServiceHelper.Get<IAppSettingsService>()); }
    protected override void OnAppearing() { base.OnAppearing(); _ = ViewModel.LoadAccountsAsync(); }
    private async void OnChooseCsvClicked(object? sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Choose a CSV file" });
            if (result is null) return;
            await using var input = await result.OpenReadAsync();
            using var buffer = new MemoryStream();
            await input.CopyToAsync(buffer);
            await ViewModel.LoadFileAsync(buffer.ToArray(), result.FileName);
        }
        catch (Exception)
        {
            await DisplayAlertAsync("CSV import", "The selected CSV file could not be opened or read. Verify that the file is accessible and try again.", "OK");
        }
    }
}
