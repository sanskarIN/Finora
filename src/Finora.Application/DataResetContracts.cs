using Finora.Shared;

namespace Finora.Application;

public sealed record FinanceResetResult(
    int Accounts,
    int Transactions,
    int Categories,
    int Tags,
    int Budgets,
    int SavingsGoals,
    int RecurrenceRules,
    int Attachments,
    int Revisions,
    int Reconciliations,
    int Notifications);

public interface IFinanceDataResetService
{
    Task<Result<FinanceResetResult>> DeleteAllFinanceDataAsync(CancellationToken cancellationToken = default);
}
