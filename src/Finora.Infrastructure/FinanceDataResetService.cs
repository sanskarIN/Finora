using Finora.Application;
using Finora.Shared;
using Microsoft.EntityFrameworkCore;

namespace Finora.Infrastructure;

public sealed class FinanceDataResetService(IDbContextFactory<FinoraDbContext> factory) : IFinanceDataResetService
{
    private readonly IDbContextFactory<FinoraDbContext> _factory = factory;

    public async Task<Result<FinanceResetResult>> DeleteAllFinanceDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            var accountCount = await db.Accounts.CountAsync(cancellationToken).ConfigureAwait(false);
            var transactionCount = await db.Transactions.CountAsync(cancellationToken).ConfigureAwait(false);
            var categoryCount = await db.Categories.CountAsync(cancellationToken).ConfigureAwait(false);
            var tagCount = await db.Tags.CountAsync(cancellationToken).ConfigureAwait(false);
            var budgetCount = await db.Budgets.CountAsync(cancellationToken).ConfigureAwait(false);
            var goalCount = await db.SavingsGoals.CountAsync(cancellationToken).ConfigureAwait(false);
            var recurrenceCount = await db.RecurrenceRules.CountAsync(cancellationToken).ConfigureAwait(false);
            var attachmentCount = await db.Attachments.CountAsync(cancellationToken).ConfigureAwait(false);
            var revisionCount = await db.TransactionRevisions.CountAsync(cancellationToken).ConfigureAwait(false);
            var reconciliationCount = await db.AccountReconciliations.CountAsync(cancellationToken).ConfigureAwait(false);
            var notificationCount = await db.NotificationSchedules.CountAsync(cancellationToken).ConfigureAwait(false);

            await db.TransactionRevisions.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await db.AccountReconciliations.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await db.NotificationSchedules.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await db.TransactionTags.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await db.TransactionSplits.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await db.Attachments.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await db.RecurrenceOccurrences.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await db.GoalContributions.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await db.BudgetPeriods.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await db.Transactions.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await db.RecurrenceRules.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await db.Budgets.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await db.SavingsGoals.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await db.Tags.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

            // Categories are self-referencing with Restrict delete behavior. Delete leaves first.
            while (await db.Categories.AnyAsync(cancellationToken).ConfigureAwait(false))
            {
                var leafIds = await db.Categories
                    .Where(category => !db.Categories.Any(child => child.ParentId == category.Id))
                    .Select(category => category.Id)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (leafIds.Count == 0)
                    return Result<FinanceResetResult>.Failure("Category hierarchy is cyclic; finance reset was rolled back.");

                await db.Categories.Where(category => leafIds.Contains(category.Id)).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            }

            await db.Accounts.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await db.AuditEntries.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await db.BackupMetadata.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return Result<FinanceResetResult>.Success(new FinanceResetResult(
                accountCount,
                transactionCount,
                categoryCount,
                tagCount,
                budgetCount,
                goalCount,
                recurrenceCount,
                attachmentCount,
                revisionCount,
                reconciliationCount,
                notificationCount));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is DbUpdateException or InvalidOperationException)
        {
            return Result<FinanceResetResult>.Failure("Finora could not delete all finance data safely. No partial reset was committed.");
        }
    }
}
