using Finora.Application;
using Finora.Domain;
using Microsoft.EntityFrameworkCore;

namespace Finora.Infrastructure;

public sealed class AdvancedReportService(IDbContextFactory<FinoraDbContext> factory) : IAdvancedReportService
{
    private readonly IDbContextFactory<FinoraDbContext> _factory = factory;

    public async Task<ReportSeries> GetCategorySpendingAsync(DateTimeOffset from, DateTimeOffset to, string currency, CancellationToken cancellationToken = default)
    {
        currency = NormalizeCurrency(currency);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rows = await db.Transactions.AsNoTracking().Include(x => x.Category).Where(x => !x.IsDeleted && x.Currency == currency && x.OccurredAtUtc >= from && x.OccurredAtUtc < to && x.AmountMinor < 0 && x.Type != TransactionType.Transfer).Select(x => new { Category = x.Category != null ? x.Category.Name : "Uncategorized", x.AmountMinor }).ToListAsync(cancellationToken).ConfigureAwait(false);
        var points = rows.GroupBy(x => x.Category).Select(g => new ReportPoint(g.Key, -g.Sum(x => x.AmountMinor))).OrderByDescending(x => x.ValueMinor).ToList();
        return new ReportSeries("Spending by category", currency, points);
    }

    public async Task<ReportSeries> GetIncomeExpenseAsync(DateTimeOffset from, DateTimeOffset to, string currency, CancellationToken cancellationToken = default)
    {
        currency = NormalizeCurrency(currency);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rows = await db.Transactions.AsNoTracking().Where(x => !x.IsDeleted && x.Currency == currency && x.OccurredAtUtc >= from && x.OccurredAtUtc < to && x.Type != TransactionType.Transfer).Select(x => new { x.AmountMinor, x.Type }).ToListAsync(cancellationToken).ConfigureAwait(false);
        var income = rows.Where(x => x.AmountMinor > 0).Sum(x => x.AmountMinor);
        var expense = -rows.Where(x => x.AmountMinor < 0).Sum(x => x.AmountMinor);
        return new ReportSeries("Income versus expense", currency, [new ReportPoint("Income", income), new ReportPoint("Expense", expense)]);
    }

    public async Task<IReadOnlyList<AccountBalanceSeries>> GetAccountBalanceTrendsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        if (to <= from) throw new ArgumentException("Report end must be after its start.");
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var accounts = await db.Accounts.AsNoTracking().Where(x => x.State != AccountState.Hidden).OrderBy(x => x.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
        var transactions = await db.Transactions.AsNoTracking().Where(x => !x.IsDeleted && x.OccurredAtUtc < to).OrderBy(x => x.OccurredAtUtc).Select(x => new { x.AccountId, x.OccurredAtUtc, x.AmountMinor }).ToListAsync(cancellationToken).ConfigureAwait(false);
        var boundaries = BuildBoundaries(from, to);
        var result = new List<AccountBalanceSeries>();
        foreach (var account in accounts)
        {
            var accountRows = transactions.Where(x => x.AccountId == account.Id).ToList();
            var points = new List<AccountBalancePoint>();
            foreach (var boundary in boundaries)
            {
                var balance = checked(account.OpeningBalanceMinor + accountRows.Where(x => x.OccurredAtUtc < boundary).Sum(x => x.AmountMinor));
                points.Add(new AccountBalancePoint(DateOnly.FromDateTime(boundary.LocalDateTime.Date), balance));
            }
            result.Add(new AccountBalanceSeries(account.Id, account.Name, account.Currency, points));
        }
        return result;
    }

    public async Task<IReadOnlyList<BudgetPerformanceItem>> GetBudgetPerformanceAsync(DateOnly periodDate, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var budgets = await db.Budgets.AsNoTracking().Include(x => x.Periods).Where(x => !x.IsArchived).ToListAsync(cancellationToken).ConfigureAwait(false);
        var categories = await db.Categories.AsNoTracking().Select(x => new { x.Id, x.ParentId }).ToListAsync(cancellationToken).ConfigureAwait(false);
        var result = new List<BudgetPerformanceItem>();
        foreach (var budget in budgets)
        {
            var (startDate, endDate, planned) = ResolveBudgetPeriod(budget, periodDate);
            var from = new DateTimeOffset(startDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var to = new DateTimeOffset(endDate.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var query = db.Transactions.AsNoTracking().Where(x => !x.IsDeleted && x.Currency == budget.Currency && x.AmountMinor < 0 && x.Type != TransactionType.Transfer && x.OccurredAtUtc >= from && x.OccurredAtUtc < to);
            if (budget.CategoryId is Guid categoryId)
            {
                if (budget.Kind == BudgetKind.Category)
                {
                    var categoryIds = categories.Where(x => x.Id == categoryId || x.ParentId == categoryId).Select(x => x.Id).ToList();
                    query = query.Where(x => x.CategoryId != null && categoryIds.Contains(x.CategoryId.Value));
                }
                else query = query.Where(x => x.CategoryId == categoryId);
            }
            var actual = -(await query.SumAsync(x => (long?)x.AmountMinor, cancellationToken).ConfigureAwait(false) ?? 0L);
            result.Add(new BudgetPerformanceItem(budget.Id, budget.Name, planned, actual, checked(planned - actual), budget.Currency));
        }
        return result;
    }

    public async Task<IReadOnlyList<MerchantReportItem>> GetMerchantReportAsync(DateTimeOffset from, DateTimeOffset to, string currency, CancellationToken cancellationToken = default)
    {
        currency = NormalizeCurrency(currency);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rows = await db.Transactions.AsNoTracking().Where(x => !x.IsDeleted && x.Currency == currency && x.OccurredAtUtc >= from && x.OccurredAtUtc < to && x.Type != TransactionType.Transfer).Select(x => new { Merchant = x.Merchant ?? "Unknown", x.AmountMinor }).ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.GroupBy(x => x.Merchant.Trim().Length == 0 ? "Unknown" : x.Merchant.Trim(), StringComparer.OrdinalIgnoreCase).Select(g => new MerchantReportItem(g.Key, -g.Where(x => x.AmountMinor < 0).Sum(x => x.AmountMinor), g.Where(x => x.AmountMinor > 0).Sum(x => x.AmountMinor), g.Count())).OrderByDescending(x => x.ExpenseMinor).ThenBy(x => x.Merchant).ToList();
    }

    public async Task<IReadOnlyList<MonthlyComparisonItem>> GetMonthlyComparisonAsync(int months, string currency, CancellationToken cancellationToken = default)
    {
        months = Math.Clamp(months, 1, 60); currency = NormalizeCurrency(currency);
        var thisMonth = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1); var startMonth = thisMonth.AddMonths(-(months - 1));
        var from = new DateTimeOffset(startMonth.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero); var to = new DateTimeOffset(thisMonth.AddMonths(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rows = await db.Transactions.AsNoTracking().Where(x => !x.IsDeleted && x.Currency == currency && x.OccurredAtUtc >= from && x.OccurredAtUtc < to && x.Type != TransactionType.Transfer).Select(x => new { x.OccurredAtUtc, x.AmountMinor }).ToListAsync(cancellationToken).ConfigureAwait(false);
        var result = new List<MonthlyComparisonItem>(months);
        for (var i = 0; i < months; i++)
        {
            var month = startMonth.AddMonths(i); var monthRows = rows.Where(x => x.OccurredAtUtc.Year == month.Year && x.OccurredAtUtc.Month == month.Month).ToList();
            var income = monthRows.Where(x => x.AmountMinor > 0).Sum(x => x.AmountMinor); var expense = -monthRows.Where(x => x.AmountMinor < 0).Sum(x => x.AmountMinor);
            result.Add(new MonthlyComparisonItem(month.Year, month.Month, income, expense, checked(income - expense)));
        }
        return result;
    }

    private static IReadOnlyList<DateTimeOffset> BuildBoundaries(DateTimeOffset from, DateTimeOffset to)
    {
        if ((to - from).TotalDays <= 31)
        {
            var result = new List<DateTimeOffset>(); var current = new DateTimeOffset(from.Year, from.Month, from.Day, 0, 0, 0, from.Offset);
            while (current < to) { result.Add(current.AddDays(1)); current = current.AddDays(1); }
            return result;
        }
        else
        {
            var result = new List<DateTimeOffset>(); var current = new DateTimeOffset(from.Year, from.Month, 1, 0, 0, 0, from.Offset).AddMonths(1);
            while (current < to) { result.Add(current); current = current.AddMonths(1); }
            result.Add(to); return result;
        }
    }

    private static (DateOnly Start, DateOnly End, long Planned) ResolveBudgetPeriod(Budget budget, DateOnly date)
    {
        var explicitPeriod = budget.Periods.FirstOrDefault(x => x.StartsOn <= date && x.EndsOn >= date);
        if (explicitPeriod is not null) return (explicitPeriod.StartsOn, explicitPeriod.EndsOn, checked(explicitPeriod.PlannedMinor + explicitPeriod.RolloverMinor));
        return budget.Cadence switch { BudgetCadence.Weekly => ResolveWeek(date, budget.LimitMinor), BudgetCadence.Monthly => (new DateOnly(date.Year, date.Month, 1), new DateOnly(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month)), budget.LimitMinor), BudgetCadence.Custom => (date, date, budget.LimitMinor), _ => (date, date, budget.LimitMinor) };
    }
    private static (DateOnly Start, DateOnly End, long Planned) ResolveWeek(DateOnly date, long planned) { var offset = ((int)date.DayOfWeek + 6) % 7; var start = date.AddDays(-offset); return (start, start.AddDays(6), planned); }
    private static string NormalizeCurrency(string currency) { var normalized = currency?.Trim().ToUpperInvariant() ?? string.Empty; if (normalized.Length is < 3 or > 8) throw new ArgumentException("Currency code is invalid.", nameof(currency)); return normalized; }
}
