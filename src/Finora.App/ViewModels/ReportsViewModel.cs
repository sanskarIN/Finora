using System.Collections.ObjectModel;
using System.Globalization;
using Finora.Application;
using Finora.Domain;
using Finora.Shared;

namespace Finora.App;

public sealed class ReportsViewModel : ViewModelBase
{
    private readonly IAdvancedReportService _reports;
    private readonly IAppSettingsService _settings;
    private DateTime _fromDate = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _toDate = DateTime.Today;
    private string _summary = string.Empty;
    private string _categorySummary = string.Empty;
    private string _incomeExpenseSummary = string.Empty;
    private string _reportingCurrencyNotice = string.Empty;
    private bool _amountsHidden;

    public ReportsViewModel(IAdvancedReportService reports, IAppSettingsService settings)
    {
        _reports = reports;
        _settings = settings;
        RefreshCommand = new AsyncCommand(LoadAsync);
    }

    public ObservableCollection<ReportPoint> CategoryPoints { get; } = [];
    public ObservableCollection<ReportPoint> IncomeExpensePoints { get; } = [];
    public ObservableCollection<ReportPoint> MonthlyNetPoints { get; } = [];
    public ObservableCollection<ReportDisplayPoint> MonthlyNetRows { get; } = [];
    public ObservableCollection<ReportPoint> YearlyNetPoints { get; } = [];
    public ObservableCollection<ReportDisplayPoint> YearlyNetRows { get; } = [];
    public ObservableCollection<MerchantReportDisplayItem> MerchantRows { get; } = [];
    public ObservableCollection<BudgetPerformanceDisplayItem> BudgetRows { get; } = [];
    public ObservableCollection<AccountBalanceDisplaySeries> AccountTrendRows { get; } = [];
    public ObservableCollection<RecurringObligationDisplayItem> RecurringRows { get; } = [];
    public ObservableCollection<SavingsProgressDisplayItem> SavingsRows { get; } = [];
    public DateTime FromDate { get => _fromDate; set => SetProperty(ref _fromDate, value.Date); }
    public DateTime ToDate { get => _toDate; set => SetProperty(ref _toDate, value.Date); }
    public string Summary { get => _summary; private set => SetProperty(ref _summary, value); }
    public string CategorySummary { get => _categorySummary; private set => SetProperty(ref _categorySummary, value); }
    public string IncomeExpenseSummary { get => _incomeExpenseSummary; private set => SetProperty(ref _incomeExpenseSummary, value); }
    public string ReportingCurrencyNotice { get => _reportingCurrencyNotice; private set => SetProperty(ref _reportingCurrencyNotice, value); }
    public bool AmountsHidden { get => _amountsHidden; private set { if (SetProperty(ref _amountsHidden, value)) OnPropertyChanged(nameof(AmountsVisible)); } }
    public bool AmountsVisible => !AmountsHidden;
    public System.Windows.Input.ICommand RefreshCommand { get; }

    public Task LoadAsync() => RunAsync(async () =>
    {
        if (ToDate.Date < FromDate.Date)
            throw new InvalidOperationException("The report end date cannot be earlier than the start date.");

        AmountsHidden = _settings.PrivacyMode || _settings.HideAmountsOnLaunch;
        var selectedRange = LocalDateRange.ToUtc(DateOnly.FromDateTime(FromDate), DateOnly.FromDateTime(ToDate), TimeZoneInfo.Local);
        var from = selectedRange.FromUtc;
        var to = selectedRange.ToExclusiveUtc;
        var currency = _settings.DefaultCurrency;
        ReportingCurrencyNotice = AmountsHidden
            ? $"Report amounts and quantitative charts are hidden by privacy mode. Aggregated reports use {currency}; rows retain their own currencies and Finora does not silently convert between currencies."
            : $"Aggregated spending, income, merchant, monthly, and yearly comparisons use {currency}. Account, budget, recurring, and savings rows retain their own currencies; Finora does not silently convert between currencies.";

        var category = await _reports.GetCategorySpendingAsync(from, to, currency);
        var incomeExpense = await _reports.GetIncomeExpenseAsync(from, to, currency);
        var merchants = await _reports.GetMerchantReportAsync(from, to, currency);
        var budgets = await _reports.GetBudgetPerformanceAsync(DateOnly.FromDateTime(ToDate));
        var monthly = await _reports.GetMonthlyComparisonAsync(12, currency);
        var yearly = await _reports.GetYearlyComparisonAsync(5, currency);
        var trends = await _reports.GetAccountBalanceTrendsAsync(from, to);
        var recurring = await _reports.GetRecurringObligationsAsync();
        var savings = await _reports.GetSavingsProgressAsync();

        Replace(CategoryPoints, AmountsHidden ? [] : category.Points);
        Replace(IncomeExpensePoints, AmountsHidden ? [] : incomeExpense.Points);
        Replace(MonthlyNetPoints, AmountsHidden ? [] : monthly.Select(item => new ReportPoint($"{CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(item.Month)} {item.Year}", item.NetMinor)));
        Replace(MonthlyNetRows, monthly.Select(item => new ReportDisplayPoint(
            $"{CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(item.Month)} {item.Year}",
            DisplayMoney(item.NetMinor, currency))));
        Replace(YearlyNetPoints, AmountsHidden ? [] : yearly.Select(item => new ReportPoint(item.Year.ToString(CultureInfo.InvariantCulture), item.NetMinor)));
        Replace(YearlyNetRows, yearly.Select(item => new ReportDisplayPoint(
            item.Year.ToString(CultureInfo.InvariantCulture),
            DisplayMoney(item.NetMinor, currency))));
        Replace(MerchantRows, merchants.Select(item => new MerchantReportDisplayItem(
            item.Merchant,
            item.TransactionCount,
            DisplayMoney(item.ExpenseMinor, currency),
            DisplayMoney(item.IncomeMinor, currency))));
        Replace(BudgetRows, budgets.Select(item => new BudgetPerformanceDisplayItem(
            item.Name,
            item.Currency,
            DisplayMoney(item.PlannedMinor, item.Currency),
            DisplayMoney(item.ActualMinor, item.Currency),
            DisplayMoney(item.VarianceMinor, item.Currency))));
        Replace(AccountTrendRows, trends.Select(series => new AccountBalanceDisplaySeries(
            series.AccountId,
            series.AccountName,
            series.Currency,
            series.Points.Select(point => new AccountBalanceDisplayPoint(
                point.Date.ToString("d", CultureInfo.CurrentCulture),
                DisplayMoney(point.BalanceMinor, series.Currency))).ToList())));
        Replace(RecurringRows, recurring.Select(item => new RecurringObligationDisplayItem(
            item.Name,
            item.Type.ToString(),
            item.Status.ToString(),
            DisplayMoney(item.AmountMinor, item.Currency),
            item.Currency,
            item.NextDueOn?.ToString("d", CultureInfo.CurrentCulture) ?? "No next due date",
            item.EndsOn?.ToString("d", CultureInfo.CurrentCulture) ?? "No end date")));
        Replace(SavingsRows, savings.Select(item => new SavingsProgressDisplayItem(
            item.Name,
            item.Currency,
            DisplayMoney(item.CurrentMinor, item.Currency),
            DisplayMoney(item.TargetMinor, item.Currency),
            $"{Math.Clamp(item.Progress * 100d, 0d, 100d):0}%",
            item.TargetDate?.ToString("d", CultureInfo.CurrentCulture) ?? "No target date",
            item.IsCompleted ? "Completed" : "In progress")));

        var income = incomeExpense.Points.FirstOrDefault(item => string.Equals(item.Label, "Income", StringComparison.OrdinalIgnoreCase))?.ValueMinor ?? 0;
        var expense = incomeExpense.Points.FirstOrDefault(item => string.Equals(item.Label, "Expense", StringComparison.OrdinalIgnoreCase))?.ValueMinor ?? 0;
        Summary = AmountsHidden
            ? $"{FromDate:d}–{ToDate:d}: monetary values hidden by privacy mode."
            : $"{FromDate:d}–{ToDate:d}: income {DisplayMoney(income, currency)}, spending {DisplayMoney(expense, currency)}, net {DisplayMoney(checked(income - expense), currency)}.";
        CategorySummary = category.Points.Count == 0
            ? "No category spending exists for this period."
            : AmountsHidden
                ? "Category spending amounts are hidden by privacy mode."
                : "Spending by category: " + string.Join("; ", category.Points.Select(item => $"{item.Label} {DisplayMoney(item.ValueMinor, currency)}")) + ".";
        IncomeExpenseSummary = AmountsHidden
            ? "Income and expense amounts are hidden by privacy mode."
            : $"Income {DisplayMoney(income, currency)}; expense {DisplayMoney(expense, currency)}.";
    });

    private string DisplayMoney(long minor, string currency)
        => AmountsHidden ? "••••" : new Money(minor, currency).Format();

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var value in values) target.Add(value);
    }
}

public sealed record ReportDisplayPoint(string Label, string Amount);
public sealed record MerchantReportDisplayItem(string Merchant, int TransactionCount, string Expense, string Income);
public sealed record BudgetPerformanceDisplayItem(string Name, string Currency, string Planned, string Actual, string Variance);
public sealed record AccountBalanceDisplayPoint(string Date, string Balance);
public sealed record AccountBalanceDisplaySeries(Guid AccountId, string AccountName, string Currency, IReadOnlyList<AccountBalanceDisplayPoint> Points);
public sealed record RecurringObligationDisplayItem(string Name, string Type, string Status, string Amount, string Currency, string NextDue, string EndDate);
public sealed record SavingsProgressDisplayItem(string Name, string Currency, string Current, string Target, string Percent, string TargetDate, string Status);
