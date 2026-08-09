using System.Globalization;
using System.Text;
using Finora.Application;
using Finora.Domain;
using Finora.Shared;
using Microsoft.EntityFrameworkCore;

namespace Finora.Infrastructure;

public sealed class CsvImportService(IDbContextFactory<FinoraDbContext> factory) : ICsvImportService
{
    private const int MaximumRows = 100_000;
    private const long MaximumBytes = 50L * 1024 * 1024;
    private readonly IDbContextFactory<FinoraDbContext> _factory = factory;

    public async Task<Result<CsvImportPreview>> PreviewAsync(Stream csvStream, string defaultCurrency, CancellationToken cancellationToken = default)
    {
        try
        {
            var table = await ReadTableAsync(csvStream, cancellationToken).ConfigureAwait(false);
            if (table.Count < 2) return Result<CsvImportPreview>.Failure("The CSV must contain a header row and at least one data row.");
            var headers = NormalizeHeaders(table[0]);
            var mapping = SuggestMapping(headers);
            if (mapping is null) return Result<CsvImportPreview>.Failure("Finora could not identify Date, Type, Amount, and Account columns.");
            return Result<CsvImportPreview>.Success(BuildPreview(headers, table.Skip(1), mapping, defaultCurrency));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is IOException or InvalidDataException or DecoderFallbackException or OverflowException or FormatException) { return Result<CsvImportPreview>.Failure(ex.Message); }
    }

    public async Task<Result<CsvImportPreview>> PreviewWithMappingAsync(Stream csvStream, string defaultCurrency, CsvColumnMapping mapping, CancellationToken cancellationToken = default)
    {
        try
        {
            var table = await ReadTableAsync(csvStream, cancellationToken).ConfigureAwait(false);
            if (table.Count < 2) return Result<CsvImportPreview>.Failure("The CSV must contain a header row and at least one data row.");
            var headers = NormalizeHeaders(table[0]);
            ValidateMapping(headers, mapping);
            return Result<CsvImportPreview>.Success(BuildPreview(headers, table.Skip(1), mapping, defaultCurrency));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is IOException or InvalidDataException or DecoderFallbackException or OverflowException or FormatException) { return Result<CsvImportPreview>.Failure(ex.Message); }
    }

    public async Task<Result<CsvImportResult>> ImportAsync(Stream csvStream, CsvImportOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            var table = await ReadTableAsync(csvStream, cancellationToken).ConfigureAwait(false);
            if (table.Count < 2) return Result<CsvImportResult>.Failure("The CSV has no data rows.");
            var headers = NormalizeHeaders(table[0]);
            ValidateMapping(headers, options.Mapping);
            var parsed = table.Skip(1).Select((row, index) => ParseRow(headers, row, options.Mapping, options.DefaultCurrency, index + 2)).ToList();
            var errors = parsed.Where(x => !x.Valid).Select(x => $"Row {x.RowNumber}: {x.Error}").Take(100).ToList();
            var valid = parsed.Where(x => x.Valid).ToList();

            await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            var accounts = await db.Accounts.Where(x => x.State != AccountState.Archived).ToListAsync(cancellationToken).ConfigureAwait(false);
            var categories = await db.Categories.Where(x => !x.IsArchived).ToListAsync(cancellationToken).ConfigureAwait(false);
            var tags = await db.Tags.Where(x => !x.IsArchived).ToListAsync(cancellationToken).ConfigureAwait(false);
            var fallback = options.FallbackAccountId is Guid fallbackId ? accounts.SingleOrDefault(x => x.Id == fallbackId) : null;
            if (options.FallbackAccountId is not null && fallback is null) return Result<CsvImportResult>.Failure("The selected fallback account is unavailable.");

            var fingerprints = options.SkipLikelyDuplicates && valid.Count > 0
                ? await db.Transactions.AsNoTracking().Where(x => !x.IsDeleted).Select(x => new Fingerprint(x.AccountId, x.AmountMinor, x.OccurredAtUtc, x.Merchant, x.Type)).ToListAsync(cancellationToken).ConfigureAwait(false)
                : [];
            var imported = 0;
            var skipped = 0;
            var transfers = new Dictionary<string, List<(Parsed Row, FinanceTransaction Transaction)>>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in valid)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var account = ResolveAccount(accounts, row.Account, row.Currency!, fallback);
                if (account is null) { errors.Add($"Row {row.RowNumber}: account '{row.Account}' was not found for {row.Currency}."); continue; }
                var category = await ResolveCategoryAsync(db, categories, row.Category, options.CreateMissingCategories, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(row.Category) && category is null) { errors.Add($"Row {row.RowNumber}: category '{row.Category}' was not found."); continue; }
                if (options.SkipLikelyDuplicates && fingerprints.Any(x => IsDuplicate(x, account.Id, row))) { skipped++; continue; }

                var transaction = new FinanceTransaction
                {
                    Type = row.Type!.Value,
                    AmountMinor = row.AmountMinor!.Value,
                    Currency = row.Currency!,
                    AccountId = account.Id,
                    CategoryId = category?.Id,
                    OccurredAtUtc = row.OccurredAt!.Value,
                    Merchant = Normalize(row.Merchant, 240),
                    Note = Normalize(row.Note, 2000),
                    PaymentMethod = Normalize(row.PaymentMethod, 120),
                    ManualLocation = Normalize(row.Location, 240)
                };
                if (row.Type == TransactionType.Transfer)
                {
                    if (string.IsNullOrWhiteSpace(row.TransferGroup)) { errors.Add($"Row {row.RowNumber}: transfer rows require a transfer-group value."); continue; }
                    if (!transfers.TryGetValue(row.TransferGroup, out var group)) transfers[row.TransferGroup] = group = [];
                    group.Add((row, transaction));
                }
                else
                {
                    AddTags(db, transaction, row.Tags, tags);
                    db.Transactions.Add(transaction);
                    imported++;
                }
            }

            foreach (var group in transfers)
            {
                if (group.Value.Count != 2) { errors.Add($"Transfer group '{group.Key}' must contain exactly two rows."); continue; }
                var first = group.Value[0].Transaction;
                var second = group.Value[1].Transaction;
                if (first.AmountMinor + second.AmountMinor != 0 || !string.Equals(first.Currency, second.Currency, StringComparison.OrdinalIgnoreCase)) { errors.Add($"Transfer group '{group.Key}' must contain opposite equal amounts in one currency."); continue; }
                if (first.AccountId == second.AccountId) { errors.Add($"Transfer group '{group.Key}' must use two different accounts."); continue; }
                var transferId = Guid.NewGuid();
                first.TransferGroupId = transferId; second.TransferGroupId = transferId;
                first.CounterpartyAccountId = second.AccountId; second.CounterpartyAccountId = first.AccountId;
                db.Transactions.AddRange(first, second);
                imported += 2;
            }

            if (errors.Count > 100) errors = errors.Take(100).ToList();
            db.AuditEntries.Add(new AuditEntry { EntityType = "CsvImport", EntityId = Guid.NewGuid(), Action = $"Imported:{imported};SkippedDuplicates:{skipped};Errors:{errors.Count}" });
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return Result<CsvImportResult>.Success(new CsvImportResult(imported, skipped, parsed.Count(x => !x.Valid) + errors.Count, errors));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is IOException or InvalidDataException or DecoderFallbackException or OverflowException or FormatException or DbUpdateException) { return Result<CsvImportResult>.Failure(ex.Message); }
    }

    private static CsvImportPreview BuildPreview(IReadOnlyList<string> headers, IEnumerable<List<string>> dataRows, CsvColumnMapping mapping, string defaultCurrency)
    {
        var parsed = dataRows.Select((row, index) => ParseRow(headers, row, mapping, defaultCurrency, index + 2)).ToList();
        var rows = parsed.Take(500).Select(x => new CsvImportPreviewRow(x.RowNumber, x.OccurredAt, x.AmountMinor, x.Currency, x.Type?.ToString(), x.Account, x.Category, x.Merchant, x.Valid, x.Error)).ToList();
        return new CsvImportPreview(headers, rows, parsed.Count(x => x.Valid), parsed.Count(x => !x.Valid), mapping);
    }

    private static Parsed ParseRow(IReadOnlyList<string> headers, IReadOnlyList<string> row, CsvColumnMapping mapping, string defaultCurrency, int rowNumber)
    {
        string? Get(string? column)
        {
            if (string.IsNullOrWhiteSpace(column)) return null;
            var index = headers.ToList().FindIndex(x => string.Equals(x, column, StringComparison.OrdinalIgnoreCase));
            return index >= 0 && index < row.Count ? NullIfBlank(row[index]) : null;
        }
        var dateText = Get(mapping.DateColumn); var typeText = Get(mapping.TypeColumn); var amountText = Get(mapping.AmountColumn); var account = Get(mapping.AccountColumn);
        var currency = (Get(mapping.CurrencyColumn) ?? defaultCurrency).Trim().ToUpperInvariant();
        if (!DateTimeOffset.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var occurredAt)) return Parsed.Invalid(rowNumber, "Date could not be parsed.", account, currency, typeText, Get(mapping.CategoryColumn), Get(mapping.MerchantColumn));
        if (!Enum.TryParse<TransactionType>(typeText, true, out var type)) return Parsed.Invalid(rowNumber, "Transaction type is invalid.", account, currency, typeText, Get(mapping.CategoryColumn), Get(mapping.MerchantColumn));
        if (string.IsNullOrWhiteSpace(account)) return Parsed.Invalid(rowNumber, "Account is required.", account, currency, typeText, Get(mapping.CategoryColumn), Get(mapping.MerchantColumn));
        if (currency.Length is < 3 or > 8) return Parsed.Invalid(rowNumber, "Currency code is invalid.", account, currency, typeText, Get(mapping.CategoryColumn), Get(mapping.MerchantColumn));
        long amountMinor;
        if (mapping.AmountIsMinorUnits)
        {
            if (!long.TryParse(amountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out amountMinor)) return Parsed.Invalid(rowNumber, "Amount minor units are invalid.", account, currency, typeText, Get(mapping.CategoryColumn), Get(mapping.MerchantColumn));
        }
        else
        {
            if (!decimal.TryParse(amountText, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var major)) return Parsed.Invalid(rowNumber, "Amount is invalid.", account, currency, typeText, Get(mapping.CategoryColumn), Get(mapping.MerchantColumn));
            amountMinor = Money.ToMinorUnits(major, 2);
        }
        amountMinor = type switch { TransactionType.Expense => -Math.Abs(amountMinor), TransactionType.Income or TransactionType.Refund => Math.Abs(amountMinor), _ => amountMinor };
        if (amountMinor == 0) return Parsed.Invalid(rowNumber, "Amount cannot be zero.", account, currency, typeText, Get(mapping.CategoryColumn), Get(mapping.MerchantColumn));
        return new Parsed(rowNumber, true, null, occurredAt.ToUniversalTime(), type, amountMinor, currency, account, Get(mapping.CategoryColumn), Get(mapping.MerchantColumn), Get(mapping.NoteColumn), Get(mapping.PaymentMethodColumn), Get(mapping.LocationColumn), Get(mapping.TransferGroupColumn), Get(mapping.CounterpartyAccountColumn), Get(mapping.TagsColumn));
    }

    private static async Task<Category?> ResolveCategoryAsync(FinoraDbContext db, List<Category> categories, string? name, bool createMissing, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var existing = categories.FirstOrDefault(x => string.Equals(x.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
        if (existing is not null || !createMissing) return existing;
        var category = new Category { Name = name.Trim(), Icon = "tag", SortOrder = categories.Count == 0 ? 0 : categories.Max(x => x.SortOrder) + 1 };
        categories.Add(category); db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return category;
    }

    private static void AddTags(FinoraDbContext db, FinanceTransaction transaction, string? text, List<Tag> tags)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        foreach (var raw in text.Split([';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var name = raw.Length <= 80 ? raw : raw[..80];
            var tag = tags.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            if (tag is null) { tag = new Tag { Name = name }; tags.Add(tag); db.Tags.Add(tag); }
            transaction.TransactionTags.Add(new TransactionTag { TransactionId = transaction.Id, TagId = tag.Id, Tag = tag });
        }
    }

    private static Account? ResolveAccount(IEnumerable<Account> accounts, string? name, string currency, Account? fallback) => !string.IsNullOrWhiteSpace(name) ? accounts.FirstOrDefault(x => string.Equals(x.Name, name.Trim(), StringComparison.OrdinalIgnoreCase) && string.Equals(x.Currency, currency, StringComparison.OrdinalIgnoreCase)) ?? (fallback?.Currency == currency ? fallback : null) : fallback?.Currency == currency ? fallback : null;
    private static bool IsDuplicate(Fingerprint existing, Guid accountId, Parsed row) => existing.AccountId == accountId && existing.AmountMinor == row.AmountMinor && existing.Type == row.Type && Math.Abs((existing.OccurredAtUtc - row.OccurredAt!.Value).TotalMinutes) <= 10 && string.Equals(NormalizeCompare(existing.Merchant), NormalizeCompare(row.Merchant), StringComparison.Ordinal);

    private static CsvColumnMapping? SuggestMapping(IReadOnlyList<string> headers)
    {
        string? Find(params string[] names) => headers.FirstOrDefault(h => names.Any(n => NormalizeHeader(h) == NormalizeHeader(n)));
        var date = Find("Date", "OccurredAt", "OccurredAtUtc", "Transaction Date", "Timestamp"); var type = Find("Type", "TransactionType", "Transaction Type"); var minor = Find("AmountMinor", "Amount Minor", "MinorUnits"); var major = Find("Amount", "Value", "Transaction Amount"); var account = Find("Account", "AccountName", "Account Name");
        if (date is null || type is null || (minor is null && major is null) || account is null) return null;
        return new CsvColumnMapping(date, type, minor ?? major!, account, Find("Currency", "CurrencyCode"), Find("Category", "CategoryName"), Find("Merchant", "Payee", "Merchant/Payee"), Find("Note", "Notes", "Memo"), Find("PaymentMethod", "Payment Method"), Find("Location", "ManualLocation"), Find("TransferGroupId", "Transfer Group", "TransferGroup"), Find("CounterpartyAccount", "Destination Account"), Find("Tags", "Tag"), minor is not null);
    }

    private static void ValidateMapping(IReadOnlyList<string> headers, CsvColumnMapping mapping)
    {
        string[] required = [mapping.DateColumn, mapping.TypeColumn, mapping.AmountColumn, mapping.AccountColumn];
        if (required.Any(string.IsNullOrWhiteSpace) || required.Any(x => !headers.Contains(x, StringComparer.OrdinalIgnoreCase))) throw new InvalidDataException("Date, Type, Amount, and Account must map to existing CSV columns.");
        string?[] optional = [mapping.CurrencyColumn, mapping.CategoryColumn, mapping.MerchantColumn, mapping.NoteColumn, mapping.PaymentMethodColumn, mapping.LocationColumn, mapping.TransferGroupColumn, mapping.CounterpartyAccountColumn, mapping.TagsColumn];
        var missing = optional.Where(x => !string.IsNullOrWhiteSpace(x) && !headers.Contains(x!, StringComparer.OrdinalIgnoreCase)).ToList();
        if (missing.Count > 0) throw new InvalidDataException("Mapped columns are missing: " + string.Join(", ", missing));
    }

    private static async Task<List<List<string>>> ReadTableAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (!stream.CanRead) throw new InvalidDataException("The selected CSV cannot be read.");
        if (stream.CanSeek && stream.Length > MaximumBytes) throw new InvalidDataException("CSV files are limited to 50 MB.");
        if (stream.CanSeek) stream.Position = 0;
        using var reader = new StreamReader(stream, new UTF8Encoding(false, true), true, 81920, true);
        var text = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        if (Encoding.UTF8.GetByteCount(text) > MaximumBytes) throw new InvalidDataException("CSV files are limited to 50 MB.");
        var rows = ParseCsv(text); if (rows.Count > MaximumRows + 1) throw new InvalidDataException($"CSV import is limited to {MaximumRows:N0} rows."); return rows;
    }

    private static List<List<string>> ParseCsv(string text)
    {
        var rows = new List<List<string>>(); var row = new List<string>(); var cell = new StringBuilder(); var quoted = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '"') { if (quoted && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; } else quoted = !quoted; }
            else if (c == ',' && !quoted) { row.Add(cell.ToString()); cell.Clear(); }
            else if ((c == '\r' || c == '\n') && !quoted) { if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++; row.Add(cell.ToString()); cell.Clear(); if (row.Any(x => !string.IsNullOrWhiteSpace(x))) rows.Add(row); row = []; }
            else cell.Append(c);
        }
        if (quoted) throw new InvalidDataException("CSV contains an unterminated quoted field.");
        if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); if (row.Any(x => !string.IsNullOrWhiteSpace(x))) rows.Add(row); }
        return rows;
    }

    private static List<string> NormalizeHeaders(IReadOnlyList<string> row)
    {
        var headers = row.Select((x, i) => string.IsNullOrWhiteSpace(x) ? $"Column{i + 1}" : x.Trim()).ToList();
        var duplicates = headers.GroupBy(x => x, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
        if (duplicates.Count > 0) throw new InvalidDataException("CSV contains duplicate column names: " + string.Join(", ", duplicates));
        return headers;
    }

    private static string NormalizeHeader(string value) => new(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string NormalizeCompare(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
    private static string? Normalize(string? value, int maxLength) { if (string.IsNullOrWhiteSpace(value)) return null; var trimmed = value.Trim(); return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength]; }

    private sealed record Fingerprint(Guid AccountId, long AmountMinor, DateTimeOffset OccurredAtUtc, string? Merchant, TransactionType Type);
    private sealed record Parsed(int RowNumber, bool Valid, string? Error, DateTimeOffset? OccurredAt, TransactionType? Type, long? AmountMinor, string? Currency, string? Account, string? Category, string? Merchant, string? Note, string? PaymentMethod, string? Location, string? TransferGroup, string? CounterpartyAccount, string? Tags)
    {
        public static Parsed Invalid(int rowNumber, string error, string? account, string? currency, string? type, string? category, string? merchant) => new(rowNumber, false, error, null, Enum.TryParse<TransactionType>(type, true, out var parsedType) ? parsedType : null, null, currency, account, category, merchant, null, null, null, null, null, null);
    }
}
