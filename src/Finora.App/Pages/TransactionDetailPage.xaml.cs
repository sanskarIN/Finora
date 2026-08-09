using Finora.Application;

namespace Finora.App;

[QueryProperty(nameof(TransactionId), "transactionId")]
public partial class TransactionDetailPage : ContentPage
{
    private readonly IAttachmentService _attachments = ServiceHelper.Get<IAttachmentService>();
    private TransactionDetailViewModel ViewModel => (TransactionDetailViewModel)BindingContext;
    private string? _transactionId;

    public TransactionDetailPage()
    {
        InitializeComponent();
        BindingContext = new TransactionDetailViewModel(ServiceHelper.Get<ITransactionMaintenanceService>(), ServiceHelper.Get<IFinanceStore>(), ServiceHelper.Get<ICategoryTagService>());
    }

    public string? TransactionId
    {
        get => _transactionId;
        set { _transactionId = value; if (Guid.TryParse(Uri.UnescapeDataString(value ?? string.Empty), out var id)) _ = ViewModel.LoadAsync(id); }
    }

    private async void OnAddAttachmentClicked(object? sender, EventArgs e)
    {
        if (ViewModel.TransactionId == Guid.Empty) return;
        try
        {
            var picked = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Choose a receipt image or PDF" });
            if (picked is null) return;
            await using var source = await picked.OpenReadAsync();
            var contentType = string.IsNullOrWhiteSpace(picked.ContentType) ? GuessContentType(picked.FileName) : picked.ContentType;
            var result = await _attachments.AddAttachmentAsync(ViewModel.TransactionId, source, picked.FileName, contentType);
            if (!result.IsSuccess) await DisplayAlertAsync("Attachment not added", result.Error ?? "The attachment could not be stored.", "OK");
            await ViewModel.ReloadAsync();
        }
        catch (Exception ex) { await DisplayAlertAsync("Attachment failed", ex.Message, "OK"); }
    }

    private async void OnOpenAttachmentClicked(object? sender, EventArgs e)
    {
        if (ViewModel.SelectedAttachment is null) { await DisplayAlertAsync("Choose an attachment", "Select an attachment first.", "OK"); return; }
        var path = await _attachments.GetLocalPathAsync(ViewModel.SelectedAttachment.Id);
        if (!path.IsSuccess || string.IsNullOrWhiteSpace(path.Value)) { await DisplayAlertAsync("Attachment unavailable", path.Error ?? "The local file was not found.", "OK"); return; }
        await Launcher.Default.OpenAsync(new OpenFileRequest("Open receipt", new ReadOnlyFile(path.Value)));
    }

    private async void OnDeleteAttachmentClicked(object? sender, EventArgs e)
    {
        if (ViewModel.SelectedAttachment is null) return;
        if (!await DisplayAlertAsync("Delete receipt?", "The local attachment file will be permanently deleted.", "Delete", "Cancel")) return;
        var result = await _attachments.DeleteAttachmentAsync(ViewModel.SelectedAttachment.Id);
        if (!result.IsSuccess) await DisplayAlertAsync("Delete failed", result.Error ?? "The attachment could not be deleted.", "OK");
        await ViewModel.ReloadAsync();
    }

    private async void OnDeleteOrRestoreClicked(object? sender, EventArgs e)
    {
        if (ViewModel.IsDeleted) ViewModel.RestoreCommand.Execute(null);
        else if (await DisplayAlertAsync("Delete transaction?", "The transaction will be soft-deleted and can be restored from this screen.", "Delete", "Cancel")) ViewModel.DeleteCommand.Execute(null);
    }

    private async void OnRefreshClicked(object? sender, EventArgs e) => await ViewModel.ReloadAsync();

    private static string GuessContentType(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".heic" => "image/heic",
        ".heif" => "image/heif",
        ".pdf" => "application/pdf",
        _ => "image/jpeg"
    };
}
