using System.Collections.ObjectModel;
using System.Globalization;
using Finora.Application;
using Finora.Domain;
using Finora.Shared;

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
    private string _currencyScope = string.Empty;
    private string _periodRange = string.Empty;
    private DashboardPeriodChoice _selectedPeriod;
    private bool _privacyMode;
    private bool _showConfiguration;

    public DashboardViewModel(IFinanceStore store, IRecurringWorkflowService recurring, IAdvancedReportService reports, IAppSettingsService settings)
    {
        _store = store;
        _recurring = recurring;
        _reports = reports;
        _settings = settings;
        _privacyMode = settings.PrivacyMode || settings.HideAmountsOnLaunch;
        PeriodChoices = Enum.GetValues<DashboardPeriod>()
            .Select(period => new DashboardPeriodChoice(period, DashboardPeriodPolicy.GetLabel(period)))
            .ToArray();
        _selectedPeriod = PeriodChoices[0];
        RefreshCommand = new AsyncCommand(LoadAsync);
        TogglePrivacyCommand = new Command(() => { PrivacyMode = !PrivacyMode; _settings.PrivacyMode = PrivacyMode; _ = LoadAsync(); });
        ToggleConfigurationCommand = new Command(() => ShowConfiguration = !ShowConfiguration);
    }

    public ObservableCollection<DashboardTransactionItem> RecentTransactions { get; } = [];
    public ObservableCollection<DashboardAmountItem> TopCategories { get; } = [];
    public ObservableCollection<DashboardAmountItem> UpcomingItems { get; } = [];
    public ObservableCollection<DashboardGoalItem> Goals { get; } = [];
    public ObservableCollection<DashboardAmountItem> CashFlow { get; } = [];
    public IReadOnlyList<DashboardPeriodChoice> PeriodChoices { get; }
    public DashboardPeriodChoice SelectedPeriod { get => _selectedPeriod; set => SetProperty(ref _selectedPeriod, value ?? PeriodChoices[0]); }
    public string PeriodRange { get => _periodRange; private set => SetProperty(ref _periodRange, value); }
    public string TotalBalance { get => _totalBalance; private set => SetProperty(ref _totalBalance, value); }
    public string Income { get => _income; private set => SetProperty(ref _income, value); }
    public string Expense { get => _expense; private set => SetProperty(ref _expense, value); }
    public string NetChange { get => _netChange; private set => SetProperty(ref _netChange, value); }
    public string RemainingBudget { get => _remainingBudget; private set => SetProperty(ref _remainingBudget, value); }
    public string CurrencyScope { get => _currencyScope; private set => SetProperty(ref _currencyScope, value); }
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

        var today = DateOnly.FromDateTime(DateTime.Today);
        var dateRange = DashboardPeriodPolicy.Resolve(SelectedPeriod.Period, today, _settings.FinancialMonthStartDay);
        var utcRange = LocalDateRange.ToUtc(dateRange.From, dateRange.Through, TimeZoneInfo.Local);
        var from = utcRange.FromUtc;
        var toExclusive = utcRange.ToExclusiveUtc;
        var reportingCurrency = _settings.DefaultCurrency;
        PeriodRange = $"{SelectedPeriod.Label}: {dateRange.From.ToString("d", CultureInfo.CurrentCulture)} – {dateRange.Through.ToString("d", CultureInfo.CurrentCulture)}";

        var incomeExpense = await _reports.GetIncomeExpenseAsync(from, toExclusive, reportingCurrency);
        var categorySeries = await _reports.GetCategorySpendingAsync(from, toExclusive, reportingCurrency);
        var accounts = await _store.GetAccountsAsync();
        var reportingAccounts = accounts.Where(x => x.State != AccountState.Archived && string.Equals(x.Currency, reportingCurrency, StringComparison.OrdinalIgnoreCase)).ToList();
        var otherCurrencyAccounts = accounts.Count(x => x.State != AccountState.Archived && !string.Equals(x.Currency, reportingCurrency, StringComparison.OrdinalIgnoreCase));
        var totalBalance = SumChecked(reportingAccounts.Select(x => x.BalanceMinor));
        var income = incomeExpense.Points.FirstOrDefault(x => x.Label == "Income")?.ValueMinor ?? 0L;
        var expense = incomeExpense.Points.FirstOrDefault(x => x.Label == "Expense")?.ValueMinor ?? 0L;
        var budgets = (await _reports.GetBudgetPerformanceAsync(dateRange.Through)).Where(x => string.Equals(x.Currency, reportingCurrency, StringComparison.OrdinalIgnoreCase)).ToList();
        var remainingBudget = SumChecked(budgets.Select(x => Math.Max(0L, x.VarianceMinor)));

        TotalBalance = Display(totalBalance, reportingCurrency);
        Income = Display(income, reportingCurrency);
        Expense = Display(expense, reportingCurrency);
        NetChange = Display(checked(income - expense), reportingCurrency);
        RemainingBudget = Display(remainingBudget, reportingCurrency);
        CurrencyScope = otherCurrencyAccounts > 0
            ? $"Dashboard totals use {reportingCurrency}. {otherCurrencyAccounts} active/hidden account(s) in other currencies are excluded from aggregate cards; Finora does not invent exchange rates."
            : $"Dashboard totals use {reportingCurrency}.";

        var recent = await _store.SearchTransactionsAsync(from: from, to: toExclusive.AddTicks(-1));
        RecentTransactions.Clear();
        foreach (var tx in recent.OrderByDescending(x => x.OccurredAtUtc).Take(8))
            RecentTransactions.Add(new DashboardTransactionItem(tx.Id, tx.Merchant ?? tx.CategoryName ?? tx.Type.ToString(), tx.AccountName, tx.OccurredAtUtc, Display(tx.AmountMinor, tx.Currency)));

        TopCategories.Clear();
        foreach (var item in categorySeries.Points.Take(5))
            TopCategories.Add(new DashboardAmountItem(item.Label, Display(item.ValueMinor, reportingCurrency), null));

        UpcomingItems.Clear();
        var upcoming = await _recurring.GetOccurrencesAsync(today, today.AddDays(30), includeCompleted: false);
        foreach (var item in upcoming.OrderBy(x => x.PostponedTo ?? x.DueOn).Take(8))
            UpcomingItems.Add(new DashboardAmountItem(item.RuleName, Display(SafeMagnitude(item.AmountMinor), item.Currency), (item.PostponedTo ?? item.DueOn).ToString("d", CultureInfo.CurrentCulture)));

        Goals.Clear();
        foreach (var goal in (await _store.GetSavingsGoalsAsync()).OrderByDescending(x => x.Progress).Take(6))
            Goals.Add(new DashboardGoalItem(goal.Name, PrivacyMode ? "••••" : $"{new Money(goal.CurrentMinor, goal.Currency).Format()} / {new Money(goal.TargetMinor, goal.Currency).Format()}", $"{Math.Clamp(goal.Progress * 100d, 0d, 100d):0}%"));

        CashFlow.Clear();
        foreach (var row in await _reports.GetMonthlyComparisonAsync(6, reportingCurrency))
            CashFlow.Add(new DashboardAmountItem(new DateTime(row.Year, row.Month, 1).ToString("MMM yyyy", CultureInfo.CurrentCulture), Display(row.NetMinor, reportingCurrency), row.NetMinor >= 0 ? "Net positive" : "Net negative"));
    });

    public string Display(long minor, string currency) => PrivacyMode ? "••••" : new Money(minor, currency).Format();

    private static long SumChecked(IEnumerable<long> values)
    {
        long total = 0;
        foreach (var value in values) total = checked(total + value);
        return total;
    }

    private static long SafeMagnitude(long value)
    {
        if (value == long.MinValue) throw new InvalidDataException("Stored monetary amount is outside the supported range.");
        return value < 0 ? -value : value;
    }
}

public sealed record DashboardPeriodChoice(DashboardPeriod Period, string Label);
public sealed record DashboardAmountItem(string Label, string Amount, string? Detail);
public sealed record DashboardGoalItem(string Name, string ProgressText, string PercentText);
public sealed record DashboardTransactionItem(Guid Id, string Label, string Account, DateTimeOffset OccurredAt, string Amount);
