using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Finora.Application;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.PerformanceLab;

internal static class Program
{
    private const string BenchmarkBackupPassword = "Finora-Performance-Lab-Only-2026!";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static async Task<int> Main(string[] args)
    {
        var options = LabOptions.Parse(args);
        var root = Path.GetFullPath(options.WorkDirectory ?? Path.Combine(Path.GetTempPath(), $"finora-performance-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);

        try
        {
            Console.WriteLine($"Finora Performance Lab | rows={options.Rows:N0} | iterations={options.Iterations}");
            Console.WriteLine($"Working directory: {root}");

            var fixture = await BenchmarkFixture.CreateAsync(root, options.Rows).ConfigureAwait(false);
            var runner = new BenchmarkRunner(fixture, options);
            var report = await runner.RunAsync().ConfigureAwait(false);
            var outputPath = Path.GetFullPath(options.OutputPath ?? Path.Combine(root, "finora-performance.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(report, JsonOptions), Encoding.UTF8).ConfigureAwait(false);

            Console.WriteLine();
            Console.WriteLine("Benchmark results");
            foreach (var measurement in report.Measurements)
            {
                Console.WriteLine(
                    $"- {measurement.Name}: median {measurement.MedianMilliseconds:N2} ms; " +
                    $"min {measurement.MinimumMilliseconds:N2} ms; max {measurement.MaximumMilliseconds:N2} ms; " +
                    $"allocated {measurement.MedianAllocatedBytes:N0} B; result {measurement.ResultUnits:N0} {measurement.ResultUnitLabel}");
            }
            Console.WriteLine($"JSON report: {outputPath}");
            return 0;
        }
        finally
        {
            if (!options.KeepData)
                TryDeleteDirectory(root);
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
            // Performance data lives under a disposable temp directory unless --keep-data is requested.
        }
        catch (UnauthorizedAccessException)
        {
            // Cleanup must never hide a completed benchmark result or its console summary.
        }
    }
}

internal sealed record LabOptions(int Rows, int Iterations, string? OutputPath, string? WorkDirectory, bool KeepData)
{
    public static LabOptions Parse(IReadOnlyList<string> args)
    {
        var rows = 10_000;
        var iterations = 3;
        string? output = null;
        string? workDirectory = null;
        var keepData = false;

        for (var index = 0; index < args.Count; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "--rows":
                    rows = ParseInteger(ReadValue(args, ref index, argument), argument, 1_000, 100_000);
                    break;
                case "--iterations":
                    iterations = ParseInteger(ReadValue(args, ref index, argument), argument, 1, 20);
                    break;
                case "--output":
                    output = ReadValue(args, ref index, argument);
                    break;
                case "--work-dir":
                    workDirectory = ReadValue(args, ref index, argument);
                    break;
                case "--keep-data":
                    keepData = true;
                    break;
                case "--help":
                case "-h":
                    throw new ArgumentException(Usage);
                default:
                    throw new ArgumentException($"Unknown argument '{argument}'.{Environment.NewLine}{Usage}");
            }
        }

        return new LabOptions(rows, iterations, output, workDirectory, keepData);
    }

    private static string ReadValue(IReadOnlyList<string> args, ref int index, string argument)
    {
        index++;
        if (index >= args.Count || string.IsNullOrWhiteSpace(args[index]))
            throw new ArgumentException($"{argument} requires a value.");
        return args[index];
    }

    private static int ParseInteger(string value, string argument, int minimum, int maximum)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed < minimum || parsed > maximum)
            throw new ArgumentOutOfRangeException(argument, $"{argument} must be between {minimum:N0} and {maximum:N0}.");
        return parsed;
    }

    private const string Usage = "Usage: dotnet run --project tools/Finora.PerformanceLab -- --rows 10000 --iterations 3 [--output report.json] [--work-dir PATH] [--keep-data]";
}

internal sealed class BenchmarkRunner(BenchmarkFixture fixture, LabOptions options)
{
    private readonly BenchmarkFixture _fixture = fixture;
    private readonly LabOptions _options = options;

    public async Task<PerformanceReport> RunAsync()
    {
        var measurements = new List<PerformanceMeasurement>();
        var from = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var history = new TransactionHistoryStore(_fixture.Factory);
        var reports = new AdvancedReportService(_fixture.Factory);
        var export = new ExportService(_fixture.Factory);
        var integrity = new DataIntegrityService(_fixture.Factory, _fixture.AppDataRoot);

        measurements.Add(await MeasureAsync(
            "database-open-and-count",
            _options.Iterations,
            async () =>
            {
                await using var db = await _fixture.Factory.CreateDbContextAsync().ConfigureAwait(false);
                var count = await db.Transactions.AsNoTracking().CountAsync().ConfigureAwait(false);
                return new OperationResult(count, "rows");
            }).ConfigureAwait(false));

        measurements.Add(await MeasureAsync(
            "transaction-search-first-page",
            _options.Iterations,
            async () =>
            {
                var page = await history.GetPageAsync(new TransactionHistoryQuery(SearchText: "Merchant 0042", PageSize: 50)).ConfigureAwait(false);
                return new OperationResult(page.Items.Count, "rows");
            }).ConfigureAwait(false));

        measurements.Add(await MeasureAsync(
            "transaction-history-late-page",
            _options.Iterations,
            async () =>
            {
                var offset = Math.Max(0, _options.Rows - 50);
                var page = await history.GetPageAsync(new TransactionHistoryQuery(Offset: offset, PageSize: 50)).ConfigureAwait(false);
                return new OperationResult(page.Items.Count, "rows");
            }).ConfigureAwait(false));

        measurements.Add(await MeasureAsync(
            "income-expense-report",
            _options.Iterations,
            async () =>
            {
                var series = await reports.GetIncomeExpenseAsync(from, to, "INR").ConfigureAwait(false);
                return new OperationResult(series.Points.Count, "points");
            }).ConfigureAwait(false));

        measurements.Add(await MeasureAsync(
            "category-spending-report",
            _options.Iterations,
            async () =>
            {
                var series = await reports.GetCategorySpendingAsync(from, to, "INR").ConfigureAwait(false);
                return new OperationResult(series.Points.Count, "points");
            }).ConfigureAwait(false));

        measurements.Add(await MeasureAsync(
            "csv-export-all",
            _options.Iterations,
            async () =>
            {
                var csv = await export.ExportTransactionsCsvAsync().ConfigureAwait(false);
                return new OperationResult(Encoding.UTF8.GetByteCount(csv), "bytes");
            }).ConfigureAwait(false));

        measurements.Add(await MeasureAsync(
            "pdf-export-all",
            _options.Iterations,
            async () =>
            {
                var pdf = await export.ExportTransactionsPdfAsync().ConfigureAwait(false);
                return new OperationResult(pdf.LongLength, "bytes");
            }).ConfigureAwait(false));

        measurements.Add(await MeasureAsync(
            "integrity-full-scan",
            _options.Iterations,
            async () =>
            {
                var report = await integrity.CheckAsync().ConfigureAwait(false);
                return new OperationResult(report.Issues.Count, "issues");
            }).ConfigureAwait(false));

        byte[]? backupBytes = null;
        try
        {
            measurements.Add(await MeasureAsync(
                "encrypted-backup-create",
                _options.Iterations,
                async () =>
                {
                    var backup = new BackupService(_fixture.Factory, _fixture.AppDataRoot);
                    var bytes = await backup.CreateEncryptedBackupAsync(ProgramBenchmarkSecrets.Password).ConfigureAwait(false);
                    backupBytes = bytes;
                    return new OperationResult(bytes.LongLength, "bytes");
                }).ConfigureAwait(false));

            if (backupBytes is not null)
            {
                measurements.Add(await MeasureAsync(
                    "encrypted-backup-restore",
                    1,
                    async () =>
                    {
                        var restoreRoot = Path.Combine(_fixture.Root, $"restore-{Guid.NewGuid():N}");
                        Directory.CreateDirectory(restoreRoot);
                        try
                        {
                            var restoreFixture = await BenchmarkFixture.CreateEmptyAsync(restoreRoot).ConfigureAwait(false);
                            var backup = new BackupService(restoreFixture.Factory, restoreFixture.AppDataRoot);
                            await using var stream = new MemoryStream(backupBytes, writable: false);
                            var result = await backup.RestoreEncryptedBackupAsync(stream, ProgramBenchmarkSecrets.Password).ConfigureAwait(false);
                            if (!result.IsSuccess) throw new InvalidDataException(result.Error ?? "Benchmark restore failed.");
                            await using var db = await restoreFixture.Factory.CreateDbContextAsync().ConfigureAwait(false);
                            var count = await db.Transactions.AsNoTracking().CountAsync().ConfigureAwait(false);
                            return new OperationResult(count, "rows");
                        }
                        finally
                        {
                            BenchmarkFixture.TryDelete(restoreRoot);
                        }
                    }).ConfigureAwait(false));
            }
        }
        finally
        {
            if (backupBytes is not null) Array.Clear(backupBytes);
        }

        measurements.Add(await MeasureAsync(
            "csv-import",
            1,
            async () =>
            {
                var importRoot = Path.Combine(_fixture.Root, $"import-{Guid.NewGuid():N}");
                Directory.CreateDirectory(importRoot);
                try
                {
                    var importFixture = await BenchmarkFixture.CreateEmptyAsync(importRoot).ConfigureAwait(false);
                    await importFixture.EnsureBenchmarkAccountAsync().ConfigureAwait(false);
                    var csvBytes = BenchmarkCsv.Create(_options.Rows);
                    await using var stream = new MemoryStream(csvBytes, writable: false);
                    var service = new CsvImportService(importFixture.Factory);
                    var mapping = new CsvColumnMapping(
                        "Date", "Type", "AmountMinor", "Account", "Currency", "Category", "Merchant", "Note",
                        null, null, null, null, null, AmountIsMinorUnits: true);
                    var result = await service.ImportAsync(stream, new CsvImportOptions(mapping, null, false, false, "INR")).ConfigureAwait(false);
                    if (!result.IsSuccess || result.Value is null) throw new InvalidDataException(result.Error ?? "Benchmark CSV import failed.");
                    return new OperationResult(result.Value.ImportedRows, "rows");
                }
                finally
                {
                    BenchmarkFixture.TryDelete(importRoot);
                }
            }).ConfigureAwait(false));

        return new PerformanceReport(
            DateTimeOffset.UtcNow,
            _options.Rows,
            _options.Iterations,
            Environment.Version.ToString(),
            Environment.OSVersion.ToString(),
            Environment.ProcessorCount,
            GCSettingsInfo.ServerGc,
            measurements);
    }

    private static async Task<PerformanceMeasurement> MeasureAsync(string name, int iterations, Func<Task<OperationResult>> operation)
    {
        var milliseconds = new double[iterations];
        var allocations = new long[iterations];
        OperationResult lastResult = default;

        for (var index = 0; index < iterations; index++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var stopwatch = Stopwatch.StartNew();
            lastResult = await operation().ConfigureAwait(false);
            stopwatch.Stop();
            var allocatedAfter = GC.GetTotalAllocatedBytes(precise: true);
            milliseconds[index] = stopwatch.Elapsed.TotalMilliseconds;
            allocations[index] = Math.Max(0, allocatedAfter - allocatedBefore);
        }

        return new PerformanceMeasurement(
            name,
            iterations,
            milliseconds.Min(),
            Median(milliseconds),
            milliseconds.Max(),
            Median(allocations),
            lastResult.Units,
            lastResult.UnitLabel);
    }

    private static double Median(IEnumerable<double> values)
    {
        var ordered = values.Order().ToArray();
        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 0 ? (ordered[middle - 1] + ordered[middle]) / 2d : ordered[middle];
    }

    private static long Median(IEnumerable<long> values)
    {
        var ordered = values.Order().ToArray();
        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 0 ? checked((ordered[middle - 1] / 2) + (ordered[middle] / 2) + ((ordered[middle - 1] % 2 + ordered[middle] % 2) / 2)) : ordered[middle];
    }
}

internal sealed class BenchmarkFixture
{
    private BenchmarkFixture(string root, string appDataRoot, PerformanceDbContextFactory factory)
    {
        Root = root;
        AppDataRoot = appDataRoot;
        Factory = factory;
    }

    public string Root { get; }
    public string AppDataRoot { get; }
    public PerformanceDbContextFactory Factory { get; }

    public static async Task<BenchmarkFixture> CreateAsync(string root, int rows)
    {
        var fixture = await CreateEmptyAsync(root).ConfigureAwait(false);
        await fixture.EnsureBenchmarkAccountAsync().ConfigureAwait(false);
        await fixture.SeedTransactionsAsync(rows).ConfigureAwait(false);
        return fixture;
    }

    public static async Task<BenchmarkFixture> CreateEmptyAsync(string root)
    {
        var appDataRoot = Path.Combine(root, "appdata");
        Directory.CreateDirectory(appDataRoot);
        var databasePath = Path.Combine(appDataRoot, "finora-performance.db");
        var options = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;
        var factory = new PerformanceDbContextFactory(options);
        await new DatabaseInitializer(factory).InitializeAsync().ConfigureAwait(false);
        return new BenchmarkFixture(root, appDataRoot, factory);
    }

    public async Task EnsureBenchmarkAccountAsync()
    {
        await using var db = await Factory.CreateDbContextAsync().ConfigureAwait(false);
        if (await db.Accounts.AnyAsync(account => account.Name == BenchmarkCsv.AccountName).ConfigureAwait(false)) return;
        db.Accounts.Add(new Account
        {
            Name = BenchmarkCsv.AccountName,
            Type = AccountType.Bank,
            Currency = "INR",
            OpeningBalanceMinor = 500_000
        });
        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    private async Task SeedTransactionsAsync(int rows)
    {
        await using var db = await Factory.CreateDbContextAsync().ConfigureAwait(false);
        var account = await db.Accounts.SingleAsync(item => item.Name == BenchmarkCsv.AccountName).ConfigureAwait(false);
        var categories = await db.Categories.OrderBy(item => item.SortOrder).Take(8).ToListAsync().ConfigureAwait(false);
        if (categories.Count == 0) throw new InvalidOperationException("Performance fixture requires default categories.");

        const int batchSize = 2_000;
        var startedAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (var offset = 0; offset < rows; offset += batchSize)
        {
            var count = Math.Min(batchSize, rows - offset);
            var batch = new List<FinanceTransaction>(count);
            for (var local = 0; local < count; local++)
            {
                var index = offset + local;
                var type = index % 5 == 0 ? TransactionType.Income : TransactionType.Expense;
                var amount = 100L + (index % 250_000);
                var transaction = TransactionFactory.Create(
                    type,
                    amount,
                    "INR",
                    account.Id,
                    startedAt.AddMinutes(index * 7L),
                    categories[index % categories.Count].Id,
                    $"Merchant {index % 500:D4}",
                    $"Synthetic benchmark row {index:D6}");
                transaction.PaymentMethod = index % 3 == 0 ? "UPI" : "Card";
                transaction.ManualLocation = index % 4 == 0 ? "Synthetic City" : null;
                batch.Add(transaction);
            }

            db.Transactions.AddRange(batch);
            await db.SaveChangesAsync().ConfigureAwait(false);
            db.ChangeTracker.Clear();
            account = await db.Accounts.SingleAsync(item => item.Id == account.Id).ConfigureAwait(false);
            categories = await db.Categories.OrderBy(item => item.SortOrder).Take(8).ToListAsync().ConfigureAwait(false);
        }
    }

    public static void TryDelete(string path)
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
}

internal sealed class PerformanceDbContextFactory(DbContextOptions<FinoraDbContext> options) : IDbContextFactory<FinoraDbContext>
{
    public FinoraDbContext CreateDbContext() => new(options);
    public Task<FinoraDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CreateDbContext());
    }
}

internal static class BenchmarkCsv
{
    public const string AccountName = "Performance Account";

    public static byte[] Create(int rows)
    {
        var builder = new StringBuilder(capacity: Math.Min(rows * 120, 32_000_000));
        builder.AppendLine("Date,Type,AmountMinor,Account,Currency,Category,Merchant,Note");
        var startedAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (var index = 0; index < rows; index++)
        {
            var type = index % 5 == 0 ? "Income" : "Expense";
            builder.Append(startedAt.AddMinutes(index * 7L).ToString("O", CultureInfo.InvariantCulture)).Append(',')
                .Append(type).Append(',')
                .Append((100L + (index % 250_000)).ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(AccountName).Append(',')
                .Append("INR,Food,")
                .Append("Import Merchant ").Append((index % 500).ToString("D4", CultureInfo.InvariantCulture)).Append(',')
                .Append("Synthetic import row ").Append(index.ToString("D6", CultureInfo.InvariantCulture)).AppendLine();
        }
        return Encoding.UTF8.GetBytes(builder.ToString());
    }
}

internal static class ProgramBenchmarkSecrets
{
    public const string Password = "Finora-Performance-Lab-Only-2026!";
}

internal readonly record struct OperationResult(long Units, string UnitLabel);

internal sealed record PerformanceMeasurement(
    string Name,
    int Iterations,
    double MinimumMilliseconds,
    double MedianMilliseconds,
    double MaximumMilliseconds,
    long MedianAllocatedBytes,
    long ResultUnits,
    string ResultUnitLabel);

internal sealed record PerformanceReport(
    DateTimeOffset CreatedAtUtc,
    int DatasetRows,
    int Iterations,
    string DotnetVersion,
    string OperatingSystem,
    int ProcessorCount,
    bool ServerGc,
    IReadOnlyList<PerformanceMeasurement> Measurements);

internal static class GCSettingsInfo
{
    public static bool ServerGc => System.Runtime.GCSettings.IsServerGC;
}
