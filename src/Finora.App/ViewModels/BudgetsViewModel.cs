using System.Collections.ObjectModel;
using System.Globalization;
using Finora.Application;
using Finora.Domain;

namespace Finora.App;

public sealed class BudgetsViewModel : ViewModelBase
{
    private readonly IFinanceStore _store;
    private readonly IAppSettingsService _settings;
    private readonly ReminderCoordinator _reminders;
    private string _name = string.Empty;
    private string _limit = string.Empty;
    private Category? _category;
    private int _warningThreshold = 80;
    private bool _rollover;
    private BudgetCadence _cadence = BudgetCadence.Monthly;
    private DateTime _customStart = DateTime.Today;
    private DateTime _customEnd = DateTime.Today.AddMonths(1).AddDays(-1);
    private DateTime _viewDate = DateTime.Today;

    public BudgetsViewModel(IFinanceStore store, IAppSettingsService settings, ReminderCoordinator reminders)
    {
        _store = store; _settings = settings; _reminders = reminders;
        RefreshCommand = new AsyncCommand(LoadAsync);
        AddCommand = new AsyncCommand(AddAsync);
    }

    public ObservableCollection<BudgetSnapshot> Budgets { get; } = [];
    public ObservableCollection<Category> Categories { get; } = [];
    public IReadOnlyList<BudgetCadence> Cadences { get; } = Enum.GetValues<BudgetCadence>();
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Limit { get => _limit; set => SetProperty(ref _limit, value); }
    public Category? Category { get => _category; set => SetProperty(ref _category, value); }
    public int WarningThreshold { get => _warningThreshold; set => SetProperty(ref _warningThreshold, Math.Clamp(value, 1, 100)); }
    public bool Rollover { get => _rollover; set => SetProperty(ref _rollover, value); }
    public BudgetCadence Cadence { get => _cadence; set { if (SetProperty(ref _cadence, value)) OnPropertyChanged(nameof(IsCustom)); } }
    public bool IsCustom => Cadence == BudgetCadence.Custom;
    public DateTime CustomStart { get => _customStart; set => SetProperty(ref _customStart, value.Date); }
    public DateTime CustomEnd { get => _customEnd; set => SetProperty(ref _customEnd, value.Date); }
    public DateTime ViewDate { get => _viewDate; set => SetProperty(ref _viewDate, value.Date); }
    public System.Windows.Input.ICommand RefreshCommand { get; }
    public System.Windows.Input.ICommand AddCommand { get; }

    public Task LoadAsync() => RunAsync(async () =>
    {
        Categories.Clear(); foreach (var category in await _store.GetCategoriesAsync()) if (!category.IsArchived) Categories.Add(category);
        await LoadBudgetsCoreAsync();
    });

    private Task AddAsync() => RunAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(Name)) throw new InvalidOperationException("Budget name is required.");
        if (!TryParseDecimal(Limit, out var major) || major <= 0) throw new InvalidOperationException("Enter a positive budget amount.");
        if (IsCustom && CustomEnd.Date < CustomStart.Date) throw new InvalidOperationException("Custom budget end date cannot be before its start date.");
        var minor = Money.FromMajorUnits(major, _settings.DefaultCurrency).MinorUnits;
        var budget = new Budget { Name = Name.Trim(), Kind = Category is null ? BudgetKind.Overall : Category.ParentId is null ? BudgetKind.Category : BudgetKind.Subcategory, Cadence = Cadence, CategoryId = Category?.Id, LimitMinor = minor, Currency = _settings.DefaultCurrency, RolloverEnabled = Rollover && !IsCustom, WarningThresholdPercent = WarningThreshold };
        if (IsCustom) budget.Periods.Add(new BudgetPeriod { BudgetId = budget.Id, StartsOn = DateOnly.FromDateTime(CustomStart), EndsOn = DateOnly.FromDateTime(CustomEnd), PlannedMinor = minor, RolloverMinor = 0 });
        await _store.SaveBudgetAsync(budget);
        Name = string.Empty; Limit = string.Empty;
        if (_settings.NotificationsEnabled) await _reminders.SyncAsync();
        await LoadBudgetsCoreAsync();
    });

    private async Task LoadBudgetsCoreAsync() { Budgets.Clear(); foreach (var budget in await _store.GetBudgetsAsync(DateOnly.FromDateTime(ViewDate))) Budgets.Add(budget); }
    private static bool TryParseDecimal(string value, out decimal result) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result) || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
}
