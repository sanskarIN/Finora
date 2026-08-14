using Finora.Domain;

namespace Finora.Application;

public sealed record ReportPoint(string Label, long ValueMinor);
public sealed record ReportSeries(string Name, string Currency, IReadOnlyList<ReportPoint> Points);
public sealed record MerchantReportItem(string Merchant, long ExpenseMinor, long IncomeMinor, int TransactionCount);
public sealed record AccountBalancePoint(DateOnly Date, long BalanceMinor);
public sealed record AccountBalanceSeries(Guid AccountId, string AccountName, string Currency, IReadOnlyList<AccountBalancePoint> Points);
public sealed record BudgetPerformanceItem(Guid BudgetId, string Name, long PlannedMinor, long ActualMinor, long VarianceMinor, string Currency);
public sealed record MonthlyComparisonItem(int Year, int Month, long IncomeMinor, long ExpenseMinor, long NetMinor);
public sealed record YearlyComparisonItem(int Year, long IncomeMinor, long ExpenseMinor, long NetMinor);
public sealed record RecurringObligationReportItem(Guid RuleId, string Name, TransactionType Type, RecurrenceStatus Status, long AmountMinor, string Currency, DateOnly? NextDueOn, DateOnly? EndsOn);
public sealed record SavingsProgressReportItem(Guid GoalId, string Name, long TargetMinor, long CurrentMinor, string Currency, double Progress, DateOnly? TargetDate, bool IsCompleted);

public interface IAdvancedReportService
{
    Task<ReportSeries> GetCategorySpendingAsync(DateTimeOffset from, DateTimeOffset through, string currency, CancellationToken cancellationToken = default);
    Task<ReportSeries> GetIncomeExpenseAsync(DateTimeOffset from, DateTimeOffset through, string currency, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountBalanceSeries>> GetAccountBalanceTrendsAsync(DateTimeOffset from, DateTimeOffset through, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BudgetPerformanceItem>> GetBudgetPerformanceAsync(DateOnly periodDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MerchantReportItem>> GetMerchantReportAsync(DateTimeOffset from, DateTimeOffset through, string currency, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonthlyComparisonItem>> GetMonthlyComparisonAsync(int months, string currency, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<YearlyComparisonItem>> GetYearlyComparisonAsync(int years, string currency, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecurringObligationReportItem>> GetRecurringObligationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SavingsProgressReportItem>> GetSavingsProgressAsync(CancellationToken cancellationToken = default);
}
