using System.Collections.ObjectModel;
using Finora.Application;
using Finora.Domain;

namespace Finora.App;

public sealed class DashboardViewModel : ViewModelBase
{
    private readonly IFinanceStore _store;
    private readonly IAppSettingsService _settings;
    private string _totalBalance = "—", _income = "—", _expense = "—", _netChange = "—", _remainingBudget = "—";
    private bool _privacyMode;
    public DashboardViewModel(IFinanceStore store, IAppSettingsService settings)
    {
        _store = store; _settings = settings; _privacyMode = settings.PrivacyMode || settings.HideAmountsOnLaunch;
        RefreshCommand = new AsyncCommand(LoadAsync);
        TogglePrivacyCommand = new Command(() => { PrivacyMode = !PrivacyMode; _settings.PrivacyMode = PrivacyMode; _ = LoadAsync(); });
    }
    public ObservableCollection<TransactionListItem> RecentTransactions { get; } = [];
    public ObservableCollection<CategorySpend> TopCategories { get; } = [];
    public string TotalBalance { get => _totalBalance; private set => SetProperty(ref _totalBalance, value); }
    public string Income { get => _income; private set => SetProperty(ref _income, value); }
    public string Expense { get => _expense; private set => SetProperty(ref _expense, value); }
    public string NetChange { get => _netChange; private set => SetProperty(ref _netChange, value); }
    public string RemainingBudget { get => _remainingBudget; private set => SetProperty(ref _remainingBudget, value); }
    public bool PrivacyMode { get => _privacyMode; set => SetProperty(ref _privacyMode, value); }
    public System.Windows.Input.ICommand RefreshCommand { get; }
    public System.Windows.Input.ICommand TogglePrivacyCommand { get; }
    public Task LoadAsync() => RunAsync(async () =>
    {
        var today = DateOnly.FromDateTime(DateTime.Today); var startDay = Math.Min(_settings.FinancialMonthStartDay, 28);
        var monthStart = today.Day >= startDay ? new DateOnly(today.Year, today.Month, startDay) : new DateOnly(today.AddMonths(-1).Year, today.AddMonths(-1).Month, startDay);
        var s = await _store.GetDashboardAsync(monthStart, monthStart.AddMonths(1).AddDays(-1));
        TotalBalance = Display(s.TotalBalanceMinor); Income = Display(s.IncomeMinor); Expense = Display(Math.Abs(s.ExpenseMinor)); NetChange = Display(s.NetChangeMinor); RemainingBudget = Display(s.RemainingBudgetMinor);
        Replace(RecentTransactions, s.RecentTransactions); Replace(TopCategories, s.TopCategories);
    });
    public string Display(long minor) => PrivacyMode ? "••••" : new Money(minor, _settings.DefaultCurrency).Format();
    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source) { target.Clear(); foreach (var item in source) target.Add(item); }
}
