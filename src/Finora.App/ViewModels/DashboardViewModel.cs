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
    private string _reportingCurrencyNotice = string.Empty;
    private bool _privacyMode;
    private bool _showConfiguration;

    public DashboardViewModel(IFinanceStore store, IRecurringWorkflowService recurring, IAdvancedReportService reports, IAppSettingsService settings)
    {
        _store = store;
        _recurring = recurring;
        _reports = reports;
        _settings = settings;
        _privacyMode = settings.PrivacyMode || settings.HideAmountsOnLaunch;
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
    public string ReportingCurrencyNotice { get => _reportingCurrencyNotice; private set => SetProperty(ref _reportingCurrencyNotice, value); }
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
        OnPropertyChanged(nameof(ShowBalance));
        OnPropertyChanged(nameof(ShowIncomeExpense));
        OnPropertyChanged(nameof(ShowBudget));
        OnPropertyChanged(nameof(ShowUpcoming));
        OnPropertyChanged(nameof(ShowCategories));
        OnPropertyChanged(nameof(ShowGoals));
        OnPropertyChanged(nameof(ShowRecent));
        OnPropertyChanged(nameof(ShowCashFlow));

        var reportingCurrency = _settings.DefaultCurrency;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var startDay = Math.Min(_settings.FinancialMonthStartDay, 28);
        var previousMonth = today.AddMonths(-1);
        var monthStart = today.Day >= startDay
            ? new DateOnly(today.Year, today.Month, startDay)
            : new DateOnly(previousMonth.Year, previousMonth.Month, startDay);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var from = new DateTimeOffset(monthStart.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toExclusive = new DateTimeOffset(monthEnd.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var accounts = await _store.GetAccountsAsync();
        var reportingAccounts = accounts.Where(account => string.Equals(account.Currency, reportingCurrency, StringComparison.OrdinalIgnoreCase)).ToList();
        var otherCurrencies = accounts
            .Where(account => !string.Equals(account.Currency, reportingCurrency, StringComparison.OrdinalIgnoreCase))
            .Select(account => account.Currency)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(currency => currency, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var totalBalanceMinor = reportingAccounts.Aggregate(0L, (sum, account) => checked(sum + account.BalanceMinor));
        TotalBalance = Display(totalBalanceMinor, reportingCurrency);
        ReportingCurrencyNotice = otherCurrencies.Count == 0
            ? $"Dashboard totals use {reportingCurrency}."
            : $"Dashboard totals use {reportingCurrency}. Other account currencies ({string.Join(", ", otherCurrencies)}) are kept separate and are not converted or added to these totals.";

        var incomeExpense = await _reports.GetIncomeExpenseAsync(from, toExclusive, reportingCurrency);
        var incomeMinor = incomeExpense.Points.FirstOrDefault(point => string.Equals(point.Label, "Income", StringComparison.OrdinalIgnoreCase))?.ValueMinor ?? 0;
        var expenseMinor = incomeExpense.Points.FirstOrDefault(point => string.Equals(point.Label, "Expense", StringComparison.OrdinalIgnoreCase))?.ValueMinor ?? 0;
        Income = Display(incomeMinor, reportingCurrency);
        Expense = Display(expenseMinor, reportingCurrency);
        NetChange = Display(checked(incomeMinor - expenseMinor), reportingCurrency);

        var budgetPerformance = await _reports.GetBudgetPerformanceAsync(monthStart);
        var remainingBudgetMinor = budgetPerformance
            .Where(item => string.Equals(item.Currency, reportingCurrency, StringComparison.OrdinalIgnoreCase))
            .Aggregate(0L, (sum, item) => checked(sum + Math.Max(0L, item.VarianceMinor)));
        RemainingBudget = Display(remainingBudgetMinor, reportingCurrency);

        RecentTransactions.Clear();
        var recentTransactions = await _store.SearchTransactionsAsync(from: from, to: toExclusive.AddTicks(-1));
        foreach (var transaction in recentTransactions.OrderByDescending(item => item.OccurredAtUtc).Take(8))
        {
            RecentTransactions.Add(new DashboardTransactionItem(
                transaction.Id,
                transaction.Merchant ?? transaction.CategoryName ?? transaction.Type.ToString(),
                transaction.AccountName,
                transaction.OccurredAtUtc,
                Display(transaction.AmountMinor, transaction.Currency)));
        }

        TopCategories.Clear();
        var categories = await _reports.GetCategorySpendingAsync(from, toExclusive, reportingCurrency);
        foreach (var point in categories.Points.Take(5))
            TopCategories.Add(new DashboardAmountItem(point.Label, Display(point.ValueMinor, reportingCurrency), null));

        UpcomingItems.Clear();
        var upcoming = await _recurring.GetOccurrencesAsync(today, today.AddDays(30), includeCompleted: false);
        foreach (var item in upcoming.OrderBy(x => x.PostponedTo ?? x.DueOn).Take(8))
        {
            UpcomingItems.Add(new DashboardAmountItem(
                item.RuleName,
                Display(item.AmountMinor, item.Currency),
                (item.PostponedTo ?? item.DueOn).ToString("d", CultureInfo.CurrentCulture)));
        }

        Goals.Clear();
        foreach (var goal in (await _store.GetSavingsGoalsAsync()).OrderByDescending(x => x.Progress).Take(6))
        {
            Goals.Add(new DashboardGoalItem(
                goal.Name,
                PrivacyMode ? "••••" : $"{new Money(goal.CurrentMinor, goal.Currency).Format()} / {new Money(goal.TargetMinor, goal.Currency).Format()}",
                $"{Math.Clamp(goal.Progress * 100d, 0d, 100d):0}%"));
        }

        CashFlow.Clear();
        foreach (var row in await _reports.GetMonthlyComparisonAsync(6, reportingCurrency))
        {
            CashFlow.Add(new DashboardAmountItem(
                new DateTime(row.Year, row.Month, 1).ToString("MMM yyyy", CultureInfo.CurrentCulture),
                Display(row.NetMinor, reportingCurrency),
                row.NetMinor >= 0 ? "Net positive" : "Net negative"));
        }
    });

    public string Display(long minor, string? currency = null)
        => PrivacyMode ? "••••" : new Money(minor, currency ?? _settings.DefaultCurrency).Format();
}

public sealed record DashboardAmountItem(string Label, string Amount, string? Detail);
public sealed record DashboardGoalItem(string Name, string ProgressText, string PercentText);
public sealed record DashboardTransactionItem(Guid Id, string Label, string Account, DateTimeOffset OccurredAt, string Amount);
