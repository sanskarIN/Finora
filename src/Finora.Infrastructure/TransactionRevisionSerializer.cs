using System.Text.Json;
using Finora.Domain;

namespace Finora.Infrastructure;

internal static class TransactionRevisionSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(FinanceTransaction transaction, IReadOnlyCollection<TransactionSplit>? splits = null, IReadOnlyCollection<Guid>? tagIds = null)
    {
        var snapshot = new TransactionRevisionSnapshot(
            transaction.Type,
            transaction.AmountMinor,
            transaction.Currency,
            transaction.AccountId,
            transaction.CategoryId,
            transaction.OccurredAtUtc,
            transaction.Merchant,
            transaction.Note,
            transaction.PaymentMethod,
            transaction.ManualLocation,
            transaction.IsDeleted,
            (splits ?? transaction.Splits).Select(x => new SplitSnapshot(x.CategoryId, x.AmountMinor, x.Note)).ToList(),
            tagIds?.ToList() ?? transaction.TransactionTags.Select(x => x.TagId).ToList());
        return JsonSerializer.Serialize(snapshot, Options);
    }

    public static string Describe(string snapshotJson)
    {
        try
        {
            var snapshot = JsonSerializer.Deserialize<TransactionRevisionSnapshot>(snapshotJson, Options);
            if (snapshot is null) return "Transaction state recorded.";
            var splitText = snapshot.Splits.Count == 0 ? "no splits" : $"{snapshot.Splits.Count} split(s)";
            var tagText = snapshot.TagIds.Count == 0 ? "no tags" : $"{snapshot.TagIds.Count} tag(s)";
            return $"{snapshot.Type}, {snapshot.AmountMinor} minor units, {snapshot.Currency}, {splitText}, {tagText}.";
        }
        catch (JsonException)
        {
            return "Transaction state recorded.";
        }
    }

    private sealed record SplitSnapshot(Guid? CategoryId, long AmountMinor, string? Note);
    private sealed record TransactionRevisionSnapshot(
        TransactionType Type,
        long AmountMinor,
        string Currency,
        Guid AccountId,
        Guid? CategoryId,
        DateTimeOffset OccurredAtUtc,
        string? Merchant,
        string? Note,
        string? PaymentMethod,
        string? ManualLocation,
        bool IsDeleted,
        IReadOnlyList<SplitSnapshot> Splits,
        IReadOnlyList<Guid> TagIds);
}
