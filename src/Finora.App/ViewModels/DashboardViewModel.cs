using System.Collections.ObjectModel;
using System.Globalization;
using Finora.Application;
using Finora.Domain;

namespace Finora.App;

public sealed class DashboardViewModel : ViewModelBase
{
    private readonly IFinanceStore _store;
    private readonly IRecurringWorkflowService _recurring;
    private readonly IAdvancedReportService _reports;
    private readonly IAppSettingsService _settings;
    private string _totalBalance = "—";
    private string _income = "—";
    private string _expense = "—";
    private string _netChange = "—";
    private string _remainingBudget = "—";
    private bool _privacyMode;
    private bool _showConfiguration;

    public DashboardViewModel(IFinanceStore store, IRecurringWorkflowService recurring, IAdvancedReportService reports, IAppSettingsService settings)
    {
        _store = store; _recurring = recurring; _reports = reports; _settings = settings; _privacyMode = settings.PrivacyMode || settings.HideAmountsOnLaunch;
        RefreshCommand = new AsyncCommand(LoadAsync);
        TogglePrivacyCommand = new Command(() => { PrivacyMode = !PrivacyMode; _settings.PrivacyMode = PrivacyMode; _ = LoadAsync(); });
        ToggleConfigurationCommand = new Command(() => ShowConfiguration = !ShowConfiguration);
    }

    public ObservableCollection<DashboardTransactionItem> RecentTransactions { get; } = [];
    public ObservableCollection<DashboardAmountItem> TopCategories { get; } = [];
    public ObservableCollection<DashboardAmountItem> UpcomingItems { get; } = [];
    public ObservableCollection<DashboardGoalItem> Goals { get; } = [];
    public ObservableCollection<DashboardAmountItem> CashFlow { get; } = [];
    public string TotalBalance { get => _totalBalance; private set => SetProperty(ref _totalBalance, value); }
    public string Income { get => _income; private set => SetProperty(ref _income, value); }
    public string Expense { get => _expense; private set => SetProperty(ref _expense, value); }
    public string NetChange { get => _netChange; private set => SetProperty(ref _netChange, value); }
    public string RemainingBudget { get => _remainingBudget; private set => SetProperty(ref _remainingBudget, value); }
    public bool PrivacyMode { get => _privacyMode; set => SetProperty(ref _privacyMode, value); }
    public bool ShowConfiguration { get => _showConfiguration; set => SetProperty(ref _showConfiguration, value); }
    public bool ShowBalance { get => _settings.DashboardShowBalance; set { _settings.DashboardShowBalance = value; OnPropertyChanged(); } }
    public bool ShowIncomeExpense { get => _settings.DashboardShowIncomeExpense; set { _settings.DashboardShowIncomeExpense = value; OnPropertyChanged(); } }
    public bool ShowBudget { get => _settings.DashboardShowBudget; set { _settings.DashboardShowBudget = value; OnPropertyChanged(); } }
    public bool ShowUpcoming { get => _settings.DashboardShowUpcoming; set { _settings.DashboardShowUpcoming = value; OnPropertyChanged(); } }
    public bool ShowCategories { get => _settings.DashboardShowCategories; set { _settings.DashboardShowCategories = value; OnPropertyChanged(); } }
    public bool ShowGoals { get => _settings.DashboardShowGoals; set { _settings.DashboardShowGoals = value; OnPropertyChanged(); } }
    public bool ShowRecent { get => _settings.DashboardShowRecent; set { _settings.DashboardShowRecent = value; OnPropertyChanged(); } }
    public bool ShowCashFlow { get => _settings.DashboardShowCashFlow; set { _settings.DashboardShowCashFlow = value; OnPropertyChanged(); } }
    public System.Windows.Input.ICommand RefreshCommand { get; }
    public System.Windows.Input.ICommand TogglePrivacyCommand { get; }
    public System.Windows.Input.ICommand ToggleConfigurationCommand { get; }

    public Task LoadAsync() => RunAsync(async () =>
    {
        OnPropertyChanged(nameof(ShowBalance)); OnPropertyChanged(nameof(ShowIncomeExpense)); OnPropertyChanged(nameof(ShowBudget)); OnPropertyChanged(nameof(ShowUpcoming)); OnPropertyChanged(nameof(ShowCategories)); OnPropertyChanged(nameof(ShowGoals)); OnPropertyChanged(nameof(ShowRecent)); OnPropertyChanged(nameof(ShowCashFlow));
        var today = DateOnly.FromDateTime(DateTime.Today); var startDay = Math.Min(_settings.FinancialMonthStartDay, 28); var previousMonth = today.AddMonths(-1); var monthStart = today.Day >= startDay ? new DateOnly(today.Year, today.Month, startDay) : new DateOnly(previousMonth.Year, previousMonth.Month, startDay); var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var snapshot = await _store.GetDashboardAsync(monthStart, monthEnd);
        TotalBalance = Display(snapshot.TotalBalanceMinor); Income = Display(snapshot.IncomeMinor); Expense = Display(Math.Abs(snapshot.ExpenseMinor)); NetChange = Display(snapshot.NetChangeMinor); RemainingBudget = Display(snapshot.RemainingBudgetMinor);
        RecentTransactions.Clear(); foreach (var tx in snapshot.RecentTransactions) RecentTransactions.Add(new DashboardTransactionItem(tx.Id, tx.Merchant ?? tx.CategoryName ?? tx.Type.ToString(), tx.AccountName, tx.OccurredAtUtc, Display(tx.AmountMinor)));
        TopCategories.Clear(); foreach (var item in snapshot.TopCategories) TopCategories.Add(new DashboardAmountItem(item.CategoryName, Display(Math.Abs(item.AmountMinor)), null));
        UpcomingItems.Clear(); var upcoming = await _recurring.GetOccurrencesAsync(today, today.AddDays(30), includeCompleted: false); foreach (var item in upcoming.OrderBy(x => x.PostponedTo ?? x.DueOn).Take(8)) UpcomingItems.Add(new DashboardAmountItem(item.RuleName, Display(Math.Abs(item.AmountMinor)), (item.PostponedTo ?? item.DueOn).ToString("d", CultureInfo.CurrentCulture)));
        Goals.Clear(); foreach (var goal in (await _store.GetSavingsGoalsAsync()).OrderByDescending(x => x.Progress).Take(6)) Goals.Add(new DashboardGoalItem(goal.Name, PrivacyMode ? "••••" : $"{new Money(goal.CurrentMinor, goal.Currency).Format()} / {new Money(goal.TargetMinor, goal.Currency).Format()}", $"{Math.Clamp(goal.Progress * 100d, 0d, 100d):0}%"));
        CashFlow.Clear(); foreach (var row in await _reports.GetMonthlyComparisonAsync(6, _settings.DefaultCurrency)) CashFlow.Add(new DashboardAmountItem(new DateTime(row.Year, row.Month, 1).ToString("MMM yyyy", CultureInfo.CurrentCulture), Display(row.NetMinor), row.NetMinor >= 0 ? "Net positive" : "Net negative"));
    });

    public string Display(long minor) => PrivacyMode ? "••••" : new Money(minor, _settings.DefaultCurrency).Format();
}

public sealed record DashboardAmountItem(string Label, string Amount, string? Detail);
public sealed record DashboardGoalItem(string Name, string ProgressText, string PercentText);
public sealed record DashboardTransactionItem(Guid Id, string Label, string Account, DateTimeOffset OccurredAt, string Amount);
