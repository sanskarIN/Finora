using System.Collections.ObjectModel;
using System.Globalization;
using Finora.Application;
using Finora.Domain;

namespace Finora.App;

public sealed class SavingsViewModel : ViewModelBase
{
    private static readonly int[] MilestonePercents = [25, 50, 75, 100];
    private readonly IFinanceStore _store;
    private readonly IAppSettingsService _settings;
    private string _name = string.Empty;
    private string _target = string.Empty;
    private string _starting = "0";
    private string _notes = string.Empty;
    private string _icon = "target";
    private DateTime _targetDate = DateTime.Today.AddMonths(6);
    private SavingsGoalSnapshot? _selectedGoal;
    private string _contribution = string.Empty;
    private string _contributionNote = string.Empty;
    private TransactionListItem? _linkedTransaction;
    private string _forecast = LocalizationResources.Get("SelectGoalForecast");
    private string _milestones = string.Empty;
    private string _status = string.Empty;
    private bool _showCompletionCelebration;

    public SavingsViewModel(IFinanceStore store, IAppSettingsService settings) { _store = store; _settings = settings; RefreshCommand = new AsyncCommand(LoadAsync); AddGoalCommand = new AsyncCommand(AddGoalAsync); ContributeCommand = new AsyncCommand(ContributeAsync); }
    public ObservableCollection<SavingsGoalSnapshot> Goals { get; } = [];
    public ObservableCollection<TransactionListItem> RecentTransactions { get; } = [];
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Target { get => _target; set => SetProperty(ref _target, value); }
    public string Starting { get => _starting; set => SetProperty(ref _starting, value); }
    public string Notes { get => _notes; set => SetProperty(ref _notes, value); }
    public string Icon { get => _icon; set => SetProperty(ref _icon, value); }
    public DateTime TargetDate { get => _targetDate; set => SetProperty(ref _targetDate, value.Date); }
    public SavingsGoalSnapshot? SelectedGoal { get => _selectedGoal; set { if (SetProperty(ref _selectedGoal, value)) UpdatePlanningText(); } }
    public string Contribution { get => _contribution; set => SetProperty(ref _contribution, value); }
    public string ContributionNote { get => _contributionNote; set => SetProperty(ref _contributionNote, value); }
    public TransactionListItem? LinkedTransaction { get => _linkedTransaction; set => SetProperty(ref _linkedTransaction, value); }
    public string Forecast { get => _forecast; private set => SetProperty(ref _forecast, value); }
    public string Milestones { get => _milestones; private set => SetProperty(ref _milestones, value); }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public bool ShowCompletionCelebration { get => _showCompletionCelebration; private set => SetProperty(ref _showCompletionCelebration, value); }
    public System.Windows.Input.ICommand RefreshCommand { get; }
    public System.Windows.Input.ICommand AddGoalCommand { get; }
    public System.Windows.Input.ICommand ContributeCommand { get; }

    public Task LoadAsync() => RunAsync(LoadCoreAsync);

    private Task AddGoalAsync() => RunAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(Name)) throw new InvalidOperationException(LocalizationResources.Get("GoalNameRequired"));
        if (!TryParse(Target, out var target) || target <= 0) throw new InvalidOperationException(LocalizationResources.Get("EnterPositiveTarget"));
        if (!TryParse(Starting, out var starting) || starting < 0) throw new InvalidOperationException(LocalizationResources.Get("StartingAmountNotNegative"));
        var targetMinor = Money.FromMajorUnits(target, _settings.DefaultCurrency).MinorUnits; var startingMinor = Money.FromMajorUnits(starting, _settings.DefaultCurrency).MinorUnits;
        if (startingMinor > targetMinor) throw new InvalidOperationException(LocalizationResources.Get("StartingAmountNotExceedTarget"));
        await _store.SaveSavingsGoalAsync(new SavingsGoal { Name = Name.Trim(), TargetMinor = targetMinor, StartingMinor = startingMinor, Currency = _settings.DefaultCurrency, TargetDate = DateOnly.FromDateTime(TargetDate), Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(), Icon = string.IsNullOrWhiteSpace(Icon) ? "target" : Icon.Trim(), IsCompleted = startingMinor >= targetMinor });
        Name = Target = Notes = string.Empty; Starting = "0"; Icon = "target"; await LoadCoreAsync();
    });

    private Task ContributeAsync() => RunAsync(async () =>
    {
        if (SelectedGoal is null) throw new InvalidOperationException(LocalizationResources.Get("ChooseSavingsGoal"));
        if (!TryParse(Contribution, out var major) || major == 0) throw new InvalidOperationException(LocalizationResources.Get("EnterNonZeroContribution"));
        var before = SelectedGoal.Progress;
        await _store.AddGoalContributionAsync(new GoalContribution { SavingsGoalId = SelectedGoal.Id, AmountMinor = Money.FromMajorUnits(major, SelectedGoal.Currency).MinorUnits, OccurredAtUtc = DateTimeOffset.UtcNow, TransactionId = LinkedTransaction?.Id, Note = string.IsNullOrWhiteSpace(ContributionNote) ? null : ContributionNote.Trim() });
        var selectedId = SelectedGoal.Id; Contribution = ContributionNote = string.Empty; LinkedTransaction = null; await LoadCoreAsync(); SelectedGoal = Goals.FirstOrDefault(x => x.Id == selectedId);
        ShowCompletionCelebration = before < 1 && SelectedGoal?.Progress >= 1;
        Status = ShowCompletionCelebration
            ? LocalizationResources.Get("GoalCompletedCongratulations")
            : major > 0
                ? LocalizationResources.Get("ContributionRecorded")
                : LocalizationResources.Get("WithdrawalRecorded");
    });

    private async Task LoadCoreAsync()
    {
        var selectedId = SelectedGoal?.Id; Goals.Clear(); foreach (var goal in await _store.GetSavingsGoalsAsync()) Goals.Add(goal); SelectedGoal = Goals.FirstOrDefault(x => x.Id == selectedId) ?? Goals.FirstOrDefault();
        RecentTransactions.Clear(); foreach (var tx in await _store.SearchTransactionsAsync(from: DateTimeOffset.UtcNow.AddMonths(-12))) RecentTransactions.Add(tx); UpdatePlanningText();
    }

    private void UpdatePlanningText()
    {
        if (SelectedGoal is null) { Forecast = LocalizationResources.Get("SelectGoalForecast"); Milestones = string.Empty; return; }
        var goal = SelectedGoal; var percent = (int)Math.Round(goal.Progress * 100, MidpointRounding.AwayFromZero); var achieved = MilestonePercents.Where(x => percent >= x).ToArray(); var next = MilestonePercents.FirstOrDefault(x => percent < x);
        Milestones = achieved.Length == 0
            ? Format("NextMilestoneFormat", next)
            : next == 0
                ? LocalizationResources.Get("MilestonesAchievedAll")
                : Format("AchievedNextFormat", string.Join(", ", achieved.Select(x => $"{x}%")), next);
        if (goal.TargetDate is null || goal.CurrentMinor >= goal.TargetMinor)
        {
            Forecast = goal.CurrentMinor >= goal.TargetMinor
                ? LocalizationResources.Get("TargetReached")
                : LocalizationResources.Get("AddTargetDateForecast");
            return;
        }

        var days = Math.Max(1, goal.TargetDate.Value.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber); var remaining = checked(goal.TargetMinor - goal.CurrentMinor); var months = Math.Max(1m, days / 30.4375m); var monthly = (long)Math.Ceiling(remaining / months);
        Forecast = _settings.PrivacyMode || _settings.HideAmountsOnLaunch
            ? Format("ForecastPrivacyFormat", goal.TargetDate.Value, days)
            : Format("ForecastAmountFormat", goal.TargetDate.Value, new Money(monthly, goal.Currency).Format(), days);
    }

    private static string Format(string key, params object[] values)
        => string.Format(CultureInfo.CurrentCulture, LocalizationResources.Get(key), values);

    private static bool TryParse(string value, out decimal result) => decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.CurrentCulture, out result) || decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result);
}
