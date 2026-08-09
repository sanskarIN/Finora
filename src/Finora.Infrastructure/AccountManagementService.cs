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
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var account = await db.Accounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == accountId, cancellationToken).ConfigureAwait(false);
        if (account is null) return Result<AccountDetail>.Failure("Account not found.");
        var transactions = await db.Transactions.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted).ToListAsync(cancellationToken).ConfigureAwait(false);
        var balance = checked(account.OpeningBalanceMinor + transactions.Sum(x => x.AmountMinor));
        return Result<AccountDetail>.Success(new AccountDetail(account.Id, account.Name, account.Type, account.Icon, account.ColorLabel, account.Currency, account.OpeningBalanceMinor, balance, account.State, account.CreditLimitMinor, account.BillingDay, account.LastReconciledAtUtc, account.ReconciledBalanceMinor, transactions.Count));
    }

    public async Task<Result> UpdateAccountAsync(AccountUpdateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return Result.Failure("Account name is required.");
        if (request.BillingDay is < 1 or > 28) return Result.Failure("Billing day must be between 1 and 28.");
        if (request.CreditLimitMinor is < 0) return Result.Failure("Credit limit cannot be negative.");
        if (request.Type != AccountType.CreditCard && (request.CreditLimitMinor is not null || request.BillingDay is not null)) return Result.Failure("Credit limit and billing day apply only to credit-card accounts.");
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var account = await db.Accounts.SingleOrDefaultAsync(x => x.Id == request.AccountId, cancellationToken).ConfigureAwait(false);
        if (account is null) return Result.Failure("Account not found.");
        account.Name = request.Name.Trim();
        account.Type = request.Type;
        account.Icon = string.IsNullOrWhiteSpace(request.Icon) ? "wallet" : request.Icon.Trim();
        account.ColorLabel = string.IsNullOrWhiteSpace(request.ColorLabel) ? null : request.ColorLabel.Trim();
        account.OpeningBalanceMinor = request.OpeningBalanceMinor;
        account.CreditLimitMinor = request.Type == AccountType.CreditCard ? request.CreditLimitMinor : null;
        account.BillingDay = request.Type == AccountType.CreditCard ? request.BillingDay : null;
        account.State = request.State;
        account.UpdatedAtUtc = DateTimeOffset.UtcNow;
        db.AuditEntries.Add(new AuditEntry { EntityType = "Account", EntityId = account.Id, Action = "Updated" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public Task<Result> ArchiveAsync(Guid accountId, CancellationToken cancellationToken = default) => SetStateAsync(accountId, AccountState.Archived, cancellationToken);
    public Task<Result> RestoreAsync(Guid accountId, CancellationToken cancellationToken = default) => SetStateAsync(accountId, AccountState.Active, cancellationToken);

    private async Task<Result> SetStateAsync(Guid accountId, AccountState state, CancellationToken cancellationToken)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var account = await db.Accounts.SingleOrDefaultAsync(x => x.Id == accountId, cancellationToken).ConfigureAwait(false);
        if (account is null) return Result.Failure("Account not found.");
        account.State = state;
        account.UpdatedAtUtc = DateTimeOffset.UtcNow;
        db.AuditEntries.Add(new AuditEntry { EntityType = "Account", EntityId = account.Id, Action = state == AccountState.Archived ? "Archived" : "Restored" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
