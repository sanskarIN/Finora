using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.Performance;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        PerformanceOptions options;
        try
        {
            options = PerformanceOptions.Parse(args);
        }
        catch (PerformanceUsageException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return ex.Message == PerformanceOptions.UsageText ? 0 : 2;
        }

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

        var ownsRoot = string.IsNullOrWhiteSpace(options.RootPath);
        var rootPath = ownsRoot
            ? Path.Combine(Path.GetTempPath(), $"finora-performance-{Guid.NewGuid():N}")
            : Path.GetFullPath(options.RootPath!);
        var outputPath = Path.GetFullPath(options.OutputPath);
        Directory.CreateDirectory(rootPath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? Directory.GetCurrentDirectory());

        try
        {
            var databasePath = Path.Combine(rootPath, "finora-performance.db3");
            var dbOptions = new DbContextOptionsBuilder<FinoraDbContext>()
                .UseSqlite($"Data Source={databasePath};Cache=Shared")
                .Options;
            var factory = new PerformanceDbFactory(dbOptions);
            var startedAt = DateTimeOffset.UtcNow;

            Console.WriteLine($"Seeding synthetic Finora dataset: {options.TransactionCount:N0} transactions...");
            var seedStopwatch = Stopwatch.StartNew();
            var dataset = await new PerformanceSeeder(factory, rootPath).SeedAsync(options, cancellation.Token).ConfigureAwait(false);
            seedStopwatch.Stop();
            Console.WriteLine($"Synthetic dataset ready in {seedStopwatch.Elapsed.TotalSeconds:F2}s.");

            var measurements = await new PerformanceRunner(factory, rootPath).RunAsync(options, dataset, cancellation.Token).ConfigureAwait(false);
            var completedAt = DateTimeOffset.UtcNow;
            var report = new PerformanceRunReport(
                "Finora",
                "1",
                RuntimeInformation.FrameworkDescription,
                RuntimeInformation.OSDescription,
                RuntimeInformation.ProcessArchitecture.ToString(),
                Environment.ProcessorCount,
                startedAt,
                completedAt,
                dataset,
                options.Operations.OrderBy(operation => operation, StringComparer.OrdinalIgnoreCase).ToArray(),
                options.Iterations,
                measurements,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["DatasetSeedMilliseconds"] = seedStopwatch.Elapsed.TotalMilliseconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture),
                    ["TimingPolicy"] = "Observational only; no arbitrary time threshold is treated as release correctness.",
                    ["DataPolicy"] = "Synthetic deterministic-pattern finance data only; no user finance data is read.",
                    ["PagingPolicy"] = "Offset paging measurements describe a fixed benchmark dataset with no concurrent mutations."
                });

            await using (var stream = File.Create(outputPath))
            {
                await JsonSerializer.SerializeAsync(stream, report, new JsonSerializerOptions { WriteIndented = true }, cancellation.Token).ConfigureAwait(false);
            }

            foreach (var measurement in measurements)
                Console.WriteLine($"{measurement.Name,-30} #{measurement.Iteration}: {measurement.ElapsedMilliseconds,10:F3} ms");
            Console.WriteLine($"JSON: {outputPath}");
            return 0;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            Console.Error.WriteLine("Performance run canceled.");
            return 130;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Performance harness failed: {ex.GetType().Name}: {ex.Message}");
            return 1;
        }
        finally
        {
            if (ownsRoot && !options.KeepData)
            {
                try
                {
                    Directory.Delete(rootPath, recursive: true);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
            else
            {
                Console.WriteLine($"Benchmark data root preserved: {rootPath}");
            }
        }
    }
}