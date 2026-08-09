using Finora.Shared;

namespace Finora.Application;

public sealed record SampleDataResetResult(
    int AccountsCreated,
    int TransactionsCreated,
    int BudgetsCreated,
    int GoalsCreated,
    int RecurrenceRulesCreated);

public interface ISampleDataService
{
    Task<Result<SampleDataResetResult>> ResetToSyntheticSampleDataAsync(
        string currency,
        CancellationToken cancellationToken = default);
}
