using Finora.Application;
using Finora.Domain;
using Microsoft.EntityFrameworkCore;

namespace Finora.Infrastructure;

public sealed class TransactionHistoryStore(IDbContextFactory<FinoraDbContext> factory) : ITransactionHistoryStore
{
    internal const int MaximumPageSize = 200;
    private readonly IDbContextFactory<FinoraDbContext> _factory = factory;

    public async Task<TransactionHistoryPage> GetPageAsync(TransactionHistoryQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.Offset < 0)
            throw new ArgumentOutOfRangeException(nameof(query), "Transaction history offset cannot be negative.");
        if (query.PageSize is <= 0 or > MaximumPageSize)
            throw new ArgumentOutOfRangeException(nameof(query), $"Transaction history page size must be between 1 and {MaximumPageSize}.");
        if (query.FromUtc is DateTimeOffset from && query.ToExclusiveUtc is DateTimeOffset toExclusive && toExclusive <= from)
            throw new ArgumentException("Transaction history end boundary must be after the start boundary.", nameof(query));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rows = db.Transactions.AsNoTracking().Where(transaction => !transaction.IsDeleted);

        if (query.AccountId is Guid accountId)
            rows = rows.Where(transaction => transaction.AccountId == accountId);
        if (query.CategoryId is Guid categoryId)
            rows = rows.Where(transaction => transaction.CategoryId == categoryId);
        if (query.Type is TransactionType type)
            rows = rows.Where(transaction => transaction.Type == type);
        if (query.FromUtc is DateTimeOffset fromUtc)
            rows = rows.Where(transaction => transaction.OccurredAtUtc >= fromUtc);
        if (query.ToExclusiveUtc is DateTimeOffset toExclusiveUtc)
            rows = rows.Where(transaction => transaction.OccurredAtUtc < toExclusiveUtc);

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var text = query.SearchText.Trim();
            rows = rows.Where(transaction =>
                (transaction.Merchant != null && transaction.Merchant.Contains(text)) ||
                (transaction.Note != null && transaction.Note.Contains(text)) ||
                (transaction.PaymentMethod != null && transaction.PaymentMethod.Contains(text)) ||
                (transaction.ManualLocation != null && transaction.ManualLocation.Contains(text)) ||
                (transaction.Account != null && transaction.Account.Name.Contains(text)) ||
                (transaction.Category != null && transaction.Category.Name.Contains(text)));
        }

        var totalCount = await rows.CountAsync(cancellationToken).ConfigureAwait(false);
        var ordered = ApplySort(rows, query.Sort);
        var items = await ordered
            .Skip(query.Offset)
            .Take(query.PageSize)
            .Select(transaction => new TransactionListItem(
                transaction.Id,
                transaction.Type,
                transaction.AmountMinor,
                transaction.Currency,
                transaction.OccurredAtUtc,
                transaction.Account!.Name,
                transaction.Category != null ? transaction.Category.Name : null,
                transaction.Merchant,
                transaction.Note))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var loadedThrough = (long)query.Offset + items.Count;
        return new TransactionHistoryPage(items, totalCount, loadedThrough < totalCount);
    }

    private static IOrderedQueryable<FinanceTransaction> ApplySort(IQueryable<FinanceTransaction> rows, TransactionHistorySort sort)
        => sort switch
        {
            TransactionHistorySort.NewestFirst => rows
                .OrderByDescending(transaction => transaction.OccurredAtUtc)
                .ThenByDescending(transaction => transaction.CreatedAtUtc)
                .ThenByDescending(transaction => transaction.Id),
            TransactionHistorySort.OldestFirst => rows
                .OrderBy(transaction => transaction.OccurredAtUtc)
                .ThenBy(transaction => transaction.CreatedAtUtc)
                .ThenBy(transaction => transaction.Id),
            TransactionHistorySort.AmountHighToLow => rows
                .OrderByDescending(transaction => transaction.AmountMinor)
                .ThenByDescending(transaction => transaction.OccurredAtUtc)
                .ThenByDescending(transaction => transaction.Id),
            TransactionHistorySort.AmountLowToHigh => rows
                .OrderBy(transaction => transaction.AmountMinor)
                .ThenByDescending(transaction => transaction.OccurredAtUtc)
                .ThenByDescending(transaction => transaction.Id),
            TransactionHistorySort.MerchantAscending => rows
                .OrderBy(transaction => (transaction.Merchant ?? string.Empty).ToUpper())
                .ThenByDescending(transaction => transaction.OccurredAtUtc)
                .ThenByDescending(transaction => transaction.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(sort), sort, "Unsupported transaction history sort order.")
        };
}