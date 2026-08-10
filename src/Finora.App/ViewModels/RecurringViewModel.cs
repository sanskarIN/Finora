using System.Collections.ObjectModel;
using System.Globalization;
using Finora.Application;
using Finora.Domain;

namespace Finora.App;

public sealed class RecurringViewModel : ViewModelBase
{
    private readonly IFinanceStore _store;
    private readonly IRecurringWorkflowService _workflow;
    private readonly IAppSettingsService _settings;
    private readonly ReminderCoordinator _reminders;
    private string _name = string.Empty;
    private string _amount = string.Empty;
    private AccountSummary? _account;
    private AccountSummary? _destinationAccount;
    private Category? _category;
    private RecurrenceFrequency _frequency = RecurrenceFrequency.Monthly;
    private TransactionType _type = TransactionType.Expense;
    private int _interval = 1;
    private DateTime _startsOn = DateTime.Today;
    private DateTime _endsOn = DateTime.Today.AddYears(1);
    private bool _hasEndDate;
    private int _gracePeriodDays;
    private int _reminderMinutesBefore = 60;
    private string _merchant = string.Empty;
    private string _note = string.Empty;
    private RecurrenceRule? _selectedRule;
    private RecurrenceOccurrenceInfo? _selectedOccurrence;
    private string _paidAmount = string.Empty;
    private DateTime _postponeDate = DateTime.Today.AddDays(1);
    private string _processingResult = string.Empty;

    public RecurringViewModel(
        IFinanceStore store,
        IRecurringWorkflowService workflow,
        IAppSettingsService settings,
        ReminderCoordinator reminders)
    {
        _store = store;
        _workflow = workflow;
        _settings = settings;
        _reminders = reminders;
        RefreshCommand = new AsyncCommand(LoadAsync);
        AddCommand = new AsyncCommand(AddAsync);
        ProcessNowCommand = new AsyncCommand(ProcessNowAsync);
        PauseRuleCommand = new AsyncCommand(PauseRuleAsync);
        ResumeRuleCommand = new AsyncCommand(ResumeRuleAsync);
        ArchiveRuleCommand = new AsyncCommand(ArchiveRuleAsync);
        MarkPaidCommand = new AsyncCommand(MarkPaidAsync);
        SkipCommand = new AsyncCommand(SkipAsync);
        PostponeCommand = new AsyncCommand(PostponeAsync);
        ReopenCommand = new AsyncCommand(ReopenAsync);
    }

    public ObservableCollection<RecurrenceRule> Rules { get; } = [];
    public ObservableCollection<RecurrenceOccurrenceInfo> Occurrences { get; } = [];
    public ObservableCollection<AccountSummary> Accounts { get; } = [];
    public ObservableCollection<Category> Categories { get; } = [];
    public IReadOnlyList<RecurrenceFrequency> Frequencies { get; } = Enum.GetValues<RecurrenceFrequency>();
    public IReadOnlyList<TransactionType> Types { get; } = [TransactionType.Expense, TransactionType.Income, TransactionType.Transfer, TransactionType.Refund];
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Amount { get => _amount; set => SetProperty(ref _amount, value); }
    public AccountSummary? Account { get => _account; set => SetProperty(ref _account, value); }
    public AccountSummary? DestinationAccount { get => _destinationAccount; set => SetProperty(ref _destinationAccount, value); }
    public Category? Category { get => _category; set => SetProperty(ref _category, value); }
    public RecurrenceFrequency Frequency { get => _frequency; set => SetProperty(ref _frequency, value); }
    public TransactionType Type { get => _type; set { if (SetProperty(ref _type, value)) OnPropertyChanged(nameof(IsTransfer)); } }
    public bool IsTransfer => Type == TransactionType.Transfer;
    public int Interval { get => _interval; set => SetProperty(ref _interval, Math.Clamp(value, 1, 365)); }
    public DateTime StartsOn { get => _startsOn; set => SetProperty(ref _startsOn, value.Date); }
    public DateTime EndsOn { get => _endsOn; set => SetProperty(ref _endsOn, value.Date); }
    public bool HasEndDate { get => _hasEndDate; set => SetProperty(ref _hasEndDate, value); }
    public int GracePeriodDays { get => _gracePeriodDays; set => SetProperty(ref _gracePeriodDays, Math.Clamp(value, 0, 90)); }
    public int ReminderMinutesBefore { get => _reminderMinutesBefore; set => SetProperty(ref _reminderMinutesBefore, Math.Clamp(value, 0, 10080)); }
    public string Merchant { get => _merchant; set => SetProperty(ref _merchant, value); }
    public string Note { get => _note; set => SetProperty(ref _note, value); }
    public RecurrenceRule? SelectedRule
    {
        get => _selectedRule;
        set
        {
            if (!SetProperty(ref _selectedRule, value)) return;
            OnPropertyChanged(nameof(CanPauseSelectedRule));
            OnPropertyChanged(nameof(CanResumeSelectedRule));
            OnPropertyChanged(nameof(CanArchiveSelectedRule));
        }
    }
    public bool CanPauseSelectedRule => SelectedRule?.Status == RecurrenceStatus.Active;
    public bool CanResumeSelectedRule => SelectedRule?.Status == RecurrenceStatus.Paused;
    public bool CanArchiveSelectedRule => SelectedRule is not null && SelectedRule.Status != RecurrenceStatus.Archived;
    public RecurrenceOccurrenceInfo? SelectedOccurrence
    {
        get => _selectedOccurrence;
        set
        {
            if (!SetProperty(ref _selectedOccurrence, value)) return;
            OnPropertyChanged(nameof(CanReopenSelectedOccurrence));
            if (value is null) return;
            PaidAmount = new Money(value.AmountMinor, value.Currency).ToMajorUnits().ToString($"N{CurrencyMinorUnits.GetDecimalPlaces(value.Currency)}", CultureInfo.CurrentCulture);
            PostponeDate = (value.PostponedTo ?? value.DueOn).ToDateTime(TimeOnly.MinValue).AddDays(1);
        }
    }
    public bool CanReopenSelectedOccurrence => SelectedOccurrence?.Status == OccurrenceStatus.Skipped;
    public string PaidAmount { get => _paidAmount; set => SetProperty(ref _paidAmount, value); }
    public DateTime PostponeDate { get => _postponeDate; set => SetProperty(ref _postponeDate, value.Date); }
    public string ProcessingResult { get => _processingResult; private set => SetProperty(ref _processingResult, value); }
    public System.Windows.Input.ICommand RefreshCommand { get; }
    public System.Windows.Input.ICommand AddCommand { get; }
    public System.Windows.Input.ICommand ProcessNowCommand { get; }
    public System.Windows.Input.ICommand PauseRuleCommand { get; }
    public System.Windows.Input.ICommand ResumeRuleCommand { get; }
    public System.Windows.Input.ICommand ArchiveRuleCommand { get; }
    public System.Windows.Input.ICommand MarkPaidCommand { get; }
    public System.Windows.Input.ICommand SkipCommand { get; }
    public System.Windows.Input.ICommand PostponeCommand { get; }
    public System.Windows.Input.ICommand ReopenCommand { get; }

    public Task LoadAsync() => RunAsync(async () =>
    {
        Accounts.Clear();
        foreach (var item in await _store.GetAccountsAsync()) if (item.State != AccountState.Archived) Accounts.Add(item);
        Categories.Clear();
        foreach (var item in await _store.GetCategoriesAsync()) if (!item.IsArchived) Categories.Add(item);
        Account ??= Accounts.FirstOrDefault(x => x.Id == _settings.DefaultAccountId) ?? Accounts.FirstOrDefault();
        await LoadRulesCoreAsync();
        await LoadOccurrencesCoreAsync();
    });

    private Task AddAsync() => RunAsync(async () =>
    {
        if (Account is null) throw new InvalidOperationException("Choose an account.");
        if (string.IsNullOrWhiteSpace(Name)) throw new InvalidOperationException("Rule name is required.");
        if (!TryParse(Amount, out var major) || major <= 0) throw new InvalidOperationException("Enter a positive amount.");
        if (HasEndDate && EndsOn.Date < StartsOn.Date) throw new InvalidOperationException("End date cannot be earlier than the start date.");
        if (IsTransfer)
        {
            if (DestinationAccount is null || DestinationAccount.Id == Account.Id) throw new InvalidOperationException("Choose a different destination account for the transfer.");
            if (!string.Equals(Account.Currency, DestinationAccount.Currency, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Recurring transfers currently require matching account currencies.");
        }

        var start = DateOnly.FromDateTime(StartsOn);
        var minor = Money.FromMajorUnits(major, Account.Currency).MinorUnits;
        var rule = new RecurrenceRule
        {
            Name = Name.Trim(),
            Frequency = Frequency,
            Interval = Math.Clamp(Interval, 1, 365),
            StartsOn = start,
            EndsOn = HasEndDate ? DateOnly.FromDateTime(EndsOn) : null,
            NextDueOn = start,
            TransactionType = Type,
            AmountMinor = minor,
            Currency = Account.Currency,
            AccountId = Account.Id,
            DestinationAccountId = IsTransfer ? DestinationAccount?.Id : null,
            CategoryId = IsTransfer ? null : Category?.Id,
            DayOfMonth = Frequency is RecurrenceFrequency.Monthly or RecurrenceFrequency.Yearly ? start.Day : null,
            DayOfWeek = Frequency == RecurrenceFrequency.Weekly ? start.DayOfWeek : null,
            GracePeriodDays = GracePeriodDays,
            ReminderMinutesBefore = ReminderMinutesBefore,
            Merchant = string.IsNullOrWhiteSpace(Merchant) ? null : Merchant.Trim(),
            Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim()
        };
        await _store.SaveRecurrenceRuleAsync(rule);
        Name = Amount = Merchant = Note = string.Empty;
        SelectedRule = rule;
        await SyncRemindersIfEnabledAsync();
        await LoadRulesCoreAsync(rule.Id);
        await LoadOccurrencesCoreAsync();
        ProcessingResult = "Recurring item saved.";
    });

    private Task ProcessNowAsync() => RunAsync(async () =>
    {
        var count = await _store.ProcessDueRecurrencesAsync(DateOnly.FromDateTime(DateTime.Today));
        ProcessingResult = $"Prepared {count} due occurrence(s). No transaction is created until you mark an occurrence paid.";
        await SyncRemindersIfEnabledAsync();
        await LoadRulesCoreAsync();
        await LoadOccurrencesCoreAsync();
    });

    private Task PauseRuleAsync() => ChangeRuleStateAsync(_workflow.PauseRuleAsync, "Recurring rule paused.");
    private Task ResumeRuleAsync() => ChangeRuleStateAsync(_workflow.ResumeRuleAsync, "Recurring rule resumed.");
    private Task ArchiveRuleAsync() => ChangeRuleStateAsync(_workflow.ArchiveRuleAsync, "Recurring rule archived. Existing occurrence history was preserved.");

    private Task ChangeRuleStateAsync(Func<Guid, CancellationToken, Task<Finora.Shared.Result>> change, string successMessage) => RunAsync(async () =>
    {
        if (SelectedRule is null) throw new InvalidOperationException("Choose a recurring rule.");
        var selectedId = SelectedRule.Id;
        var result = await change(selectedId, CancellationToken.None);
        if (!result.IsSuccess) throw new InvalidOperationException(result.Error);
        ProcessingResult = successMessage;
        await SyncRemindersIfEnabledAsync();
        await LoadRulesCoreAsync(selectedId);
        await LoadOccurrencesCoreAsync();
    });

    private Task MarkPaidAsync() => RunAsync(async () =>
    {
        if (SelectedOccurrence is null) throw new InvalidOperationException("Choose a due occurrence.");
        if (!TryParse(PaidAmount, out var major) || major <= 0) throw new InvalidOperationException("Enter a positive paid amount.");
        var minor = Money.FromMajorUnits(major, SelectedOccurrence.Currency).MinorUnits;
        var result = await _workflow.MarkPaidAsync(SelectedOccurrence.Id, minor);
        if (!result.IsSuccess) throw new InvalidOperationException(result.Error);
        ProcessingResult = minor == SelectedOccurrence.AmountMinor ? "Occurrence marked paid." : "Partial payment recorded.";
        await LoadOccurrencesCoreAsync();
    });

    private Task SkipAsync() => RunAsync(async () =>
    {
        if (SelectedOccurrence is null) throw new InvalidOperationException("Choose a due occurrence.");
        var result = await _workflow.SkipAsync(SelectedOccurrence.Id);
        if (!result.IsSuccess) throw new InvalidOperationException(result.Error);
        ProcessingResult = "Occurrence skipped without creating a transaction.";
        await LoadOccurrencesCoreAsync();
    });

    private Task PostponeAsync() => RunAsync(async () =>
    {
        if (SelectedOccurrence is null) throw new InvalidOperationException("Choose a due occurrence.");
        var result = await _workflow.PostponeAsync(SelectedOccurrence.Id, DateOnly.FromDateTime(PostponeDate));
        if (!result.IsSuccess) throw new InvalidOperationException(result.Error);
        ProcessingResult = $"Occurrence postponed to {PostponeDate:d}.";
        await LoadOccurrencesCoreAsync();
    });

    private Task ReopenAsync() => RunAsync(async () =>
    {
        if (SelectedOccurrence is null) throw new InvalidOperationException("Choose a skipped occurrence.");
        var result = await _workflow.ReopenAsync(SelectedOccurrence.Id);
        if (!result.IsSuccess) throw new InvalidOperationException(result.Error);
        ProcessingResult = "Skipped occurrence reopened as pending.";
        await LoadOccurrencesCoreAsync();
    });

    private async Task LoadRulesCoreAsync(Guid? preferredId = null)
    {
        var selectedId = preferredId ?? SelectedRule?.Id;
        Rules.Clear();
        foreach (var rule in await _store.GetRecurrenceRulesAsync()) Rules.Add(rule);
        SelectedRule = Rules.FirstOrDefault(x => x.Id == selectedId) ?? Rules.FirstOrDefault();
    }

    private async Task LoadOccurrencesCoreAsync()
    {
        var selectedId = SelectedOccurrence?.Id;
        Occurrences.Clear();
        foreach (var occurrence in await _workflow.GetOccurrencesAsync(DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)), DateOnly.FromDateTime(DateTime.Today.AddMonths(6)), true))
            Occurrences.Add(occurrence);
        SelectedOccurrence = Occurrences.FirstOrDefault(x => x.Id == selectedId)
            ?? Occurrences.FirstOrDefault(x => x.Status is OccurrenceStatus.Pending or OccurrenceStatus.Postponed or OccurrenceStatus.PartiallyPaid or OccurrenceStatus.Skipped)
            ?? Occurrences.FirstOrDefault();
    }

    private Task SyncRemindersIfEnabledAsync()
        => _settings.NotificationsEnabled ? _reminders.SyncAsync() : Task.CompletedTask;

    private static bool TryParse(string value, out decimal result)
        => decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result)
           || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
}
