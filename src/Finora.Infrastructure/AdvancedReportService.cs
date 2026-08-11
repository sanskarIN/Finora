using Finora.Application;
using Finora.Domain;
using Finora.Shared;
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
        var boundaries = BuildBalanceBoundaries(from, to, TimeZoneInfo.Local);
        var result = new List<AccountBalanceSeries>();

        foreach (var account in accounts)
        {
            DomainRules.ValidateAccount(account);
            var accountRows = transactions.Where(x => x.AccountId == account.Id).ToList();
            var points = new List<AccountBalancePoint>();
            foreach (var boundary in boundaries)
            {
                var movement = SumChecked(accountRows.Where(x => x.OccurredAtUtc < boundary.ToExclusiveUtc).Select(x => x.AmountMinor));
                var balance = checked(account.OpeningBalanceMinor + movement);
                points.Add(new AccountBalancePoint(boundary.Through, balance));
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
            if (!BudgetPeriodPolicy.TryResolve(budget, periodDate, out var resolved)) continue;
            var utcRange = LocalDateRange.ToUtc(resolved.StartsOn, resolved.EndsOn, TimeZoneInfo.Local);
            var transactions = await db.Transactions.AsNoTracking()
                .Include(x => x.Splits)
                .Where(x => !x.IsDeleted && x.Currency == budget.Currency && x.AmountMinor < 0 && x.Type != TransactionType.Transfer && x.OccurredAtUtc >= utcRange.FromUtc && x.OccurredAtUtc < utcRange.ToExclusiveUtc)
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
                resolved.PlannedMinor,
                actual,
                checked(resolved.PlannedMinor - actual),
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
        var timeZone = TimeZoneInfo.Local;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).DateTime);
        var thisMonth = new DateOnly(today.Year, today.Month, 1);
        var startMonth = thisMonth.AddMonths(-(months - 1));
        var utcRange = LocalDateRange.ToUtc(startMonth, today, timeZone);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rows = await db.Transactions.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Currency == currency && x.OccurredAtUtc >= utcRange.FromUtc && x.OccurredAtUtc < utcRange.ToExclusiveUtc && x.Type != TransactionType.Transfer)
            .Select(x => new DatedAmount(x.OccurredAtUtc, x.AmountMinor))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var localRows = rows.Select(x => new LocalDatedAmount(DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(x.OccurredAtUtc, timeZone).DateTime), x.AmountMinor)).ToList();
        var result = new List<MonthlyComparisonItem>(months);

        for (var i = 0; i < months; i++)
        {
            var month = startMonth.AddMonths(i);
            long income = 0;
            long expense = 0;
            foreach (var row in localRows.Where(x => x.Date.Year == month.Year && x.Date.Month == month.Month))
            {
                EnsureSupportedAmount(row.AmountMinor);
                if (row.AmountMinor > 0) income = checked(income + row.AmountMinor);
                else expense = checked(expense + ExpenseMagnitude(row.AmountMinor));
            }
            result.Add(new MonthlyComparisonItem(month.Year, month.Month, income, expense, checked(income - expense)));
        }
        return result;
    }

    public async Task<IReadOnlyList<YearlyComparisonItem>> GetYearlyComparisonAsync(int years, string currency, CancellationToken cancellationToken = default)
    {
        years = Math.Clamp(years, 1, 20);
        currency = NormalizeCurrency(currency);
        var timeZone = TimeZoneInfo.Local;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).DateTime);
        var firstYear = today.Year - (years - 1);
        var utcRange = LocalDateRange.ToUtc(new DateOnly(firstYear, 1, 1), today, timeZone);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rows = await db.Transactions.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Currency == currency && x.OccurredAtUtc >= utcRange.FromUtc && x.OccurredAtUtc < utcRange.ToExclusiveUtc && x.Type != TransactionType.Transfer)
            .Select(x => new DatedAmount(x.OccurredAtUtc, x.AmountMinor))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var localRows = rows.Select(x => new LocalDatedAmount(DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(x.OccurredAtUtc, timeZone).DateTime), x.AmountMinor)).ToList();
        var result = new List<YearlyComparisonItem>(years);

        for (var year = firstYear; year <= today.Year; year++)
        {
            long income = 0;
            long expense = 0;
            foreach (var row in localRows.Where(x => x.Date.Year == year))
            {
                EnsureSupportedAmount(row.AmountMinor);
                if (row.AmountMinor > 0) income = checked(income + row.AmountMinor);
                else expense = checked(expense + ExpenseMagnitude(row.AmountMinor));
            }
            result.Add(new YearlyComparisonItem(year, income, expense, checked(income - expense)));
        }
        return result;
    }

    public async Task<IReadOnlyList<RecurringObligationReportItem>> GetRecurringObligationsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rules = await db.RecurrenceRules.AsNoTracking()
            .Where(x => x.Status != RecurrenceStatus.Archived)
            .OrderBy(x => x.Status)
            .ThenBy(x => x.NextDueOn)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = new List<RecurringObligationReportItem>(rules.Count);
        foreach (var rule in rules)
        {
            DomainRules.ValidateRecurrenceRule(rule);
            result.Add(new RecurringObligationReportItem(rule.Id, rule.Name, rule.TransactionType, rule.Status, rule.AmountMinor, rule.Currency, rule.NextDueOn, rule.EndsOn));
        }
        return result;
    }

    public async Task<IReadOnlyList<SavingsProgressReportItem>> GetSavingsProgressAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var goals = await db.SavingsGoals.AsNoTracking()
            .Include(x => x.Contributions)
            .OrderBy(x => x.TargetDate)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var result = new List<SavingsProgressReportItem>(goals.Count);

        foreach (var goal in goals)
        {
            DomainRules.ValidateSavingsGoal(goal);
            long current = goal.StartingMinor;
            foreach (var contribution in goal.Contributions.OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.CreatedAtUtc).ThenBy(x => x.Id))
            {
                DomainRules.ValidateGoalContribution(contribution);
                current = checked(current + contribution.AmountMinor);
                if (current < 0) throw new InvalidDataException("Savings goal history falls below zero.");
            }
            var progress = Math.Clamp((double)current / goal.TargetMinor, 0d, 1d);
            result.Add(new SavingsProgressReportItem(goal.Id, goal.Name, goal.TargetMinor, current, goal.Currency, progress, goal.TargetDate, current >= goal.TargetMinor));
        }
        return result;
    }

    private static IReadOnlyList<BalanceBoundary> BuildBalanceBoundaries(DateTimeOffset from, DateTimeOffset to, TimeZoneInfo timeZone)
    {
        var localFrom = TimeZoneInfo.ConvertTime(from, timeZone);
        var localLastInstant = TimeZoneInfo.ConvertTime(to.AddTicks(-1), timeZone);
        var fromDate = DateOnly.FromDateTime(localFrom.DateTime);
        var throughDate = DateOnly.FromDateTime(localLastInstant.DateTime);
        var inclusiveDays = throughDate.DayNumber - fromDate.DayNumber + 1;
        var result = new List<BalanceBoundary>();

        if (inclusiveDays <= 31)
        {
            for (var date = fromDate; date <= throughDate; date = date.AddDays(1))
                result.Add(new BalanceBoundary(date, LocalDateRange.ToUtc(date, date, timeZone).ToExclusiveUtc));
            return result;
        }

        var cursor = new DateOnly(fromDate.Year, fromDate.Month, 1);
        while (cursor <= throughDate)
        {
            var monthEnd = new DateOnly(cursor.Year, cursor.Month, DateTime.DaysInMonth(cursor.Year, cursor.Month));
            var boundaryDate = monthEnd > throughDate ? throughDate : monthEnd;
            if (boundaryDate >= fromDate)
                result.Add(new BalanceBoundary(boundaryDate, LocalDateRange.ToUtc(boundaryDate, boundaryDate, timeZone).ToExclusiveUtc));
            cursor = cursor.AddMonths(1);
        }
        return result;
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
    {
        totals.TryGetValue(key, out var current);
        totals[key] = checked(current + value);
    }

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
    private sealed record LocalDatedAmount(DateOnly Date, long AmountMinor);
    private sealed record BalanceBoundary(DateOnly Through, DateTimeOffset ToExclusiveUtc);
}
