using System.Collections.ObjectModel;
using Finora.Application;
using Finora.Domain;
namespace Finora.App;
public sealed class BudgetsViewModel:ViewModelBase
{
 private readonly IFinanceStore _store; private readonly IAppSettingsService _settings; private string _name="",_limit=""; private Category? _category; private int _warningThreshold=80; private bool _rollover;
 public BudgetsViewModel(IFinanceStore store,IAppSettingsService settings){_store=store;_settings=settings;RefreshCommand=new AsyncCommand(LoadAsync);AddCommand=new AsyncCommand(AddAsync);}
 public ObservableCollection<BudgetSnapshot> Budgets{get;}=[]; public ObservableCollection<Category> Categories{get;}=[]; public string Name{get=>_name;set=>SetProperty(ref _name,value);} public string Limit{get=>_limit;set=>SetProperty(ref _limit,value);} public Category? Category{get=>_category;set=>SetProperty(ref _category,value);} public int WarningThreshold{get=>_warningThreshold;set=>SetProperty(ref _warningThreshold,Math.Clamp(value,1,100));} public bool Rollover{get=>_rollover;set=>SetProperty(ref _rollover,value);} public System.Windows.Input.ICommand RefreshCommand{get;} public System.Windows.Input.ICommand AddCommand{get;}
 public Task LoadAsync()=>RunAsync(async()=>{Categories.Clear();foreach(var c in await _store.GetCategoriesAsync())if(!c.IsArchived)Categories.Add(c);await LoadBudgetsCoreAsync();});
 private Task AddAsync()=>RunAsync(async()=>{if(string.IsNullOrWhiteSpace(Name))throw new InvalidOperationException("Budget name is required.");if(!decimal.TryParse(Limit,out var major)||major<=0)throw new InvalidOperationException("Enter a positive budget amount.");await _store.SaveBudgetAsync(new Budget{Name=Name.Trim(),Kind=Category is null?BudgetKind.Overall:BudgetKind.Category,Cadence=BudgetCadence.Monthly,CategoryId=Category?.Id,LimitMinor=Money.FromMajorUnits(major,_settings.DefaultCurrency).MinorUnits,Currency=_settings.DefaultCurrency,RolloverEnabled=Rollover,WarningThresholdPercent=WarningThreshold});Name=Limit="";await LoadBudgetsCoreAsync();});
 private async Task LoadBudgetsCoreAsync(){Budgets.Clear();foreach(var b in await _store.GetBudgetsAsync(DateOnly.FromDateTime(DateTime.Today)))Budgets.Add(b);}
}
