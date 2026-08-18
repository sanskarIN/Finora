using System.Diagnostics;
using Finora.Application;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.Performance;

internal sealed class PerformanceRunner(PerformanceDbFactory factory, string rootPath)
{
    private readonly PerformanceDbFactory _factory = factory;
    private readonly string _rootPath = rootPath;

    public async Task<IReadOnlyList<PerformanceMeasurement>> RunAsync(
        PerformanceOptions options,
        PerformanceDatasetSummary dataset,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(dataset);

        var measurements = new List<PerformanceMeasurement>();
        foreach (var operation in PerformanceOptions.SupportedOperations.Where(options.Operations.Contains))
        {
            for (var iteration = 1; iteration <= options.Iterations; iteration++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                switch (operation)
                {
                    case "startup":
                        measurements.Add(await MeasureAsync("startup.initialize", iteration, MeasureStartupAsync, cancellationToken).ConfigureAwait(false));
                        break;
                    case "history":
                        measurements.AddRange(await MeasureHistoryAsync(iteration, dataset, cancellationToken).ConfigureAwait(false));
                        break;
                    case "reports":
                        measurements.AddRange(await MeasureReportsAsync(iteration, cancellationToken).ConfigureAwait(false));
                        break;
                    case "csv":
                        measurements.Add(await MeasureAsync("export.csv.all", iteration, MeasureCsvAsync, cancellationToken).ConfigureAwait(false));
                        break;
                    case "pdf":
                        measurements.Add(await MeasureAsync("export.pdf.all", iteration, MeasurePdfAsync, cancellationToken).ConfigureAwait(false));
                        break;
                    case "backup":
                        measurements.Add(await MeasureAsync("backup.create.encrypted", iteration, MeasureBackupAsync, cancellationToken).ConfigureAwait(false));
                        break;
                    case "integrity":
                        measurements.Add(await MeasureAsync("integrity.full", iteration, MeasureIntegrityAsync, cancellationToken).ConfigureAwait(false));
                        break;
                }
            }
        }

        return measurements;
    }

    private async Task<IReadOnlyList<PerformanceMeasurement>> MeasureHistoryAsync(
        int iteration,
        PerformanceDatasetSummary dataset,
        CancellationToken cancellationToken)
    {
        var store = new TransactionHistoryStore(_factory);
        var measurements = new List<PerformanceMeasurement>(5)
        {
            await MeasureAsync("history.first-page", iteration, async token =>
            {
                var page = await store.GetPageAsync(new TransactionHistoryQuery(PageSize: 50), token).ConfigureAwait(false);
                if (page.TotalCount != dataset.Transactions)
                    throw new InvalidOperationException($"Expected {dataset.Transactions} visible transactions but history reported {page.TotalCount}.");
                return new MeasurementPayload(ItemCount: page.Items.Count);
            }, cancellationToken).ConfigureAwait(false),
            await MeasureAsync("history.deep-page", iteration, async token =>
            {
                var offset = Math.Max(0, dataset.Transactions / 2);
                var page = await store.GetPageAsync(new TransactionHistoryQuery(Offset: offset, PageSize: 50), token).ConfigureAwait(false);
                if (dataset.Transactions > offset && page.Items.Count == 0)
                    throw new InvalidOperationException("Deep history page unexpectedly returned no rows.");
                return new MeasurementPayload(ItemCount: page.Items.Count);
            }, cancellationToken).ConfigureAwait(false),
            await MeasureAsync("history.search-common", iteration, async token =>
            {
                var page = await store.GetPageAsync(new TransactionHistoryQuery(SearchText: "Merchant", PageSize: 50), token).ConfigureAwait(false);
                return new MeasurementPayload(ItemCount: page.TotalCount);
            }, cancellationToken).ConfigureAwait(false),
            await MeasureAsync("history.search-selective", iteration, async token =>
            {
                var page = await store.GetPageAsync(new TransactionHistoryQuery(SearchText: "Merchant 042", PageSize: 50), token).ConfigureAwait(false);
                return new MeasurementPayload(ItemCount: page.TotalCount);
            }, cancellationToken).ConfigureAwait(false),
            await MeasureAsync("history.amount-sort", iteration, async token =>
            {
                var page = await store.GetPageAsync(new TransactionHistoryQuery(Sort: TransactionHistorySort.AmountHighToLow, PageSize: 50), token).ConfigureAwait(false);
                return new MeasurementPayload(ItemCount: page.Items.Count);
            }, cancellationToken).ConfigureAwait(false)
        };
        return measurements;
    }

    private async Task<IReadOnlyList<PerformanceMeasurement>> MeasureReportsAsync(int iteration, CancellationToken cancellationToken)
    {
        var reports = new AdvancedReportService(_factory);
        var from = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = DateTimeOffset.UtcNow.AddDays(1);
        var today = DateOnly.FromDateTime(DateTime.Now);
        var measurements = new List<PerformanceMeasurement>(7)
        {
            await MeasureAsync("reports.income-expense", iteration, async token =>
            {
                var result = await reports.GetIncomeExpenseAsync(from, to, "INR", token).ConfigureAwait(false);
                return new MeasurementPayload(ItemCount: result.Points.Count);
            }, cancellationToken).ConfigureAwait(false),
            await MeasureAsync("reports.category-spending", iteration, async token =>
            {
                var result = await reports.GetCategorySpendingAsync(from, to, "INR", token).ConfigureAwait(false);
                return new MeasurementPayload(ItemCount: result.Points.Count);
            }, cancellationToken).ConfigureAwait(false),
            await MeasureAsync("reports.merchant", iteration, async token =>
            {
                var result = await reports.GetMerchantReportAsync(from, to, "INR", token).ConfigureAwait(false);
                return new MeasurementPayload(ItemCount: result.Count);
            }, cancellationToken).ConfigureAwait(false),
            await MeasureAsync("reports.account-trends", iteration, async token =>
            {
                var result = await reports.GetAccountBalanceTrendsAsync(to.AddDays(-365), to, token).ConfigureAwait(false);
                return new MeasurementPayload(ItemCount: result.Sum(series => series.Points.Count));
            }, cancellationToken).ConfigureAwait(false),
            await MeasureAsync("reports.budgets", iteration, async token =>
            {
                var result = await reports.GetBudgetPerformanceAsync(today, token).ConfigureAwait(false);
                return new MeasurementPayload(ItemCount: result.Count);
            }, cancellationToken).ConfigureAwait(false),
            await MeasureAsync("reports.recurring", iteration, async token =>
            {
                var result = await reports.GetRecurringObligationsAsync(token).ConfigureAwait(false);
                return new MeasurementPayload(ItemCount: result.Count);
            }, cancellationToken).ConfigureAwait(false),
            await MeasureAsync("reports.savings", iteration, async token =>
            {
                var result = await reports.GetSavingsProgressAsync(token).ConfigureAwait(false);
                return new MeasurementPayload(ItemCount: result.Count);
            }, cancellationToken).ConfigureAwait(false)
        };
        return measurements;
    }

    private async Task<MeasurementPayload> MeasureStartupAsync(CancellationToken cancellationToken)
    {
        await new DatabaseInitializer(_factory).InitializeAsync(cancellationToken).ConfigureAwait(false);
        return new MeasurementPayload();
    }

    private async Task<MeasurementPayload> MeasureCsvAsync(CancellationToken cancellationToken)
    {
        var csv = await new ExportService(_factory).ExportTransactionsCsvAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (csv.Length == 0) throw new InvalidOperationException("CSV export returned an empty payload.");
        return new MeasurementPayload(OutputBytes: System.Text.Encoding.UTF8.GetByteCount(csv));
    }

    private async Task<MeasurementPayload> MeasurePdfAsync(CancellationToken cancellationToken)
    {
        var pdf = await new ExportService(_factory).ExportTransactionsPdfAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (pdf.Length == 0) throw new InvalidOperationException("PDF export returned an empty payload.");
        return new MeasurementPayload(OutputBytes: pdf.Length);
    }

    private async Task<MeasurementPayload> MeasureBackupAsync(CancellationToken cancellationToken)
    {
        var backup = await new BackupService(_factory, _rootPath).CreateEncryptedBackupAsync("Finora-Performance-Synthetic-Only", cancellationToken).ConfigureAwait(false);
        if (backup.Length == 0) throw new InvalidOperationException("Encrypted backup returned an empty payload.");
        return new MeasurementPayload(OutputBytes: backup.Length);
    }

    private async Task<MeasurementPayload> MeasureIntegrityAsync(CancellationToken cancellationToken)
    {
        var report = await new DataIntegrityService(_factory, _rootPath).CheckAsync(cancellationToken).ConfigureAwait(false);
        if (!report.IsHealthy)
            throw new InvalidOperationException($"Synthetic benchmark data failed integrity with {report.Issues.Count} issue(s).");
        return new MeasurementPayload(ItemCount: checked(report.AccountsChecked + report.TransactionsChecked + report.AttachmentsChecked + report.RecurrenceOccurrencesChecked));
    }

    private static async Task<PerformanceMeasurement> MeasureAsync(
        string name,
        int iteration,
        Func<CancellationToken, Task<MeasurementPayload>> operation,
        CancellationToken cancellationToken)
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: false);
        GC.WaitForPendingFinalizers();
        var managedBefore = GC.GetTotalMemory(forceFullCollection: false);
        var workingBefore = Environment.WorkingSet;
        var stopwatch = Stopwatch.StartNew();
        var payload = await operation(cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();
        var managedAfter = GC.GetTotalMemory(forceFullCollection: false);
        var workingAfter = Environment.WorkingSet;

        return new PerformanceMeasurement(
            name,
            iteration,
            stopwatch.Elapsed.TotalMilliseconds,
            managedBefore,
            managedAfter,
            workingBefore,
            workingAfter,
            payload.OutputBytes,
            payload.ItemCount);
    }

    private readonly record struct MeasurementPayload(long? OutputBytes = null, long? ItemCount = null);
}