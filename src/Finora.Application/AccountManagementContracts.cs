using Finora.Domain;
using Finora.Shared;

namespace Finora.Application;

public sealed record AccountDetail(
    Guid Id,
    string Name,
    AccountType Type,
    string Icon,
    string? ColorLabel,
    string Currency,
    long OpeningBalanceMinor,
    long CurrentBalanceMinor,
    AccountState State,
    long? CreditLimitMinor,
    int? BillingDay,
    DateTimeOffset? LastReconciledAtUtc,
    long? ReconciledBalanceMinor,
    int TransactionCount);

public sealed record AccountUpdateRequest(
    Guid AccountId,
    string Name,
    AccountType Type,
    string Icon,
    string? ColorLabel,
    long OpeningBalanceMinor,
    long? CreditLimitMinor,
    int? BillingDay,
    AccountState State);

public interface IAccountManagementService
{
    Task<Result<AccountDetail>> GetAccountAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<Result> UpdateAccountAsync(AccountUpdateRequest request, CancellationToken cancellationToken = default);
    Task<Result> ArchiveAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<Result> RestoreAsync(Guid accountId, CancellationToken cancellationToken = default);
}
