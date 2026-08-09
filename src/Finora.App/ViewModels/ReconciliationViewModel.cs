using System.Collections.ObjectModel;
using System.Globalization;
using Finora.Application;
using Finora.Domain;

namespace Finora.App;

public sealed class ReconciliationViewModel : ViewModelBase
{
    private readonly IFinanceStore _store;
    private readonly IReconciliationService _service;
    private AccountSummary? _account;
    private string _statementBalance = string.Empty;
    private DateTime _statementDate = DateTime.Today;
    private bool _createAdjustment;
    private string _note = string.Empty;
    private string _previewText = "Choose an account and enter the statement balance.";
    private string _status = string.Empty;

    public ReconciliationViewModel(IFinanceStore store, IReconciliationService service)
    {
        _store = store; _service = service;
        RefreshCommand = new AsyncCommand(LoadAsync);
        PreviewCommand = new AsyncCommand(PreviewAsync);
        CompleteCommand = new AsyncCommand(CompleteAsync);
    }

    public ObservableCollection<AccountSummary> Accounts { get; } = [];
    public ObservableCollection<ReconciliationHistoryItem> History { get; } = [];
    public AccountSummary? Account { get => _account; set { if (SetProperty(ref _account, value)) _ = LoadHistoryAsync(); } }
    public string StatementBalance { get => _statementBalance; set => SetProperty(ref _statementBalance, value); }
    public DateTime StatementDate { get => _statementDate; set => SetProperty(ref _statementDate, value); }
    public bool CreateAdjustment { get => _createAdjustment; set => SetProperty(ref _createAdjustment, value); }
    public string Note { get => _note; set => SetProperty(ref _note, value); }
    public string PreviewText { get => _previewText; private set => SetProperty(ref _previewText, value); }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public System.Windows.Input.ICommand RefreshCommand { get; }
    public System.Windows.Input.ICommand PreviewCommand { get; }
    public System.Windows.Input.ICommand CompleteCommand { get; }

    public Task LoadAsync() => RunAsync(async () =>
    {
        var selected = Account?.Id;
        Accounts.Clear(); foreach (var account in await _store.GetAccountsAsync()) if (account.State != AccountState.Archived) Accounts.Add(account);
        Account = Accounts.FirstOrDefault(x => x.Id == selected) ?? Accounts.FirstOrDefault();
        await LoadHistoryAsync();
    });

    private Task PreviewAsync() => RunAsync(async () =>
    {
        var (account, minor, date) = ReadInput();
        var result = await _service.PreviewAsync(account.Id, minor, date);
        if (!result.IsSuccess || result.Value is null) throw new InvalidOperationException(result.Error);
        var p = result.Value;
        PreviewText = $"Book balance: {new Money(p.BookBalanceMinor, p.Currency).Format()}\nStatement balance: {new Money(p.StatementBalanceMinor, p.Currency).Format()}\nDifference: {new Money(p.DifferenceMinor, p.Currency).Format()}";
    });

    private Task CompleteAsync() => RunAsync(async () =>
    {
        var (account, minor, date) = ReadInput();
        var result = await _service.CompleteAsync(account.Id, minor, date, CreateAdjustment, Note);
        if (!result.IsSuccess) throw new InvalidOperationException(result.Error);
        Status = result.Value?.AdjustmentCreated == true ? "Reconciliation completed with an explicit adjustment transaction." : "Reconciliation completed.";
        await LoadHistoryAsync(); await LoadAccountsBalancesAsync(account.Id);
    });

    private (AccountSummary Account, long Minor, DateTimeOffset Date) ReadInput()
    {
        if (Account is null) throw new InvalidOperationException("Choose an account.");
        if (!decimal.TryParse(StatementBalance, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.CurrentCulture, out var major) && !decimal.TryParse(StatementBalance, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out major)) throw new InvalidOperationException("Enter a valid statement balance.");
        var minor = Money.FromMajorUnits(major, Account.Currency).MinorUnits;
        var local = DateTime.SpecifyKind(StatementDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59), DateTimeKind.Local);
        return (Account, minor, new DateTimeOffset(local).ToUniversalTime());
    }

    private async Task LoadHistoryAsync()
    {
        History.Clear(); if (Account is null) return; foreach (var item in await _service.GetHistoryAsync(Account.Id)) History.Add(item);
    }

    private async Task LoadAccountsBalancesAsync(Guid preferredId)
    {
        Accounts.Clear(); foreach (var item in await _store.GetAccountsAsync()) if (item.State != AccountState.Archived) Accounts.Add(item); Account = Accounts.FirstOrDefault(x => x.Id == preferredId);
    }
}
