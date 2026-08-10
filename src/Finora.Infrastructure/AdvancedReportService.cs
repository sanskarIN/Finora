using Finora.Application;
using Finora.Domain;
using Microsoft.EntityFrameworkCore;

namespace Finora.Infrastructure;

public sealed class AdvancedReportService(IDbContextFactory<FinoraDbContext> factory) : IAdvancedReportService
{
    private readonly IDbContextFactory<FinoraDbContext> _factory = factory;

    public async Task<ReportSeries> GetCategorySpendingAsync(DateTimeOffset from, DateTimeOffset to, string currency, CancellationToken cancellationToken = default)
    {
        ValidateRange(from, to);
        currency = NormalizeCurrency(currency);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var transactions = await db.Transactions.AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Splits).ThenInclude(x => x.Category)
            .Where(x => !x.IsDeleted && x.Currency == currency && x.OccurredAtUtc >= from && x.OccurredAtUtc < to && x.AmountMinor < 0 && x.Type != TransactionType.Transfer)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var totals = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var transaction in transactions)
        {
            DomainRules.ValidateTransaction(transaction);
            if (transaction.Splits.Count > 0)
            {
                foreach (var split in transaction.Splits)
                    AddChecked(totals, split.Category?.Name ?? "Uncategorized", ExpenseMagnitude(split.AmountMinor));
            }
            else
            {
                AddChecked(totals, transaction.Category?.Name ?? "Uncategorized", ExpenseMagnitude(transaction.AmountMinor));
            }
        }

        var points = totals
            .Select(item => new ReportPoint(item.Key, item.Value))
            .OrderByDescending(x => x.ValueMinor)
            .ThenBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new ReportSeries("Spending by category", currency, points);
    }

    public async Task<ReportSeries> GetIncomeExpenseAsync(DateTimeOffset from, DateTimeOffset to, string currency, CancellationToken cancellationToken = default)
    {
        ValidateRange(from, to);
        currency = NormalizeCurrency(currency);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var amounts = await db.Transactions.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Currency == currency && x.OccurredAtUtc >= from && x.OccurredAtUtc < to && x.Type != TransactionType.Transfer)
            .Select(x => x.AmountMinor)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        long income = 0;
        long expense = 0;
        foreach (var amount in amounts)
        {
            EnsureSupportedAmount(amount);
            if (amount > 0) income = checked(income + amount);
            else expense = checked(expense + ExpenseMagnitude(amount));
        }
        return new ReportSeries("Income versus expense", currency, [new ReportPoint("Income", income), new ReportPoint("Expense", expense)]);
    }

    public async Task<IReadOnlyList<AccountBalanceSeries>> GetAccountBalanceTrendsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        ValidateRange(from, to);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var accounts = await db.Accounts.AsNoTracking()
            .Where(x => x.State != AccountState.Hidden)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var transactions = await db.Transactions.AsNoTracking()
            .Where(x => !x.IsDeleted && x.OccurredAtUtc < to)
            .OrderBy(x => x.OccurredAtUtc)
            .Select(x => new { x.AccountId, x.OccurredAtUtc, x.AmountMinor })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var boundaries = BuildBoundaries(from, to);
        var result = new List<AccountBalanceSeries>();

        foreach (var account in accounts)
        {
            DomainRules.ValidateAccount(account);
            var accountRows = transactions.Where(x => x.AccountId == account.Id).ToList();
            var points = new List<AccountBalancePoint>();
            foreach (var boundary in boundaries)
            {
                var movement = SumChecked(accountRows.Where(x => x.OccurredAtUtc < boundary).Select(x => x.AmountMinor));
                var balance = checked(account.OpeningBalanceMinor + movement);
                points.Add(new AccountBalancePoint(DateOnly.FromDateTime(boundary.Date), balance));
            }
            result.Add(new AccountBalanceSeries(account.Id, account.Name, account.Currency, points));
        }
        return result;
    }

    public async Task<IReadOnlyList<BudgetPerformanceItem>> GetBudgetPerformanceAsync(DateOnly periodDate, CancellationToken cancellationToken = default)
    {
        if (periodDate == default) throw new ArgumentException("Budget report date is required.", nameof(periodDate));
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var budgets = await db.Budgets.AsNoTracking()
            .Include(x => x.Periods)
            .Where(x => !x.IsArchived)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var categories = await db.Categories.AsNoTracking()
            .Select(x => new CategoryNode(x.Id, x.ParentId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var result = new List<BudgetPerformanceItem>();

        foreach (var budget in budgets)
        {
            DomainRules.ValidateBudget(budget);
            var (startDate, endDate, planned) = ResolveBudgetPeriod(budget, periodDate);
            var from = new DateTimeOffset(startDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var to = new DateTimeOffset(endDate.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var transactions = await db.Transactions.AsNoTracking()
                .Include(x => x.Splits)
                .Where(x => !x.IsDeleted && x.Currency == budget.Currency && x.AmountMinor < 0 && x.Type != TransactionType.Transfer && x.OccurredAtUtc >= from && x.OccurredAtUtc < to)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            HashSet<Guid>? categoryIds = null;
            if (budget.CategoryId is Guid categoryId)
                categoryIds = budget.Kind == BudgetKind.Category ? DescendantsIncludingSelf(categories, categoryId) : [categoryId];

            long actual = 0;
            foreach (var transaction in transactions)
            {
                DomainRules.ValidateTransaction(transaction);
                if (categoryIds is null)
                {
                    actual = checked(actual + ExpenseMagnitude(transaction.AmountMinor));
                    continue;
                }

                if (transaction.Splits.Count > 0)
                {
                    foreach (var split in transaction.Splits)
                    {
                        if (split.CategoryId is Guid splitCategory && categoryIds.Contains(splitCategory))
                            actual = checked(actual + ExpenseMagnitude(split.AmountMinor));
                    }
                }
                else if (transaction.CategoryId is Guid transactionCategory && categoryIds.Contains(transactionCategory))
                {
                    actual = checked(actual + ExpenseMagnitude(transaction.AmountMinor));
                }
            }

            result.Add(new BudgetPerformanceItem(
                budget.Id,
                budget.Name,
                planned,
                actual,
                checked(planned - actual),
                budget.Currency));
        }
        return result;
    }

    public async Task<IReadOnlyList<MerchantReportItem>> GetMerchantReportAsync(DateTimeOffset from, DateTimeOffset to, string currency, CancellationToken cancellationToken = default)
    {
        ValidateRange(from, to);
        currency = NormalizeCurrency(currency);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rows = await db.Transactions.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Currency == currency && x.OccurredAtUtc >= from && x.OccurredAtUtc < to && x.Type != TransactionType.Transfer)
            .Select(x => new MerchantAmount(x.Merchant, x.AmountMinor))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = new List<MerchantReportItem>();
        foreach (var group in rows.GroupBy(x => NormalizeMerchant(x.Merchant), StringComparer.OrdinalIgnoreCase))
        {
            long expense = 0;
            long income = 0;
            foreach (var row in group)
            {
                EnsureSupportedAmount(row.AmountMinor);
                if (row.AmountMinor < 0) expense = checked(expense + ExpenseMagnitude(row.AmountMinor));
                else income = checked(income + row.AmountMinor);
            }
            result.Add(new MerchantReportItem(group.Key, expense, income, group.Count()));
        }
        return result.OrderByDescending(x => x.ExpenseMinor).ThenBy(x => x.Merchant, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<IReadOnlyList<MonthlyComparisonItem>> GetMonthlyComparisonAsync(int months, string currency, CancellationToken cancellationToken = default)
    {
        months = Math.Clamp(months, 1, 60);
        currency = NormalizeCurrency(currency);
        var thisMonth = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
        var startMonth = thisMonth.AddMonths(-(months - 1));
        var from = new DateTimeOffset(startMonth.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var to = new DateTimeOffset(thisMonth.AddMonths(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rows = await db.Transactions.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Currency == currency && x.OccurredAtUtc >= from && x.OccurredAtUtc < to && x.Type != TransactionType.Transfer)
            .Select(x => new DatedAmount(x.OccurredAtUtc, x.AmountMinor))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var result = new List<MonthlyComparisonItem>(months);

        for (var i = 0; i < months; i++)
        {
            var month = startMonth.AddMonths(i);
            long income = 0;
            long expense = 0;
            foreach (var row in rows.Where(x => x.OccurredAtUtc.Year == month.Year && x.OccurredAtUtc.Month == month.Month))
            {
                EnsureSupportedAmount(row.AmountMinor);
                if (row.AmountMinor > 0) income = checked(income + row.AmountMinor);
                else expense = checked(expense + ExpenseMagnitude(row.AmountMinor));
            }
            result.Add(new MonthlyComparisonItem(month.Year, month.Month, income, expense, checked(income - expense)));
        }
        return result;
    }

    private static IReadOnlyList<DateTimeOffset> BuildBoundaries(DateTimeOffset from, DateTimeOffset to)
    {
        if ((to - from).TotalDays <= 31)
        {
            var result = new List<DateTimeOffset>();
            var current = new DateTimeOffset(from.Year, from.Month, from.Day, 0, 0, 0, from.Offset);
            while (current < to)
            {
                var next = current.AddDays(1);
                result.Add(next <= to ? next : to);
                current = next;
            }
            return result;
        }

        var monthly = new List<DateTimeOffset>();
        var monthBoundary = new DateTimeOffset(from.Year, from.Month, 1, 0, 0, 0, from.Offset).AddMonths(1);
        while (monthBoundary < to)
        {
            monthly.Add(monthBoundary);
            monthBoundary = monthBoundary.AddMonths(1);
        }
        monthly.Add(to);
        return monthly;
    }

    private static HashSet<Guid> DescendantsIncludingSelf(IReadOnlyCollection<CategoryNode> categories, Guid rootId)
    {
        var result = new HashSet<Guid> { rootId };
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var category in categories)
            {
                if (category.ParentId is Guid parentId && result.Contains(parentId) && result.Add(category.Id))
                    changed = true;
            }
        }
        return result;
    }

    private static (DateOnly Start, DateOnly End, long Planned) ResolveBudgetPeriod(Budget budget, DateOnly date)
    {
        var explicitPeriod = budget.Periods.FirstOrDefault(x => x.StartsOn <= date && x.EndsOn >= date);
        if (explicitPeriod is not null)
            return (explicitPeriod.StartsOn, explicitPeriod.EndsOn, checked(explicitPeriod.PlannedMinor + (budget.RolloverEnabled ? explicitPeriod.RolloverMinor : 0)));
        return budget.Cadence switch
        {
            BudgetCadence.Weekly => ResolveWeek(date, budget.LimitMinor),
            BudgetCadence.Monthly => (new DateOnly(date.Year, date.Month, 1), new DateOnly(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month)), budget.LimitMinor),
            BudgetCadence.Custom => (date, date, budget.LimitMinor),
            _ => throw new InvalidDataException("Budget cadence is unsupported.")
        };
    }

    private static (DateOnly Start, DateOnly End, long Planned) ResolveWeek(DateOnly date, long planned)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7;
        var start = date.AddDays(-offset);
        return (start, start.AddDays(6), planned);
    }

    private static long SumChecked(IEnumerable<long> values)
    {
        long total = 0;
        foreach (var value in values)
        {
            EnsureSupportedAmount(value);
            total = checked(total + value);
        }
        return total;
    }

    private static long ExpenseMagnitude(long amountMinor)
    {
        EnsureSupportedAmount(amountMinor);
        return amountMinor < 0 ? checked(-amountMinor) : 0L;
    }

    private static void EnsureSupportedAmount(long amountMinor)
    {
        if (amountMinor == long.MinValue)
            throw new InvalidDataException("Stored monetary amount is outside the supported range.");
    }

    private static void AddChecked(IDictionary<string, long> totals, string key, long value)
        => totals[key] = checked(totals.GetValueOrDefault(key) + value);

    private static string NormalizeMerchant(string? merchant)
        => string.IsNullOrWhiteSpace(merchant) ? "Unknown" : merchant.Trim();

    private static void ValidateRange(DateTimeOffset from, DateTimeOffset to)
    {
        if (to <= from) throw new ArgumentException("Report end must be after its start.");
    }

    private static string NormalizeCurrency(string currency)
    {
        var normalized = currency?.Trim().ToUpperInvariant() ?? string.Empty;
        DomainRules.ValidateCurrency(normalized);
        return normalized;
    }

    private sealed record CategoryNode(Guid Id, Guid? ParentId);
    private sealed record MerchantAmount(string? Merchant, long AmountMinor);
    private sealed record DatedAmount(DateTimeOffset OccurredAtUtc, long AmountMinor);
}
