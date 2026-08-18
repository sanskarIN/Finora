using System.Globalization;

namespace Finora.Performance;

internal sealed record PerformanceOptions(
    int TransactionCount,
    int AttachmentCount,
    int RecurrenceCount,
    int BudgetCount,
    int GoalCount,
    int Iterations,
    IReadOnlySet<string> Operations,
    string OutputPath,
    string? RootPath,
    bool KeepData)
{
    private const int MaximumCsvImportRows = 100_000;
    internal static readonly string[] SupportedOperations = ["startup", "history", "reports", "csv", "pdf", "backup", "integrity"];

    public static PerformanceOptions Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var transactionCount = 10_000;
        var attachmentCount = 25;
        var recurrenceCount = 50;
        var budgetCount = 25;
        var goalCount = 25;
        var iterations = 1;
        var operations = new HashSet<string>(["startup", "history", "reports", "integrity"], StringComparer.OrdinalIgnoreCase);
        var outputPath = Path.Combine("artifacts", "performance", "result.json");
        string? rootPath = null;
        var keepData = false;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "--transactions":
                    transactionCount = ParseInteger(NextValue(args, ref index, argument), argument, 1, 1_000_000);
                    break;
                case "--attachments":
                    attachmentCount = ParseInteger(NextValue(args, ref index, argument), argument, 0, 100_000);
                    break;
                case "--recurrences":
                    recurrenceCount = ParseInteger(NextValue(args, ref index, argument), argument, 0, 100_000);
                    break;
                case "--budgets":
                    budgetCount = ParseInteger(NextValue(args, ref index, argument), argument, 0, 100_000);
                    break;
                case "--goals":
                    goalCount = ParseInteger(NextValue(args, ref index, argument), argument, 0, 100_000);
                    break;
                case "--iterations":
                    iterations = ParseInteger(NextValue(args, ref index, argument), argument, 1, 20);
                    break;
                case "--operations":
                    operations = ParseOperations(NextValue(args, ref index, argument));
                    break;
                case "--output":
                    outputPath = NextValue(args, ref index, argument);
                    break;
                case "--root":
                    rootPath = NextValue(args, ref index, argument);
                    break;
                case "--keep-data":
                    keepData = true;
                    break;
                case "--help" or "-h":
                    throw new PerformanceUsageException(UsageText);
                default:
                    throw new PerformanceUsageException($"Unknown option '{argument}'.{Environment.NewLine}{Environment.NewLine}{UsageText}");
            }
        }

        if (attachmentCount > transactionCount)
            throw new PerformanceUsageException("--attachments cannot exceed --transactions.");
        if (operations.Contains("csv") && transactionCount > MaximumCsvImportRows)
            throw new PerformanceUsageException($"The CSV round-trip benchmark supports at most {MaximumCsvImportRows.ToString("N0", CultureInfo.InvariantCulture)} transactions, matching Finora's CSV import safety limit.");

        return new PerformanceOptions(
            transactionCount,
            attachmentCount,
            recurrenceCount,
            budgetCount,
            goalCount,
            iterations,
            operations,
            outputPath,
            rootPath,
            keepData);
    }

    public static string UsageText => """
Finora performance harness

Usage:
  dotnet run --project tools/Finora.Performance/Finora.Performance.csproj -c Release -- [options]

Options:
  --transactions <1..1000000>   Synthetic transaction count. Default: 10000
  --attachments <0..100000>     Synthetic receipt count. Default: 25
  --recurrences <0..100000>     Recurring-rule count. Default: 50
  --budgets <0..100000>         Budget count. Default: 25
  --goals <0..100000>           Savings-goal count. Default: 25
  --iterations <1..20>          Measurement repetitions. Default: 1
  --operations <csv>            startup,history,reports,csv,pdf,backup,integrity or all
  --output <path>               JSON result path. Default: artifacts/performance/result.json
  --root <path>                 Working-data root. Default: isolated temporary directory
  --keep-data                   Preserve the generated database/files after the run
  --help, -h                    Show this help

The CSV round-trip operation is limited to 100,000 transactions, matching Finora's production CSV import limit.
Timing values are observational evidence only. The harness fails on correctness errors, not arbitrary timing thresholds.
""";

    private static string NextValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
            throw new PerformanceUsageException($"{option} requires a value.");
        index++;
        return args[index];
    }

    private static int ParseInteger(string value, string option, int minimum, int maximum)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed < minimum || parsed > maximum)
            throw new PerformanceUsageException($"{option} must be an integer from {minimum.ToString(CultureInfo.InvariantCulture)} to {maximum.ToString(CultureInfo.InvariantCulture)}.");
        return parsed;
    }

    private static HashSet<string> ParseOperations(string value)
    {
        if (string.Equals(value.Trim(), "all", StringComparison.OrdinalIgnoreCase))
            return new HashSet<string>(SupportedOperations, StringComparer.OrdinalIgnoreCase);

        var parsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!SupportedOperations.Contains(item, StringComparer.OrdinalIgnoreCase))
                throw new PerformanceUsageException($"Unsupported operation '{item}'. Supported values: {string.Join(',', SupportedOperations)}.");
            parsed.Add(item);
        }

        if (parsed.Count == 0)
            throw new PerformanceUsageException("--operations must contain at least one supported operation.");
        return parsed;
    }
}

internal sealed class PerformanceUsageException(string message) : Exception(message);
