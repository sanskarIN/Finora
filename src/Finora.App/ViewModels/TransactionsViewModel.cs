using System.Collections.ObjectModel;
using System.Globalization;
using Finora.Application;
using Finora.Domain;
using Finora.Shared;

namespace Finora.App;

public sealed class TransactionsViewModel : ViewModelBase
{
    private const int PageSize = 50;
    private readonly IFinanceStore _store;
    private readonly IAppSettingsService _settings;
    private TransactionListItem[] _allMatches = [];
    private string _searchText = string.Empty;
    private TransactionType _type;
    private AccountSummary? _selectedAccount;
    private Category? _selectedCategory;
    private string _amount = string.Empty;
    private string _merchant = string.Empty;
    private string _note = string.Empty;
    private string _paymentMethod = string.Empty;
    private string _manualLocation = string.Empty;
    private DateTime _occurredDate = DateTime.Today;
    private TimeSpan _occurredTime = DateTime.Now.TimeOfDay;
    private string _calculatorExpression = string.Empty;
    private bool _showAdvancedFilters;
    private AccountSummary? _filterAccount;
    private Category? _filterCategory;
    private string _filterType = "All";
    private DateTime _filterFromDate = DateTime.Today.AddMonths(-3);
    private DateTime _filterToDate = DateTime.Today;
    private string _sortOrder = "Newest first";

    public TransactionsViewModel(IFinanceStore store, IAppSettingsService settings)
    {
        _store = store;
        _settings = settings;
        _type = settings.DefaultTransactionType;
        RefreshCommand = new AsyncCommand(LoadAsync);
        SearchCommand = new AsyncCommand(SearchAsync);
        AddCommand = new AsyncCommand(AddAsync);
        LoadMoreCommand = new AsyncCommand(LoadMoreAsync);
        ToggleFiltersCommand = new Command(() => ShowAdvancedFilters = !ShowAdvancedFilters);
        ClearFiltersCommand = new AsyncCommand(ClearFiltersAsync);
    }

    public ObservableCollection<TransactionListItem> Transactions { get; } = [];
    public ObservableCollection<AccountSummary> Accounts { get; } = [];
    public ObservableCollection<Category> Categories { get; } = [];
    public IReadOnlyList<TransactionType> TransactionTypes { get; } = [TransactionType.Expense, TransactionType.Income, TransactionType.Refund, TransactionType.Adjustment];
    public IReadOnlyList<string> FilterTypes { get; } = ["All", "Expense", "Income", "Transfer", "Refund", "Adjustment"];
    public IReadOnlyList<string> SortOrders { get; } = ["Newest first", "Oldest first", "Amount high to low", "Amount low to high", "Merchant A–Z"];
    public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }
    public TransactionType Type { get => _type; set => SetProperty(ref _type, value); }
    public AccountSummary? SelectedAccount { get => _selectedAccount; set => SetProperty(ref _selectedAccount, value); }
    public Category? SelectedCategory { get => _selectedCategory; set => SetProperty(ref _selectedCategory, value); }
    public string Amount { get => _amount; set => SetProperty(ref _amount, value); }
    public string Merchant { get => _merchant; set => SetProperty(ref _merchant, value); }
    public string Note { get => _note; set => SetProperty(ref _note, value); }
    public string PaymentMethod { get => _paymentMethod; set => SetProperty(ref _paymentMethod, value); }
    public string ManualLocation { get => _manualLocation; set => SetProperty(ref _manualLocation, value); }
    public DateTime OccurredDate { get => _occurredDate; set => SetProperty(ref _occurredDate, value); }
    public TimeSpan OccurredTime { get => _occurredTime; set => SetProperty(ref _occurredTime, value); }
    public string CalculatorExpression { get => _calculatorExpression; set => SetProperty(ref _calculatorExpression, value); }
    public bool ShowAdvancedFilters { get => _showAdvancedFilters; set => SetProperty(ref _showAdvancedFilters, value); }
    public AccountSummary? FilterAccount { get => _filterAccount; set => SetProperty(ref _filterAccount, value); }
    public Category? FilterCategory { get => _filterCategory; set => SetProperty(ref _filterCategory, value); }
    public string FilterType { get => _filterType; set => SetProperty(ref _filterType, value); }
    public DateTime FilterFromDate { get => _filterFromDate; set => SetProperty(ref _filterFromDate, value); }
    public DateTime FilterToDate { get => _filterToDate; set => SetProperty(ref _filterToDate, value); }
    public string SortOrder { get => _sortOrder; set => SetProperty(ref _sortOrder, value); }
    public bool HasMore => Transactions.Count < _allMatches.Length;
    public string HistoryStatus => _allMatches.Length == 0 ? "No matching transactions." : $"Showing {Transactions.Count} of {_allMatches.Length} matching transaction(s).";
    public System.Windows.Input.ICommand RefreshCommand { get; }
    public System.Windows.Input.ICommand SearchCommand { get; }
    public System.Windows.Input.ICommand AddCommand { get; }
    public System.Windows.Input.ICommand LoadMoreCommand { get; }
    public System.Windows.Input.ICommand ToggleFiltersCommand { get; }
    public System.Windows.Input.ICommand ClearFiltersCommand { get; }

    public Task LoadAsync() => RunAsync(async () =>
    {
        await LoadLookupsAsync();
        await SearchCoreAsync();
    });

    private Task SearchAsync() => RunAsync(SearchCoreAsync);

    private Task ClearFiltersAsync() => RunAsync(async () =>
    {
        SearchText = string.Empty;
        FilterAccount = null;
        FilterCategory = null;
        FilterType = "All";
        FilterFromDate = DateTime.Today.AddMonths(-3);
        FilterToDate = DateTime.Today;
        SortOrder = "Newest first";
        ShowAdvancedFilters = false;
        await SearchCoreAsync();
    });

    private Task LoadMoreAsync()
    {
        AppendNextPage();
        return Task.CompletedTask;
    }

    private Task AddAsync() => RunAsync(async () =>
    {
        if (SelectedAccount is null) throw new InvalidOperationException("Choose an account.");
        if (!decimal.TryParse(Amount, NumberStyles.Number, CultureInfo.CurrentCulture, out var major) &&
            !decimal.TryParse(Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out major))
            throw new InvalidOperationException("Enter a valid amount.");
        if (major <= 0) throw new InvalidOperationException("Enter a positive amount.");

        var local = DateTime.SpecifyKind(OccurredDate.Date + OccurredTime, DateTimeKind.Local);
        if (local > DateTime.Now.AddMinutes(5)) throw new InvalidOperationException("Transaction time cannot be in the future.");
        var occurredAt = new DateTimeOffset(local).ToUniversalTime();
        var minor = Money.FromMajorUnits(major, SelectedAccount.Currency).MinorUnits;
        var tx = TransactionFactory.Create(Type, minor, SelectedAccount.Currency, SelectedAccount.Id, occurredAt, SelectedCategory?.Id,
            NullIfBlank(Merchant), NullIfBlank(Note));
        tx.PaymentMethod = NullIfBlank(PaymentMethod);
        tx.ManualLocation = NullIfBlank(ManualLocation);
        await _store.SaveTransactionAsync(tx);

        Amount = string.Empty;
        CalculatorExpression = string.Empty;
        Merchant = string.Empty;
        Note = string.Empty;
        PaymentMethod = string.Empty;
        ManualLocation = string.Empty;
        OccurredDate = DateTime.Today;
        OccurredTime = DateTime.Now.TimeOfDay;
        await SearchCoreAsync();
    });

    public void AppendCalculatorToken(string token)
    {
        if (string.IsNullOrEmpty(token)) return;
        var normalized = token switch { "×" => "*", "÷" => "/", _ => token };
        if (normalized.Length != 1 || !"0123456789.+-*/()".Contains(normalized, StringComparison.Ordinal)) return;
        if (CalculatorExpression.Length >= 64) return;
        CalculatorExpression += normalized;
    }

    public void ClearCalculator() => CalculatorExpression = string.Empty;

    public void BackspaceCalculator()
    {
        if (CalculatorExpression.Length > 0) CalculatorExpression = CalculatorExpression[..^1];
    }

    public void EvaluateCalculator()
    {
        try
        {
            var value = DecimalCalculator.Evaluate(CalculatorExpression);
            if (value <= 0) throw new InvalidOperationException("The calculated amount must be positive.");
            Amount = value.ToString("0.############################", CultureInfo.CurrentCulture);
            CalculatorExpression = value.ToString("0.############################", CultureInfo.InvariantCulture);
            ErrorMessage = null;
        }
        catch (Exception ex) when (ex is FormatException or DivideByZeroException or OverflowException or InvalidOperationException)
        {
            ErrorMessage = ex.Message;
        }
    }

    private async Task LoadLookupsAsync()
    {
        Accounts.Clear();
        foreach (var account in await _store.GetAccountsAsync()) if (account.State != AccountState.Archived) Accounts.Add(account);
        Categories.Clear();
        foreach (var category in await _store.GetCategoriesAsync()) if (!category.IsArchived) Categories.Add(category);
        SelectedAccount ??= Accounts.FirstOrDefault(x => x.Id == _settings.DefaultAccountId) ?? Accounts.FirstOrDefault();
    }

    private async Task SearchCoreAsync()
    {
        DateTimeOffset? from = null;
        DateTimeOffset? to = null;
        if (ShowAdvancedFilters)
        {
            if (FilterToDate.Date < FilterFromDate.Date) throw new InvalidOperationException("Filter end date cannot be before start date.");
            var range = LocalDateRange.ToUtc(DateOnly.FromDateTime(FilterFromDate), DateOnly.FromDateTime(FilterToDate), TimeZoneInfo.Local);
            from = range.FromUtc;
            to = range.ToExclusiveUtc.AddTicks(-1);
        }

        var items = await _store.SearchTransactionsAsync(
            string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
            ShowAdvancedFilters ? FilterAccount?.Id : null,
            ShowAdvancedFilters ? FilterCategory?.Id : null,
            from,
            to);

        if (ShowAdvancedFilters && !string.Equals(FilterType, "All", StringComparison.OrdinalIgnoreCase) && Enum.TryParse<TransactionType>(FilterType, true, out var parsed))
            items = items.Where(x => x.Type == parsed).ToArray();

        _allMatches = Sort(items).ToArray();
        Transactions.Clear();
        AppendNextPage();
    }

    private IEnumerable<TransactionListItem> Sort(IEnumerable<TransactionListItem> items) => SortOrder switch
    {
        "Oldest first" => items.OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.Id),
        "Amount high to low" => items.OrderByDescending(x => x.AmountMinor).ThenByDescending(x => x.OccurredAtUtc),
        "Amount low to high" => items.OrderBy(x => x.AmountMinor).ThenByDescending(x => x.OccurredAtUtc),
        "Merchant A–Z" => items.OrderBy(x => x.Merchant ?? string.Empty, StringComparer.CurrentCultureIgnoreCase).ThenByDescending(x => x.OccurredAtUtc),
        _ => items.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
    };

    private void AppendNextPage()
    {
        foreach (var item in _allMatches.Skip(Transactions.Count).Take(PageSize)) Transactions.Add(item);
        OnPropertyChanged(nameof(HasMore));
        OnPropertyChanged(nameof(HistoryStatus));
    }

    private static string? NullIfBlank(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
