namespace Finora.Application;

public sealed record ReportPoint(string Label, long ValueMinor);
public sealed record ReportSeries(string Name, string Currency, IReadOnlyList<ReportPoint> Points);
public sealed record MerchantReportItem(string Merchant, long ExpenseMinor, long IncomeMinor, int TransactionCount);
public sealed record AccountBalancePoint(DateOnly Date, long BalanceMinor);
public sealed record AccountBalanceSeries(Guid AccountId, string AccountName, string Currency, IReadOnlyList<AccountBalancePoint> Points);
public sealed record BudgetPerformanceItem(Guid BudgetId, string Name, long PlannedMinor, long ActualMinor, long VarianceMinor, string Currency);
public sealed record MonthlyComparisonItem(int Year, int Month, long IncomeMinor, long ExpenseMinor, long NetMinor);

public interface IAdvancedReportService
{
    Task<ReportSeries> GetCategorySpendingAsync(DateTimeOffset from, DateTimeOffset to, string currency, CancellationToken cancellationToken = default);
    Task<ReportSeries> GetIncomeExpenseAsync(DateTimeOffset from, DateTimeOffset to, string currency, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountBalanceSeries>> GetAccountBalanceTrendsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BudgetPerformanceItem>> GetBudgetPerformanceAsync(DateOnly periodDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MerchantReportItem>> GetMerchantReportAsync(DateTimeOffset from, DateTimeOffset to, string currency, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonthlyComparisonItem>> GetMonthlyComparisonAsync(int months, string currency, CancellationToken cancellationToken = default);
}
