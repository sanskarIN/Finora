using Finora.Application;
using Finora.Domain;
using Finora.Shared;
using Microsoft.EntityFrameworkCore;

namespace Finora.Infrastructure;

public sealed class AccountManagementService(IDbContextFactory<FinoraDbContext> factory) : IAccountManagementService
{
    private readonly IDbContextFactory<FinoraDbContext> _factory = factory;

    public async Task<Result<AccountDetail>> GetAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var account = await db.Accounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == accountId, cancellationToken).ConfigureAwait(false);
            if (account is null) return Result<AccountDetail>.Failure("Account not found.");
            DomainRules.ValidateAccount(account);

            var amounts = await db.Transactions.AsNoTracking()
                .Where(x => x.AccountId == accountId && !x.IsDeleted)
                .Select(x => x.AmountMinor)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            long transactionTotal = 0;
            foreach (var amount in amounts) transactionTotal = checked(transactionTotal + amount);
            var balance = checked(account.OpeningBalanceMinor + transactionTotal);

            return Result<AccountDetail>.Success(new AccountDetail(
                account.Id,
                account.Name,
                account.Type,
                account.Icon,
                account.ColorLabel,
                account.Currency,
                account.OpeningBalanceMinor,
                balance,
                account.State,
                account.CreditLimitMinor,
                account.BillingDay,
                account.LastReconciledAtUtc,
                account.ReconciledBalanceMinor,
                amounts.Count));
        }
        catch (OverflowException)
        {
            return Result<AccountDetail>.Failure("Account balance is outside the supported 64-bit minor-unit range. Run the data-integrity check before editing this account.");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return Result<AccountDetail>.Failure("Account data failed domain validation. Run the data-integrity check before editing this account.");
        }
    }

    public async Task<Result> UpdateAccountAsync(AccountUpdateRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var account = await db.Accounts.SingleOrDefaultAsync(x => x.Id == request.AccountId, cancellationToken).ConfigureAwait(false);
        if (account is null) return Result.Failure("Account not found.");

        var candidate = new Account
        {
            Id = account.Id,
            Name = request.Name,
            Type = request.Type,
            Icon = string.IsNullOrWhiteSpace(request.Icon) ? "wallet" : request.Icon.Trim(),
            ColorLabel = string.IsNullOrWhiteSpace(request.ColorLabel) ? null : request.ColorLabel.Trim(),
            Currency = account.Currency,
            OpeningBalanceMinor = request.OpeningBalanceMinor,
            State = request.State,
            CreditLimitMinor = request.Type == AccountType.CreditCard ? request.CreditLimitMinor : null,
            BillingDay = request.Type == AccountType.CreditCard ? request.BillingDay : null,
            LastReconciledAtUtc = account.LastReconciledAtUtc,
            ReconciledBalanceMinor = account.ReconciledBalanceMinor,
            CreatedAtUtc = account.CreatedAtUtc,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        try { DomainRules.ValidateAccount(candidate); }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return Result.Failure(exception.Message);
        }

        if (account.LastReconciledAtUtc is not null && account.OpeningBalanceMinor != candidate.OpeningBalanceMinor)
            return Result.Failure("Opening balance cannot be changed after this account has reconciliation history. Use an explicit adjustment transaction instead.");
        if (candidate.State == AccountState.Archived && account.State != AccountState.Archived && await HasActiveRecurrenceAsync(db, account.Id, cancellationToken).ConfigureAwait(false))
            return Result.Failure("Pause, complete, or archive recurring items that use this account before archiving the account.");

        db.Entry(account).CurrentValues.SetValues(candidate);
        db.AuditEntries.Add(new AuditEntry { EntityType = "Account", EntityId = account.Id, Action = "Updated" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public Task<Result> ArchiveAsync(Guid accountId, CancellationToken cancellationToken = default)
        => SetStateAsync(accountId, AccountState.Archived, cancellationToken);

    public Task<Result> RestoreAsync(Guid accountId, CancellationToken cancellationToken = default)
        => SetStateAsync(accountId, AccountState.Active, cancellationToken);

    private async Task<Result> SetStateAsync(Guid accountId, AccountState state, CancellationToken cancellationToken)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var account = await db.Accounts.SingleOrDefaultAsync(x => x.Id == accountId, cancellationToken).ConfigureAwait(false);
        if (account is null) return Result.Failure("Account not found.");
        if (account.State == state) return Result.Success();
        if (state == AccountState.Archived && await HasActiveRecurrenceAsync(db, account.Id, cancellationToken).ConfigureAwait(false))
            return Result.Failure("Pause, complete, or archive recurring items that use this account before archiving the account.");

        account.State = state;
        account.UpdatedAtUtc = DateTimeOffset.UtcNow;
        db.AuditEntries.Add(new AuditEntry { EntityType = "Account", EntityId = account.Id, Action = state == AccountState.Archived ? "Archived" : "Restored" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static Task<bool> HasActiveRecurrenceAsync(FinoraDbContext db, Guid accountId, CancellationToken cancellationToken)
        => db.RecurrenceRules.AsNoTracking().AnyAsync(
            rule => rule.Status == RecurrenceStatus.Active && (rule.AccountId == accountId || rule.DestinationAccountId == accountId),
            cancellationToken);
}
