using System.Collections.ObjectModel;
using System.Globalization;
using Finora.Application;
using Finora.Domain;

namespace Finora.App;

public sealed class AccountDetailViewModel : ViewModelBase
{
    private readonly IAccountManagementService _accounts;
    private readonly IFinanceStore _store;
    private Guid _accountId;
    private string _name = string.Empty;
    private AccountType _type;
    private string _icon = "wallet";
    private string _colorLabel = string.Empty;
    private string _openingBalance = "0";
    private string _creditLimit = string.Empty;
    private int _billingDay = 1;
    private AccountState _state;
    private string _summary = string.Empty;
    private string _status = string.Empty;

    public AccountDetailViewModel(IAccountManagementService accounts, IFinanceStore store)
    {
        _accounts = accounts;
        _store = store;
        SaveCommand = new AsyncCommand(SaveAsync);
        ArchiveOrRestoreCommand = new AsyncCommand(ArchiveOrRestoreAsync);
    }

    public ObservableCollection<TransactionListItem> Transactions { get; } = [];
    public IReadOnlyList<AccountType> AccountTypes { get; } = Enum.GetValues<AccountType>();
    public IReadOnlyList<AccountState> States { get; } = Enum.GetValues<AccountState>();
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public AccountType Type { get => _type; set { if (SetProperty(ref _type, value)) OnPropertyChanged(nameof(IsCreditCard)); } }
    public bool IsCreditCard => Type == AccountType.CreditCard;
    public string Icon { get => _icon; set => SetProperty(ref _icon, value); }
    public string ColorLabel { get => _colorLabel; set => SetProperty(ref _colorLabel, value); }
    public string OpeningBalance { get => _openingBalance; set => SetProperty(ref _openingBalance, value); }
    public string CreditLimit { get => _creditLimit; set => SetProperty(ref _creditLimit, value); }
    public int BillingDay { get => _billingDay; set => SetProperty(ref _billingDay, Math.Clamp(value, 1, 31)); }
    public AccountState State { get => _state; set => SetProperty(ref _state, value); }
    public string Summary { get => _summary; private set => SetProperty(ref _summary, value); }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public System.Windows.Input.ICommand SaveCommand { get; }
    public System.Windows.Input.ICommand ArchiveOrRestoreCommand { get; }

    public Task LoadAsync(Guid id) => RunAsync(async () =>
    {
        _accountId = id;
        var result = await _accounts.GetAccountAsync(id);
        if (!result.IsSuccess || result.Value is null) throw new InvalidOperationException(result.Error);
        var account = result.Value;
        Name = account.Name;
        Type = account.Type;
        Icon = account.Icon;
        ColorLabel = account.ColorLabel ?? string.Empty;
        OpeningBalance = new Money(account.OpeningBalanceMinor, account.Currency).ToMajorUnits().ToString("0.00", CultureInfo.CurrentCulture);
        CreditLimit = account.CreditLimitMinor is long limit ? new Money(limit, account.Currency).ToMajorUnits().ToString("0.00", CultureInfo.CurrentCulture) : string.Empty;
        BillingDay = account.BillingDay ?? 1;
        State = account.State;
        Summary = $"Current balance: {new Money(account.CurrentBalanceMinor, account.Currency).Format()} · {account.TransactionCount} transaction(s)" + (account.LastReconciledAtUtc is DateTimeOffset reconciled ? $" · Last reconciled {reconciled.ToLocalTime():g}" : string.Empty);
        Transactions.Clear();
        foreach (var tx in await _store.SearchTransactionsAsync(accountId: id)) Transactions.Add(tx);
    });

    private Task SaveAsync() => RunAsync(async () =>
    {
        var current = await _accounts.GetAccountAsync(_accountId);
        if (!current.IsSuccess || current.Value is null) throw new InvalidOperationException(current.Error);
        if (!TryParseDecimal(OpeningBalance, out var opening)) throw new InvalidOperationException("Enter a valid opening balance.");
        long? credit = null;
        if (IsCreditCard && !string.IsNullOrWhiteSpace(CreditLimit))
        {
            if (!TryParseDecimal(CreditLimit, out var creditMajor) || creditMajor < 0) throw new InvalidOperationException("Enter a valid non-negative credit limit.");
            credit = Money.FromMajorUnits(creditMajor, current.Value.Currency).MinorUnits;
        }
        var result = await _accounts.UpdateAccountAsync(new AccountUpdateRequest(
            _accountId,
            Name,
            Type,
            Icon,
            ColorLabel,
            Money.FromMajorUnits(opening, current.Value.Currency).MinorUnits,
            credit,
            IsCreditCard ? BillingDay : null,
            State));
        if (!result.IsSuccess) throw new InvalidOperationException(result.Error);
        Status = "Account settings saved.";
        await LoadAsync(_accountId);
    });

    private Task ArchiveOrRestoreAsync() => RunAsync(async () =>
    {
        var wasArchived = State == AccountState.Archived;
        var result = wasArchived ? await _accounts.RestoreAsync(_accountId) : await _accounts.ArchiveAsync(_accountId);
        if (!result.IsSuccess) throw new InvalidOperationException(result.Error);
        await LoadAsync(_accountId);
        Status = wasArchived ? "Account restored." : "Account archived. Existing transactions were preserved.";
    });

    private static bool TryParseDecimal(string value, out decimal result)
        => decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.CurrentCulture, out result)
           || decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result);
}
