using Finora.Shared;

namespace Finora.Application;

public sealed record CsvColumnMapping(string DateColumn, string TypeColumn, string AmountColumn, string AccountColumn, string? CurrencyColumn, string? CategoryColumn, string? MerchantColumn, string? NoteColumn, string? PaymentMethodColumn, string? LocationColumn, string? TransferGroupColumn, string? CounterpartyAccountColumn, string? TagsColumn, bool AmountIsMinorUnits = false);
public sealed record CsvImportPreviewRow(int RowNumber, DateTimeOffset? OccurredAtUtc, long? AmountMinor, string? Currency, string? Type, string? Account, string? Category, string? Merchant, bool IsValid, string? Error);
public sealed record CsvImportPreview(IReadOnlyList<string> Headers, IReadOnlyList<CsvImportPreviewRow> Rows, int ValidRows, int InvalidRows, CsvColumnMapping? SuggestedMapping);
public sealed record CsvImportOptions(CsvColumnMapping Mapping, Guid? FallbackAccountId, bool CreateMissingCategories, bool SkipLikelyDuplicates, string DefaultCurrency);
public sealed record CsvImportResult(int ImportedRows, int SkippedDuplicateRows, int InvalidRows, IReadOnlyList<string> Errors);

public interface ICsvImportService
{
    Task<Result<CsvImportPreview>> PreviewAsync(Stream csvStream, string defaultCurrency, CancellationToken cancellationToken = default);
    Task<Result<CsvImportPreview>> PreviewWithMappingAsync(Stream csvStream, string defaultCurrency, CsvColumnMapping mapping, CancellationToken cancellationToken = default);
    Task<Result<CsvImportResult>> ImportAsync(Stream csvStream, CsvImportOptions options, CancellationToken cancellationToken = default);
}
