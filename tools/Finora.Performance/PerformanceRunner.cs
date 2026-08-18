using System.Diagnostics;
using System.Text;
using Finora.Application;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.Performance;

internal sealed class PerformanceRunner(PerformanceDbFactory factory, string rootPath)
{
    private const string BackupPassword = "Finora-Performance-Synthetic-Only";
    private static readonly CsvColumnMapping ExportCsvMapping = new(
        "Date",
        "Type",
        "AmountMinor",
        "Account",
        "Currency",
        "Category",
        "Merchant",
        "Note",
        "PaymentMethod",
        "ManualLocation",
        "TransferGroupId",
        "CounterpartyAccount",
        "Tags",
        AmountIsMinorUnits: true);

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
                        measurements.AddRange(await MeasureCsvAsync(iteration, dataset, cancellationToken).ConfigureAwait(false));
                        break;
                    case "pdf":
                        measurements.Add(await MeasureAsync("export.pdf.all", iteration, MeasurePdfAsync, cancellationToken).ConfigureAwait(false));
                        break;
                    case "backup":
                        measurements.AddRange(await MeasureBackupAsync(iteration, dataset, cancellationToken).ConfigureAwait(false));
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

    private async Task<IReadOnlyList<PerformanceMeasurement>> MeasureCsvAsync(
        int iteration,
        PerformanceDatasetSummary dataset,
        CancellationToken cancellationToken)
    {
        var exportMeasurement = await MeasureAsync("export.csv.all", iteration, MeasureCsvExportAsync, cancellationToken).ConfigureAwait(false);
        var csv = await new ExportService(_factory).ExportTransactionsCsvAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var csvBytes = Encoding.UTF8.GetBytes(csv);
        var importRoot = Path.Combine(_rootPath, $"csv-import-{iteration}-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(importRoot);
            var importFactory = CreateFactory(importRoot, "finora-import.db3");
            await PrepareCsvImportTargetAsync(importFactory, cancellationToken).ConfigureAwait(false);

            var importMeasurement = await MeasureAsync("import.csv.all", iteration, async token =>
            {
                await using var stream = new MemoryStream(csvBytes, writable: false);
                var result = await new CsvImportService(importFactory).ImportAsync(
                    stream,
                    new CsvImportOptions(ExportCsvMapping, null, CreateMissingCategories: false, SkipLikelyDuplicates: false, DefaultCurrency: "INR"),
                    token).ConfigureAwait(false);
                if (!result.IsSuccess || result.Value is null)
                    throw new InvalidOperationException($"CSV import failed: {result.Error ?? "unknown error"}");
                if (result.Value.ImportedRows != dataset.Transactions || result.Value.InvalidRows != 0 || result.Value.SkippedDuplicateRows != 0)
                    throw new InvalidOperationException($"CSV import expected {dataset.Transactions} rows with no skips/errors but imported {result.Value.ImportedRows}, skipped {result.Value.SkippedDuplicateRows}, invalid {result.Value.InvalidRows}.");

                await using var db = await importFactory.CreateDbContextAsync(token).ConfigureAwait(false);
                var importedCount = await db.Transactions.AsNoTracking().CountAsync(token).ConfigureAwait(false);
                if (importedCount != dataset.Transactions)
                    throw new InvalidOperationException($"CSV import target contains {importedCount} transactions; expected {dataset.Transactions}.");
                return new MeasurementPayload(OutputBytes: csvBytes.LongLength, ItemCount: importedCount);
            }, cancellationToken).ConfigureAwait(false);

            return [exportMeasurement, importMeasurement];
        }
        finally
        {
            TryDeleteDirectory(importRoot);
        }
    }

    private async Task<IReadOnlyList<PerformanceMeasurement>> MeasureBackupAsync(
        int iteration,
        PerformanceDatasetSummary dataset,
        CancellationToken cancellationToken)
    {
        var backupPath = Path.Combine(_rootPath, $"performance-restore-{iteration}.finora");
        try
        {
            var createMeasurement = await MeasureAsync("backup.create.encrypted", iteration, async token =>
            {
                var backup = await new BackupService(_factory, _rootPath).CreateEncryptedBackupAsync(BackupPassword, token).ConfigureAwait(false);
                if (backup.Length == 0) throw new InvalidOperationException("Encrypted backup returned an empty payload.");
                await File.WriteAllBytesAsync(backupPath, backup, token).ConfigureAwait(false);
                return new MeasurementPayload(OutputBytes: backup.LongLength);
            }, cancellationToken).ConfigureAwait(false);

            var restoreMeasurement = await MeasureAsync("backup.restore.encrypted", iteration, async token =>
            {
                await using var stream = File.OpenRead(backupPath);
                var result = await new BackupService(_factory, _rootPath).RestoreEncryptedBackupAsync(stream, BackupPassword, token).ConfigureAwait(false);
                if (!result.IsSuccess)
                    throw new InvalidOperationException($"Encrypted backup restore failed: {result.Error ?? "unknown error"}");

                await using var db = await _factory.CreateDbContextAsync(token).ConfigureAwait(false);
                var transactionCount = await db.Transactions.AsNoTracking().CountAsync(token).ConfigureAwait(false);
                var attachmentCount = await db.Attachments.AsNoTracking().CountAsync(token).ConfigureAwait(false);
                if (transactionCount != dataset.Transactions || attachmentCount != dataset.Attachments)
                    throw new InvalidOperationException($"Restored graph count mismatch. Transactions {transactionCount}/{dataset.Transactions}; attachments {attachmentCount}/{dataset.Attachments}.");
                return new MeasurementPayload(ItemCount: checked(transactionCount + attachmentCount));
            }, cancellationToken).ConfigureAwait(false);

            return [createMeasurement, restoreMeasurement];
        }
        finally
        {
            try
            {
                File.Delete(backupPath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private async Task<MeasurementPayload> MeasureStartupAsync(CancellationToken cancellationToken)
    {
        await new DatabaseInitializer(_factory).InitializeAsync(cancellationToken).ConfigureAwait(false);
        return new MeasurementPayload();
    }

    private async Task<MeasurementPayload> MeasureCsvExportAsync(CancellationToken cancellationToken)
    {
        var csv = await new ExportService(_factory).ExportTransactionsCsvAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (csv.Length == 0) throw new InvalidOperationException("CSV export returned an empty payload.");
        return new MeasurementPayload(OutputBytes: Encoding.UTF8.GetByteCount(csv));
    }

    private async Task<MeasurementPayload> MeasurePdfAsync(CancellationToken cancellationToken)
    {
        var pdf = await new ExportService(_factory).ExportTransactionsPdfAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (pdf.Length == 0) throw new InvalidOperationException("PDF export returned an empty payload.");
        return new MeasurementPayload(OutputBytes: pdf.Length);
    }

    private async Task<MeasurementPayload> MeasureIntegrityAsync(CancellationToken cancellationToken)
    {
        var report = await new DataIntegrityService(_factory, _rootPath).CheckAsync(cancellationToken).ConfigureAwait(false);
        if (!report.IsHealthy)
            throw new InvalidOperationException($"Synthetic benchmark data failed integrity with {report.Issues.Count} issue(s).");
        return new MeasurementPayload(ItemCount: checked(report.AccountsChecked + report.TransactionsChecked + report.AttachmentsChecked + report.RecurrenceOccurrencesChecked));
    }

    private async Task PrepareCsvImportTargetAsync(PerformanceDbFactory importFactory, CancellationToken cancellationToken)
    {
        await new DatabaseInitializer(importFactory).InitializeAsync(cancellationToken).ConfigureAwait(false);

        await using var sourceDb = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var sourceAccounts = await sourceDb.Accounts.AsNoTracking().OrderBy(account => account.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
        var accounts = sourceAccounts.Select(account => new Account
        {
            Name = account.Name,
            Type = account.Type,
            Icon = account.Icon,
            ColorLabel = account.ColorLabel,
            Currency = account.Currency,
            OpeningBalanceMinor = account.OpeningBalanceMinor,
            State = account.State,
            CreditLimitMinor = account.CreditLimitMinor,
            BillingDay = account.BillingDay,
            LastReconciledAtUtc = account.LastReconciledAtUtc,
            ReconciledBalanceMinor = account.ReconciledBalanceMinor
        }).ToList();

        await using var importDb = await importFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        importDb.Accounts.AddRange(accounts);
        await importDb.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static PerformanceDbFactory CreateFactory(string rootPath, string databaseFileName)
    {
        var databasePath = Path.Combine(rootPath, databaseFileName);
        var options = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseSqlite($"Data Source={databasePath};Cache=Shared")
            .Options;
        return new PerformanceDbFactory(options);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
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
