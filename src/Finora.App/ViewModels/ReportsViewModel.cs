using System.Collections.ObjectModel;
using System.Globalization;
using Finora.Application;
using Finora.Domain;

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

    public ReportsViewModel(IAdvancedReportService reports, IAppSettingsService settings) { _reports = reports; _settings = settings; RefreshCommand = new AsyncCommand(LoadAsync); }
    public ObservableCollection<ReportPoint> CategoryPoints { get; } = [];
    public ObservableCollection<ReportPoint> IncomeExpensePoints { get; } = [];
    public ObservableCollection<ReportPoint> MonthlyNetPoints { get; } = [];
    public ObservableCollection<MerchantReportItem> Merchants { get; } = [];
    public ObservableCollection<BudgetPerformanceItem> Budgets { get; } = [];
    public ObservableCollection<AccountBalanceSeries> AccountTrends { get; } = [];
    public DateTime FromDate { get => _fromDate; set => SetProperty(ref _fromDate, value.Date); }
    public DateTime ToDate { get => _toDate; set => SetProperty(ref _toDate, value.Date); }
    public string Summary { get => _summary; private set => SetProperty(ref _summary, value); }
    public string CategorySummary { get => _categorySummary; private set => SetProperty(ref _categorySummary, value); }
    public string IncomeExpenseSummary { get => _incomeExpenseSummary; private set => SetProperty(ref _incomeExpenseSummary, value); }
    public System.Windows.Input.ICommand RefreshCommand { get; }

    public Task LoadAsync() => RunAsync(async () =>
    {
        if (ToDate.Date < FromDate.Date) throw new InvalidOperationException("The report end date cannot be earlier than the start date.");
        var from = new DateTimeOffset(DateTime.SpecifyKind(FromDate.Date, DateTimeKind.Local)).ToUniversalTime(); var to = new DateTimeOffset(DateTime.SpecifyKind(ToDate.Date.AddDays(1), DateTimeKind.Local)).ToUniversalTime(); var currency = _settings.DefaultCurrency;
        var category = await _reports.GetCategorySpendingAsync(from, to, currency); var incomeExpense = await _reports.GetIncomeExpenseAsync(from, to, currency); var merchants = await _reports.GetMerchantReportAsync(from, to, currency); var budgets = await _reports.GetBudgetPerformanceAsync(DateOnly.FromDateTime(ToDate)); var monthly = await _reports.GetMonthlyComparisonAsync(12, currency); var trends = await _reports.GetAccountBalanceTrendsAsync(from, to);
        Replace(CategoryPoints, category.Points); Replace(IncomeExpensePoints, incomeExpense.Points); Replace(MonthlyNetPoints, monthly.Select(x => new ReportPoint($"{CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(x.Month)} {x.Year}", x.NetMinor))); Replace(Merchants, merchants); Replace(Budgets, budgets); Replace(AccountTrends, trends);
        var income = incomeExpense.Points.FirstOrDefault(x => x.Label == "Income")?.ValueMinor ?? 0; var expense = incomeExpense.Points.FirstOrDefault(x => x.Label == "Expense")?.ValueMinor ?? 0;
        Summary = $"{FromDate:d}–{ToDate:d}: income {new Money(income, currency).Format()}, spending {new Money(expense, currency).Format()}, net {new Money(checked(income - expense), currency).Format()}.";
        CategorySummary = CategoryPoints.Count == 0 ? "No category spending exists for this period." : "Spending by category: " + string.Join("; ", CategoryPoints.Select(x => $"{x.Label} {new Money(x.ValueMinor, currency).Format()}")) + ".";
        IncomeExpenseSummary = $"Income {new Money(income, currency).Format()}; expense {new Money(expense, currency).Format()}.";
    });

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values) { target.Clear(); foreach (var value in values) target.Add(value); }
}
