using Finora.Application;

namespace Finora.App;

[QueryProperty(nameof(AccountId), "accountId")]
public partial class AccountDetailPage : ContentPage
{
    private AccountDetailViewModel ViewModel => (AccountDetailViewModel)BindingContext;
    private string? _accountId;
    public AccountDetailPage()
    {
        InitializeComponent();
        BindingContext = new AccountDetailViewModel(ServiceHelper.Get<IAccountManagementService>(), ServiceHelper.Get<IFinanceStore>());
    }
    public string? AccountId { get => _accountId; set { _accountId = value; if (Guid.TryParse(Uri.UnescapeDataString(value ?? string.Empty), out var id)) _ = ViewModel.LoadAsync(id); } }
    private async void OnTransactionSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not TransactionListItem item) return;
        AccountTransactions.SelectedItem = null;
        await Shell.Current.GoToAsync($"{nameof(TransactionDetailPage)}?transactionId={item.Id}");
    }
    private async void OnReconcileClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(ReconciliationPage));
}
