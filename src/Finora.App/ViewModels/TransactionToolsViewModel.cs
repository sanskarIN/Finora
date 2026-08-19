using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using Finora.Application;
using Finora.Domain;
using Finora.Shared;

namespace Finora.App;

public sealed class TransactionToolsViewModel : ViewModelBase
{
    private readonly IFinanceStore _store;
    private readonly ITransactionMaintenanceService _maintenance;
    private Category? _bulkCategory;
    private string _status = string.Empty;
    private DateTime _fromDate = DateTime.Today.AddMonths(-1);
    private DateTime _toDate = DateTime.Today;

    public TransactionToolsViewModel(IFinanceStore store, ITransactionMaintenanceService maintenance)
    {
        _store = store;
        _maintenance = maintenance;
        LoadCommand = new AsyncCommand(LoadAsync);
        BulkCategorizeCommand = new AsyncCommand(BulkCategorizeAsync);
        ScanDuplicatesCommand = new AsyncCommand(ScanDuplicatesAsync);
    }

    public ObservableCollection<ToolTransactionItem> Transactions { get; } = [];
    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<DuplicateTransactionCandidate> Duplicates { get; } = [];
    public Category? BulkCategory { get => _bulkCategory; set => SetProperty(ref _bulkCategory, value); }
    public DateTime FromDate { get => _fromDate; set => SetProperty(ref _fromDate, value.Date); }
    public DateTime ToDate { get => _toDate; set => SetProperty(ref _toDate, value.Date); }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public System.Windows.Input.ICommand LoadCommand { get; }
    public System.Windows.Input.ICommand BulkCategorizeCommand { get; }
    public System.Windows.Input.ICommand ScanDuplicatesCommand { get; }

    public Task LoadAsync() => RunAsync(async () =>
    {
        Categories.Clear();
        foreach (var category in await _store.GetCategoriesAsync()) if (!category.IsArchived) Categories.Add(category);
        await LoadTransactionsCoreAsync();
        await ScanDuplicatesCoreAsync();
    });

    private Task BulkCategorizeAsync() => RunAsync(async () =>
    {
        var ids = Transactions.Where(x => x.IsSelected).Select(x => x.Id).ToArray();
        if (ids.Length == 0) throw new InvalidOperationException(LocalizationResources.Get("TransactionToolsSelectOne"));
        var count = await _maintenance.BulkCategorizeAsync(ids, BulkCategory?.Id);
        Status = Format("TransactionToolsUpdatedFormat", count);
        await LoadTransactionsCoreAsync();
    });

    private Task ScanDuplicatesAsync() => RunAsync(ScanDuplicatesCoreAsync);

    private async Task LoadTransactionsCoreAsync()
    {
        var range = ResolveRange();
        Transactions.Clear();
        foreach (var tx in await _store.SearchTransactionsAsync(from: range.FromUtc, to: range.ToExclusiveUtc.AddTicks(-1)))
            Transactions.Add(new ToolTransactionItem(tx));
    }

    private async Task ScanDuplicatesCoreAsync()
    {
        var range = ResolveRange();
        Duplicates.Clear();
        foreach (var item in await _maintenance.FindLikelyDuplicatesAsync(range.FromUtc, range.ToExclusiveUtc.AddTicks(-1))) Duplicates.Add(item);
        Status = Duplicates.Count == 0
            ? LocalizationResources.Get("TransactionToolsNoDuplicatesPeriod")
            : Format("TransactionToolsFoundDuplicatesFormat", Duplicates.Count);
    }

    private UtcDateRange ResolveRange()
    {
        if (ToDate.Date < FromDate.Date) throw new InvalidOperationException(LocalizationResources.Get("TransactionToolsEndBeforeStart"));
        return LocalDateRange.ToUtc(DateOnly.FromDateTime(FromDate), DateOnly.FromDateTime(ToDate), TimeZoneInfo.Local);
    }

    private static string Format(string key, params object[] values)
        => string.Format(CultureInfo.CurrentCulture, LocalizationResources.Get(key), values);
}

public sealed class ToolTransactionItem : INotifyPropertyChanged
{
    private bool _isSelected;
    public ToolTransactionItem(TransactionListItem item) { Id = item.Id; Type = item.Type; AmountMinor = item.AmountMinor; Currency = item.Currency; OccurredAtUtc = item.OccurredAtUtc; AccountName = item.AccountName; CategoryName = item.CategoryName; Merchant = item.Merchant; }
    public Guid Id { get; }
    public TransactionType Type { get; }
    public long AmountMinor { get; }
    public string Currency { get; }
    public DateTimeOffset OccurredAtUtc { get; }
    public string AccountName { get; }
    public string? CategoryName { get; }
    public string? Merchant { get; }
    public bool IsSelected { get => _isSelected; set { if (_isSelected == value) return; _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); } }
    public event PropertyChangedEventHandler? PropertyChanged;
}
