namespace Finora.Performance;

internal sealed record PerformanceMeasurement(
    string Name,
    int Iteration,
    double ElapsedMilliseconds,
    long ManagedHeapBeforeBytes,
    long ManagedHeapAfterBytes,
    long WorkingSetBeforeBytes,
    long WorkingSetAfterBytes,
    long? OutputBytes = null,
    long? ItemCount = null);

internal sealed record PerformanceDatasetSummary(
    int Accounts,
    int Categories,
    int Transactions,
    int Attachments,
    int RecurrenceRules,
    int Budgets,
    int SavingsGoals,
    long DatabaseBytes,
    long AttachmentBytes);

internal sealed record PerformanceRunReport(
    string Product,
    string HarnessVersion,
    string RuntimeVersion,
    string OperatingSystem,
    string ProcessorArchitecture,
    int ProcessorCount,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    PerformanceDatasetSummary Dataset,
    IReadOnlyList<string> Operations,
    int Iterations,
    IReadOnlyList<PerformanceMeasurement> Measurements,
    IReadOnlyDictionary<string, string> Notes);