using Finora.Domain;
using Finora.Shared;

namespace Finora.Application;

public sealed record RecurrenceOccurrenceInfo(Guid Id, Guid RuleId, string RuleName, DateOnly DueOn, OccurrenceStatus Status, long AmountMinor, string Currency, long? PaidAmountMinor, DateOnly? PostponedTo, Guid? GeneratedTransactionId);

public interface IRecurringWorkflowService
{
    Task<IReadOnlyList<DateOnly>> PreviewNextOccurrencesAsync(Guid ruleId, int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecurrenceOccurrenceInfo>> GetOccurrencesAsync(DateOnly? from = null, DateOnly? to = null, bool includeCompleted = true, CancellationToken cancellationToken = default);
    Task<Result> MarkPaidAsync(Guid occurrenceId, long? paidAmountMinor = null, CancellationToken cancellationToken = default);
    Task<Result> SkipAsync(Guid occurrenceId, CancellationToken cancellationToken = default);
    Task<Result> PostponeAsync(Guid occurrenceId, DateOnly newDate, CancellationToken cancellationToken = default);
}
