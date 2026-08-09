using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using Finora.Application;
using Finora.Domain;

namespace Finora.App;

public sealed class TransactionDetailViewModel : ViewModelBase
{
    private readonly ITransactionMaintenanceService _maintenance;
    private readonly IFinanceStore _store;
    private readonly ICategoryTagService _categoryTags;
    private Guid _transactionId;
    private TransactionType _type;
    private AccountSummary? _account;
    private Category? _category;
    private string _amount = string.Empty;
    private DateTime _transactionDate = DateTime.Today;
    private TimeSpan _transactionTime;
    private string _merchant = string.Empty;
    private string _note = string.Empty;
    private string _paymentMethod = string.Empty;
    private string _manualLocation = string.Empty;
    private Category? _newSplitCategory;
    private string _newSplitAmount = string.Empty;
    private string _newSplitNote = string.Empty;
    private SplitEditorItem? _selectedSplit;
    private AttachmentInfo? _selectedAttachment;
    private bool _isDeleted;
    private bool _isTransfer;
    private string _status = string.Empty;

    public TransactionDetailViewModel(ITransactionMaintenanceService maintenance, IFinanceStore store, ICategoryTagService categoryTags)
    {
        _maintenance = maintenance;
        _store = store;
        _categoryTags = categoryTags;
        SaveCommand = new AsyncCommand(SaveAsync);
        AddSplitCommand = new AsyncCommand(AddSplitAsync);
        RemoveSplitCommand = new AsyncCommand(RemoveSplitAsync);
        DeleteCommand = new AsyncCommand(DeleteAsync);
        RestoreCommand = new AsyncCommand(RestoreAsync);
    }

    public ObservableCollection<AccountSummary> Accounts { get; } = [];
    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<SelectableTagItem> Tags { get; } = [];
    public ObservableCollection<SplitEditorItem> Splits { get; } = [];
    public ObservableCollection<AttachmentInfo> Attachments { get; } = [];
    public ObservableCollection<TransactionRevisionInfo> Revisions { get; } = [];
    public IReadOnlyList<TransactionType> TransactionTypes { get; } = [TransactionType.Expense, TransactionType.Income, TransactionType.Refund, TransactionType.Adjustment];
    public TransactionType Type { get => _type; set => SetProperty(ref _type, value); }
    public AccountSummary? Account { get => _account; set => SetProperty(ref _account, value); }
    public Category? Category { get => _category; set => SetProperty(ref _category, value); }
    public string Amount { get => _amount; set => SetProperty(ref _amount, value); }
    public DateTime TransactionDate { get => _transactionDate; set => SetProperty(ref _transactionDate, value); }
    public TimeSpan TransactionTime { get => _transactionTime; set => SetProperty(ref _transactionTime, value); }
    public string Merchant { get => _merchant; set => SetProperty(ref _merchant, value); }
    public string Note { get => _note; set => SetProperty(ref _note, value); }
    public string PaymentMethod { get => _paymentMethod; set => SetProperty(ref _paymentMethod, value); }
    public string ManualLocation { get => _manualLocation; set => SetProperty(ref _manualLocation, value); }
    public Category? NewSplitCategory { get => _newSplitCategory; set => SetProperty(ref _newSplitCategory, value); }
    public string NewSplitAmount { get => _newSplitAmount; set => SetProperty(ref _newSplitAmount, value); }
    public string NewSplitNote { get => _newSplitNote; set => SetProperty(ref _newSplitNote, value); }
    public SplitEditorItem? SelectedSplit { get => _selectedSplit; set => SetProperty(ref _selectedSplit, value); }
    public AttachmentInfo? SelectedAttachment { get => _selectedAttachment; set => SetProperty(ref _selectedAttachment, value); }
    public bool IsDeleted { get => _isDeleted; private set => SetProperty(ref _isDeleted, value); }
    public bool IsTransfer { get => _isTransfer; private set => SetProperty(ref _isTransfer, value); }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public Guid TransactionId => _transactionId;
    public System.Windows.Input.ICommand SaveCommand { get; }
    public System.Windows.Input.ICommand AddSplitCommand { get; }
    public System.Windows.Input.ICommand RemoveSplitCommand { get; }
    public System.Windows.Input.ICommand DeleteCommand { get; }
    public System.Windows.Input.ICommand RestoreCommand { get; }

    public Task LoadAsync(Guid transactionId) => RunAsync(async () =>
    {
        _transactionId = transactionId;
        Accounts.Clear();
        foreach (var account in await _store.GetAccountsAsync()) Accounts.Add(account);
        Categories.Clear();
        foreach (var category in await _store.GetCategoriesAsync()) Categories.Add(category);
        var tagInfos = await _categoryTags.GetTagsAsync(false);
        var result = await _maintenance.GetTransactionAsync(transactionId);
        if (!result.IsSuccess || result.Value is null) throw new InvalidOperationException(result.Error);
        var detail = result.Value;
        Type = detail.Type;
        IsTransfer = detail.Type == TransactionType.Transfer;
        Account = Accounts.FirstOrDefault(x => x.Id == detail.AccountId);
        Category = Categories.FirstOrDefault(x => x.Id == detail.CategoryId);
        var displayMinor = detail.Type is TransactionType.Expense or TransactionType.Transfer ? Math.Abs(detail.AmountMinor) : detail.AmountMinor;
        Amount = new Money(displayMinor, detail.Currency).ToMajorUnits().ToString("0.00", CultureInfo.CurrentCulture);
        var local = detail.OccurredAtUtc.ToLocalTime();
        TransactionDate = local.Date;
        TransactionTime = local.TimeOfDay;
        Merchant = detail.Merchant ?? string.Empty;
        Note = detail.Note ?? string.Empty;
        PaymentMethod = detail.PaymentMethod ?? string.Empty;
        ManualLocation = detail.ManualLocation ?? string.Empty;
        IsDeleted = detail.IsDeleted;

        Splits.Clear();
        foreach (var split in detail.Splits)
        {
            var amount = new Money(Math.Abs(split.AmountMinor), detail.Currency).ToMajorUnits().ToString("0.00", CultureInfo.CurrentCulture);
            Splits.Add(new SplitEditorItem(split.CategoryId, Categories.FirstOrDefault(x => x.Id == split.CategoryId)?.Name ?? "Uncategorized", amount, split.Note));
        }
        Tags.Clear();
        var selectedTagIds = detail.Tags.Select(x => x.Id).ToHashSet();
        foreach (var tag in tagInfos) Tags.Add(new SelectableTagItem(tag.Id, tag.Name, selectedTagIds.Contains(tag.Id)));
        ReplaceAttachments(detail.Attachments);
        ReplaceRevisions(detail.Revisions);
        Status = string.Empty;
        OnPropertyChanged(nameof(TransactionId));
    });

    public async Task ReloadAsync()
    {
        if (_transactionId != Guid.Empty) await LoadAsync(_transactionId);
    }

    private Task SaveAsync() => RunAsync(async () =>
    {
        if (Account is null) throw new InvalidOperationException("Choose an account.");
        if (!TryParseDecimal(Amount, out var major) || major == 0) throw new InvalidOperationException("Enter a non-zero amount.");
        var unsignedMinor = Math.Abs(Money.FromMajorUnits(major, Account.Currency).MinorUnits);
        var occurredLocal = DateTime.SpecifyKind(TransactionDate.Date + TransactionTime, DateTimeKind.Local);
        if (occurredLocal > DateTime.Now.AddMinutes(5)) throw new InvalidOperationException("Transaction time cannot be in the future.");
        var occurredAt = new DateTimeOffset(occurredLocal).ToUniversalTime();

        if (IsTransfer)
        {
            var transferResult = await _maintenance.UpdateTransferAsync(_transactionId, unsignedMinor, occurredAt, Note);
            if (!transferResult.IsSuccess) throw new InvalidOperationException(transferResult.Error);
        }
        else
        {
            var amountMinor = Type switch
            {
                TransactionType.Expense => -unsignedMinor,
                TransactionType.Income or TransactionType.Refund => unsignedMinor,
                TransactionType.Adjustment => major < 0 ? -unsignedMinor : unsignedMinor,
                _ => unsignedMinor
            };
            var splitInputs = Splits.Select(x =>
            {
                if (!TryParseDecimal(x.AmountMajor, out var splitMajor) || splitMajor <= 0) throw new InvalidOperationException("Each split must have a positive amount.");
                var splitMinor = Math.Abs(Money.FromMajorUnits(splitMajor, Account.Currency).MinorUnits);
                if (amountMinor < 0) splitMinor = -splitMinor;
                return new TransactionSplitInput(x.CategoryId, splitMinor, x.Note);
            }).ToList();
            var request = new TransactionEditRequest(_transactionId, Type, amountMinor, Account.Currency, Account.Id, Category?.Id, occurredAt, Merchant, Note, PaymentMethod, ManualLocation, splitInputs, Tags.Where(x => x.IsSelected).Select(x => x.Id).ToList());
            var result = await _maintenance.UpdateTransactionAsync(request);
            if (!result.IsSuccess) throw new InvalidOperationException(result.Error);
        }
        Status = "Transaction saved with revision history.";
        await ReloadCoreAsync();
    });

    private Task AddSplitAsync() => RunAsync(() =>
    {
        if (Account is null) throw new InvalidOperationException("Choose an account first.");
        if (!TryParseDecimal(NewSplitAmount, out var amount) || amount <= 0) throw new InvalidOperationException("Enter a positive split amount.");
        Splits.Add(new SplitEditorItem(NewSplitCategory?.Id, NewSplitCategory?.Name ?? "Uncategorized", amount.ToString("0.00", CultureInfo.CurrentCulture), string.IsNullOrWhiteSpace(NewSplitNote) ? null : NewSplitNote.Trim()));
        NewSplitAmount = string.Empty;
        NewSplitNote = string.Empty;
        NewSplitCategory = null;
        return Task.CompletedTask;
    });

    private Task RemoveSplitAsync() => RunAsync(() =>
    {
        if (SelectedSplit is null) throw new InvalidOperationException("Choose a split to remove.");
        Splits.Remove(SelectedSplit);
        SelectedSplit = null;
        return Task.CompletedTask;
    });

    private Task DeleteAsync() => RunAsync(async () =>
    {
        if (IsDeleted) return;
        await _store.SoftDeleteTransactionAsync(_transactionId);
        IsDeleted = true;
        Status = "Transaction deleted. You can restore it from this screen while it remains in the local database.";
        await ReloadCoreAsync();
    });

    private Task RestoreAsync() => RunAsync(async () =>
    {
        if (!IsDeleted) return;
        await _store.RestoreDeletedTransactionAsync(_transactionId);
        IsDeleted = false;
        Status = "Transaction restored.";
        await ReloadCoreAsync();
    });

    private async Task ReloadCoreAsync()
    {
        var result = await _maintenance.GetTransactionAsync(_transactionId);
        if (!result.IsSuccess || result.Value is null) return;
        IsDeleted = result.Value.IsDeleted;
        ReplaceAttachments(result.Value.Attachments);
        ReplaceRevisions(result.Value.Revisions);
    }

    private void ReplaceAttachments(IReadOnlyList<AttachmentInfo> values)
    {
        Attachments.Clear();
        foreach (var item in values) Attachments.Add(item);
        SelectedAttachment = Attachments.FirstOrDefault();
    }

    private void ReplaceRevisions(IReadOnlyList<TransactionRevisionInfo> values)
    {
        Revisions.Clear();
        foreach (var item in values) Revisions.Add(item);
    }

    private static bool TryParseDecimal(string value, out decimal result) => decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.CurrentCulture, out result) || decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result);
}

public sealed class SelectableTagItem(Guid id, string name, bool isSelected) : INotifyPropertyChanged
{
    private bool _isSelected = isSelected;
    public Guid Id { get; } = id;
    public string Name { get; } = name;
    public bool IsSelected { get => _isSelected; set { if (_isSelected == value) return; _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); } }
    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed record SplitEditorItem(Guid? CategoryId, string CategoryName, string AmountMajor, string? Note);
