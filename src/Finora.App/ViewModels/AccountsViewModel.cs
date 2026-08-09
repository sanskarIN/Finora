using System.Collections.ObjectModel;
using Finora.Application;
using Finora.Domain;
namespace Finora.App;
public sealed class AccountsViewModel : ViewModelBase
{
    private readonly IFinanceStore _store; private readonly IAppSettingsService _settings;
    private string _name = string.Empty, _openingBalance = "0", _transferAmount = string.Empty, _transferNote = string.Empty;
    private AccountType _accountType = AccountType.Bank; private AccountSummary? _sourceAccount, _destinationAccount;
    public AccountsViewModel(IFinanceStore store, IAppSettingsService settings) { _store=store; _settings=settings; RefreshCommand=new AsyncCommand(LoadAsync); AddAccountCommand=new AsyncCommand(AddAccountAsync); TransferCommand=new AsyncCommand(TransferAsync); }
    public ObservableCollection<AccountSummary> Accounts { get; }=[]; public IReadOnlyList<AccountType> AccountTypes { get; }=Enum.GetValues<AccountType>();
    public string Name { get=>_name; set=>SetProperty(ref _name,value); } public AccountType AccountType { get=>_accountType; set=>SetProperty(ref _accountType,value); } public string OpeningBalance { get=>_openingBalance; set=>SetProperty(ref _openingBalance,value); }
    public AccountSummary? SourceAccount { get=>_sourceAccount; set=>SetProperty(ref _sourceAccount,value); } public AccountSummary? DestinationAccount { get=>_destinationAccount; set=>SetProperty(ref _destinationAccount,value); } public string TransferAmount { get=>_transferAmount; set=>SetProperty(ref _transferAmount,value); } public string TransferNote { get=>_transferNote; set=>SetProperty(ref _transferNote,value); }
    public System.Windows.Input.ICommand RefreshCommand { get; } public System.Windows.Input.ICommand AddAccountCommand { get; } public System.Windows.Input.ICommand TransferCommand { get; }
    public Task LoadAsync()=>RunAsync(LoadCoreAsync);
    private Task AddAccountAsync()=>RunAsync(async()=>{ if(string.IsNullOrWhiteSpace(Name)) throw new InvalidOperationException("Account name is required."); if(!decimal.TryParse(OpeningBalance,out var opening)) throw new InvalidOperationException("Enter a valid opening balance."); await _store.SaveAccountAsync(new Account{Name=Name.Trim(),Type=AccountType,Currency=_settings.DefaultCurrency,OpeningBalanceMinor=Money.FromMajorUnits(opening,_settings.DefaultCurrency).MinorUnits}); Name=string.Empty; OpeningBalance="0"; await LoadCoreAsync(); });
    private Task TransferAsync()=>RunAsync(async()=>{ if(SourceAccount is null||DestinationAccount is null) throw new InvalidOperationException("Choose both accounts."); if(SourceAccount.Id==DestinationAccount.Id) throw new InvalidOperationException("Choose two different accounts."); if(!decimal.TryParse(TransferAmount,out var major)||major<=0) throw new InvalidOperationException("Enter a positive transfer amount."); await _store.RecordTransferAsync(SourceAccount.Id,DestinationAccount.Id,Money.FromMajorUnits(major,SourceAccount.Currency).MinorUnits,DateTimeOffset.UtcNow,string.IsNullOrWhiteSpace(TransferNote)?null:TransferNote.Trim()); TransferAmount=TransferNote=string.Empty; await LoadCoreAsync(); });
    private async Task LoadCoreAsync(){ Accounts.Clear(); foreach(var account in await _store.GetAccountsAsync()) Accounts.Add(account); }
}
