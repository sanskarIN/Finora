using System.Collections.ObjectModel;
using Finora.Application;
using Finora.Domain;
namespace Finora.App;
public sealed class TransactionsViewModel : ViewModelBase
{
    private readonly IFinanceStore _store; private string _searchText=string.Empty,_amount=string.Empty,_merchant=string.Empty,_note=string.Empty; private TransactionType _type=TransactionType.Expense; private AccountSummary? _selectedAccount; private Category? _selectedCategory;
    public TransactionsViewModel(IFinanceStore store){_store=store;RefreshCommand=new AsyncCommand(LoadAsync);SearchCommand=new AsyncCommand(SearchAsync);AddCommand=new AsyncCommand(AddAsync);}
    public ObservableCollection<TransactionListItem> Transactions{get;}=[]; public ObservableCollection<AccountSummary> Accounts{get;}=[]; public ObservableCollection<Category> Categories{get;}=[]; public IReadOnlyList<TransactionType> TransactionTypes{get;}=[TransactionType.Expense,TransactionType.Income,TransactionType.Refund,TransactionType.Adjustment];
    public string SearchText{get=>_searchText;set=>SetProperty(ref _searchText,value);} public TransactionType Type{get=>_type;set=>SetProperty(ref _type,value);} public AccountSummary? SelectedAccount{get=>_selectedAccount;set=>SetProperty(ref _selectedAccount,value);} public Category? SelectedCategory{get=>_selectedCategory;set=>SetProperty(ref _selectedCategory,value);} public string Amount{get=>_amount;set=>SetProperty(ref _amount,value);} public string Merchant{get=>_merchant;set=>SetProperty(ref _merchant,value);} public string Note{get=>_note;set=>SetProperty(ref _note,value);}
    public System.Windows.Input.ICommand RefreshCommand{get;} public System.Windows.Input.ICommand SearchCommand{get;} public System.Windows.Input.ICommand AddCommand{get;}
    public Task LoadAsync()=>RunAsync(async()=>{await LoadLookupsAsync();await SearchCoreAsync(null);}); private Task SearchAsync()=>RunAsync(()=>SearchCoreAsync(SearchText));
    private Task AddAsync()=>RunAsync(async()=>{if(SelectedAccount is null)throw new InvalidOperationException("Choose an account.");if(!decimal.TryParse(Amount,out var major)||major<=0)throw new InvalidOperationException("Enter a positive amount.");var tx=TransactionFactory.Create(Type,Money.FromMajorUnits(major,SelectedAccount.Currency).MinorUnits,SelectedAccount.Currency,SelectedAccount.Id,DateTimeOffset.UtcNow,SelectedCategory?.Id,string.IsNullOrWhiteSpace(Merchant)?null:Merchant.Trim(),string.IsNullOrWhiteSpace(Note)?null:Note.Trim());await _store.SaveTransactionAsync(tx);Amount=Merchant=Note=string.Empty;await SearchCoreAsync(SearchText);});
    private async Task LoadLookupsAsync(){Accounts.Clear();foreach(var a in await _store.GetAccountsAsync())Accounts.Add(a);Categories.Clear();foreach(var c in await _store.GetCategoriesAsync())if(!c.IsArchived)Categories.Add(c);SelectedAccount??=Accounts.FirstOrDefault();}
    private async Task SearchCoreAsync(string? query){Transactions.Clear();foreach(var item in await _store.SearchTransactionsAsync(string.IsNullOrWhiteSpace(query)?null:query.Trim()))Transactions.Add(item);}
}
