using System.Globalization;
using System.Text;
using Finora.Application;
using Microsoft.EntityFrameworkCore;

namespace Finora.Infrastructure;

public sealed class ExportService(IDbContextFactory<FinoraDbContext> factory) : IExportService
{
    private readonly IDbContextFactory<FinoraDbContext> _factory = factory;

    public async Task<string> ExportTransactionsCsvAsync(IReadOnlyCollection<Guid>? transactionIds = null, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var query = db.Transactions.AsNoTracking().Include(x => x.Account).Include(x => x.Category).Include(x => x.TransactionTags).ThenInclude(x => x.Tag).Where(x => !x.IsDeleted);
        if (transactionIds is { Count: > 0 }) query = query.Where(x => transactionIds.Contains(x.Id));
        var rows = await query.OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var accountNames = await db.Accounts.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken).ConfigureAwait(false);
        var sb = new StringBuilder();
        sb.AppendLine("TransactionId,Date,Type,AmountMinor,Currency,Account,Category,Merchant,Note,PaymentMethod,ManualLocation,TransferGroupId,CounterpartyAccount,Tags");
        foreach (var x in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tags = string.Join(';', x.TransactionTags.Where(t => t.Tag is not null).Select(t => t.Tag!.Name).OrderBy(t => t, StringComparer.OrdinalIgnoreCase));
            sb.Append(Csv(x.Id.ToString())).Append(',').Append(Csv(x.OccurredAtUtc.ToString("O", CultureInfo.InvariantCulture))).Append(',').Append(Csv(x.Type.ToString())).Append(',').Append(x.AmountMinor.ToString(CultureInfo.InvariantCulture)).Append(',').Append(Csv(x.Currency)).Append(',').Append(Csv(x.Account?.Name)).Append(',').Append(Csv(x.Category?.Name)).Append(',').Append(Csv(x.Merchant)).Append(',').Append(Csv(x.Note)).Append(',').Append(Csv(x.PaymentMethod)).Append(',').Append(Csv(x.ManualLocation)).Append(',').Append(Csv(x.TransferGroupId?.ToString())).Append(',').Append(Csv(x.CounterpartyAccountId is Guid counterparty ? accountNames.GetValueOrDefault(counterparty) : null)).Append(',').Append(Csv(tags)).Append("\r\n");
        }
        return sb.ToString();
    }

    public async Task<byte[]> ExportTransactionsPdfAsync(IReadOnlyCollection<Guid>? transactionIds = null, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var query = db.Transactions.AsNoTracking().Include(x => x.Account).Include(x => x.Category).Include(x => x.TransactionTags).ThenInclude(x => x.Tag).Where(x => !x.IsDeleted);
        if (transactionIds is { Count: > 0 }) query = query.Where(x => transactionIds.Contains(x.Id));
        var rows = await query.OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var lines = new List<string> { $"Generated {DateTimeOffset.Now:yyyy-MM-dd HH:mm zzz}", $"Transactions: {rows.Count}", string.Empty };
        foreach (var x in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var category = x.Category?.Name ?? "Uncategorized"; var merchant = string.IsNullOrWhiteSpace(x.Merchant) ? "—" : x.Merchant;
            var tags = string.Join(", ", x.TransactionTags.Where(t => t.Tag is not null).Select(t => t.Tag!.Name).OrderBy(t => t, StringComparer.OrdinalIgnoreCase));
            lines.Add($"{x.OccurredAtUtc:yyyy-MM-dd HH:mm} | {x.Type} | {x.AmountMinor} minor | {x.Currency} | {x.Account?.Name ?? "Unknown"}");
            lines.Add($"  Category: {category} | Merchant/payee: {merchant}");
            if (!string.IsNullOrWhiteSpace(x.Note)) lines.Add($"  Note: {x.Note}");
            if (!string.IsNullOrWhiteSpace(x.PaymentMethod)) lines.Add($"  Payment method: {x.PaymentMethod}");
            if (!string.IsNullOrWhiteSpace(x.ManualLocation)) lines.Add($"  Location: {x.ManualLocation}");
            if (!string.IsNullOrWhiteSpace(tags)) lines.Add($"  Tags: {tags}");
            lines.Add(string.Empty);
        }
        if (rows.Count == 0) lines.Add("No transactions matched the export selection.");
        return MinimalPdf.Create("Finora Transaction Export", lines);
    }

    public async Task<IReadOnlyList<CsvImportRow>> PreviewCsvAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(csvStream, Encoding.UTF8, true, 4096, true); var result = new List<CsvImportRow>(); var row = 0;
        while (result.Count < 500)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null) break;
            row++;
            if (row == 1 && line.Contains("Date", StringComparison.OrdinalIgnoreCase)) continue; if (string.IsNullOrWhiteSpace(line)) continue;
            var cells = ParseCsvLine(line); var dateIndex = cells.Count >= 14 ? 1 : 0; var typeIndex = cells.Count >= 14 ? 2 : 1; var amountIndex = cells.Count >= 14 ? 3 : 2; var accountIndex = cells.Count >= 14 ? 5 : 4; var categoryIndex = cells.Count >= 14 ? 6 : 5; var merchantIndex = cells.Count >= 14 ? 7 : 6; var noteIndex = cells.Count >= 14 ? 8 : 7;
            var valid = cells.Count > accountIndex && DateTimeOffset.TryParse(Cell(cells, dateIndex), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _) && long.TryParse(Cell(cells, amountIndex), NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
            result.Add(new CsvImportRow(row, Cell(cells, dateIndex), Cell(cells, amountIndex), Cell(cells, typeIndex), Cell(cells, accountIndex), Cell(cells, categoryIndex), Cell(cells, merchantIndex), Cell(cells, noteIndex), valid, valid ? null : "Invalid date or amount."));
        }
        return result;
    }

    private static string Csv(string? value) { value ??= string.Empty; return $"\"{value.Replace("\"", "\"\"")}\""; }
    private static string? Cell(IReadOnlyList<string> cells, int index) => index < cells.Count ? cells[index] : null;
    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>(); var sb = new StringBuilder(); var quoted = false;
        for (var i = 0; i < line.Length; i++) { var c = line[i]; if (c == '"') { if (quoted && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; } else quoted = !quoted; } else if (c == ',' && !quoted) { result.Add(sb.ToString()); sb.Clear(); } else sb.Append(c); }
        result.Add(sb.ToString()); return result;
    }

    private static class MinimalPdf
    {
        private const int LinesPerPage = 48;
        public static byte[] Create(string title, IReadOnlyList<string> lines)
        {
            var chunks = lines.Chunk(LinesPerPage).Select(x => (IReadOnlyList<string>)x.ToList()).ToList(); if (chunks.Count == 0) chunks.Add([]);
            var objects = new List<string>(); var pageObjectNumbers = Enumerable.Range(0, chunks.Count).Select(i => 4 + i * 2).ToList();
            objects.Add("<< /Type /Catalog /Pages 2 0 R >>"); objects.Add($"<< /Type /Pages /Kids [{string.Join(" ", pageObjectNumbers.Select(x => $"{x} 0 R"))}] /Count {chunks.Count} >>"); objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
            for (var i = 0; i < chunks.Count; i++) { var pageNumber = 4 + i * 2; var contentNumber = pageNumber + 1; var content = BuildPageContent(title, chunks[i], i + 1, chunks.Count); objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentNumber} 0 R >>"); objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream"); }
            using var ms = new MemoryStream(); using var writer = new StreamWriter(ms, Encoding.ASCII, 1024, true) { NewLine = "\n" }; writer.WriteLine("%PDF-1.4"); writer.Flush(); var offsets = new List<long>();
            for (var i = 0; i < objects.Count; i++) { offsets.Add(ms.Position); writer.WriteLine($"{i + 1} 0 obj"); writer.WriteLine(objects[i]); writer.WriteLine("endobj"); writer.Flush(); }
            var xref = ms.Position; writer.WriteLine("xref"); writer.WriteLine($"0 {objects.Count + 1}"); writer.WriteLine("0000000000 65535 f "); foreach (var offset in offsets) writer.WriteLine($"{offset:0000000000} 00000 n "); writer.WriteLine($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF"); writer.Flush(); return ms.ToArray();
        }
        private static string BuildPageContent(string title, IReadOnlyList<string> lines, int page, int pageCount)
        {
            var content = new StringBuilder("BT /F1 14 Tf 42 758 Td (").Append(Escape(title)).Append(") Tj 0 -18 Td /F1 8 Tf (").Append(Escape($"Page {page} of {pageCount}")).Append(") Tj 0 -18 Td ");
            foreach (var raw in lines) foreach (var line in Wrap(AsciiSafe(raw), 105)) content.Append('(').Append(Escape(line)).Append(") Tj 0 -13 Td "); content.Append("ET"); return content.ToString();
        }
        private static IEnumerable<string> Wrap(string text, int width) { if (string.IsNullOrEmpty(text)) { yield return string.Empty; yield break; } for (var index = 0; index < text.Length; index += width) yield return text.Substring(index, Math.Min(width, text.Length - index)); }
        private static string AsciiSafe(string value) => new(value.Select(c => c is >= ' ' and <= '~' ? c : '?').ToArray());
        private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }
}
