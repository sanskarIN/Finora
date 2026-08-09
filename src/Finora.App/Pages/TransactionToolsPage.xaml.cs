using Finora.Application;

namespace Finora.App;

public partial class TransactionToolsPage : ContentPage
{
    private readonly IExportService _export = ServiceHelper.Get<IExportService>();
    private TransactionToolsViewModel ViewModel => (TransactionToolsViewModel)BindingContext;
    public TransactionToolsPage()
    {
        InitializeComponent();
        BindingContext = new TransactionToolsViewModel(ServiceHelper.Get<IFinanceStore>(), ServiceHelper.Get<ITransactionMaintenanceService>());
    }
    protected override void OnAppearing() { base.OnAppearing(); _ = ViewModel.LoadAsync(); }

    private async void OnExportSelectedCsvClicked(object? sender, EventArgs e)
    {
        var ids = ViewModel.Transactions.Where(x => x.IsSelected).Select(x => x.Id).ToArray();
        if (ids.Length == 0) { await DisplayAlertAsync("Nothing selected", "Select one or more transactions first.", "OK"); return; }
        try { var csv = await _export.ExportTransactionsCsvAsync(ids); var path = Path.Combine(FileSystem.CacheDirectory, $"Finora-selected-{DateTime.Now:yyyyMMdd-HHmmss}.csv"); await File.WriteAllTextAsync(path, csv); await Share.Default.RequestAsync(new ShareFileRequest("Export selected Finora transactions", new ShareFile(path))); }
        catch (Exception ex) { await DisplayAlertAsync("Export failed", ex.Message, "OK"); }
    }

    private async void OnExportSelectedPdfClicked(object? sender, EventArgs e)
    {
        var ids = ViewModel.Transactions.Where(x => x.IsSelected).Select(x => x.Id).ToArray();
        if (ids.Length == 0) { await DisplayAlertAsync("Nothing selected", "Select one or more transactions first.", "OK"); return; }
        try { var pdf = await _export.ExportTransactionsPdfAsync(ids); var path = Path.Combine(FileSystem.CacheDirectory, $"Finora-selected-{DateTime.Now:yyyyMMdd-HHmmss}.pdf"); await File.WriteAllBytesAsync(path, pdf); await Share.Default.RequestAsync(new ShareFileRequest("Export selected Finora transactions", new ShareFile(path))); }
        catch (Exception ex) { await DisplayAlertAsync("Export failed", ex.Message, "OK"); }
    }

    private async void OnDuplicateSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not DuplicateTransactionCandidate item) return;
        DuplicateList.SelectedItem = null;
        await Shell.Current.GoToAsync($"{nameof(TransactionDetailPage)}?transactionId={item.TransactionId}");
    }
}
