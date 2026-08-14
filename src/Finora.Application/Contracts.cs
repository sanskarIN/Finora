using Finora.Domain;
using Finora.Shared;

namespace Finora.Application;

public sealed record AccountSummary(Guid Id, string Name, AccountType Type, string Currency, long BalanceMinor, AccountState State);
public sealed record TransactionListItem(Guid Id, TransactionType Type, long AmountMinor, string Currency, DateTimeOffset OccurredAtUtc, string AccountName, string? CategoryName, string? Merchant, string? Note);
public sealed record DashboardSnapshot(long TotalBalanceMinor, long IncomeMinor, long ExpenseMinor, long NetChangeMinor, long RemainingBudgetMinor, IReadOnlyList<TransactionListItem> RecentTransactions, IReadOnlyList<CategorySpend> TopCategories);
public sealed record CategorySpend(string CategoryName, long AmountMinor);
public sealed record BudgetSnapshot(Guid Id, string Name, long PlannedMinor, long ActualMinor, string Currency, int WarningThresholdPercent);
public sealed record SavingsGoalSnapshot(Guid Id, string Name, long TargetMinor, long CurrentMinor, string Currency, DateOnly? TargetDate, double Progress);
public sealed record BackupPreview(int SchemaVersion, DateTimeOffset CreatedAtUtc, int AccountCount, int TransactionCount, int BudgetCount, int SavingsGoalCount);
public sealed record CsvImportRow(int RowNumber, string? Date, string? Amount, string? Type, string? Account, string? Category, string? Merchant, string? Note, bool IsValid, string? Error);

public interface IFinanceStore
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountSummary>> GetAccountsAsync(CancellationToken cancellationToken = default);
    Task<Guid> SaveAccountAsync(Account account, CancellationToken cancellationToken = default);
    Task ArchiveAccountAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TransactionListItem>> SearchTransactionsAsync(string? query = null, Guid? accountId = null, Guid? categoryId = null, DateTimeOffset? from = null, DateTimeOffset? through = null, CancellationToken cancellationToken = default);
    Task<Guid> SaveTransactionAsync(FinanceTransaction transaction, CancellationToken cancellationToken = default);
    Task<(Guid SourceTransactionId, Guid DestinationTransactionId)> RecordTransferAsync(Guid sourceAccountId, Guid destinationAccountId, long amountMinor, DateTimeOffset occurredAtUtc, string? note, CancellationToken cancellationToken = default);
    Task SoftDeleteTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task RestoreDeletedTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<Guid> SaveCategoryAsync(Category category, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BudgetSnapshot>> GetBudgetsAsync(DateOnly periodDate, CancellationToken cancellationToken = default);
    Task<Guid> SaveBudgetAsync(Budget budget, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SavingsGoalSnapshot>> GetSavingsGoalsAsync(CancellationToken cancellationToken = default);
    Task<Guid> SaveSavingsGoalAsync(SavingsGoal goal, CancellationToken cancellationToken = default);
    Task AddGoalContributionAsync(GoalContribution contribution, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecurrenceRule>> GetRecurrenceRulesAsync(CancellationToken cancellationToken = default);
    Task<Guid> SaveRecurrenceRuleAsync(RecurrenceRule rule, CancellationToken cancellationToken = default);
    Task<int> ProcessDueRecurrencesAsync(DateOnly throughDate, CancellationToken cancellationToken = default);
    Task<DashboardSnapshot> GetDashboardAsync(DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken = default);
    Task DeleteAllDataAsync(CancellationToken cancellationToken = default);
}

public interface IBackupService
{
    Task<byte[]> CreateEncryptedBackupAsync(string password, CancellationToken cancellationToken = default);
    Task<Result<BackupPreview>> PreviewEncryptedBackupAsync(Stream backupStream, string password, CancellationToken cancellationToken = default);
    Task<Result> RestoreEncryptedBackupAsync(Stream backupStream, string password, CancellationToken cancellationToken = default);
}

public interface IExportService
{
    Task<string> ExportTransactionsCsvAsync(IReadOnlyCollection<Guid>? transactionIds = null, CancellationToken cancellationToken = default);
    Task<byte[]> ExportTransactionsPdfAsync(IReadOnlyCollection<Guid>? transactionIds = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CsvImportRow>> PreviewCsvAsync(Stream csvStream, CancellationToken cancellationToken = default);
}

public interface IAppSettingsService
{
    string DefaultCurrency { get; set; }
    string Locale { get; set; }
    int FinancialMonthStartDay { get; set; }
    bool PrivacyMode { get; set; }
    bool HideAmountsOnLaunch { get; set; }
    ThemePreference Theme { get; set; }
    bool ReducedMotion { get; set; }
    bool BackupRemindersEnabled { get; set; }
    bool OnboardingComplete { get; set; }
    int AutoLockMinutes { get; set; }
    bool LocalPremiumDemoEnabled { get; set; }
    bool NotificationsEnabled { get; set; }
    bool BiometricUnlockEnabled { get; set; }
    bool SensitiveScreenProtectionEnabled { get; set; }
    int ReceiptImageQuality { get; set; }
    bool LargerInterface { get; set; }
    Guid? DefaultAccountId { get; set; }
    TransactionType DefaultTransactionType { get; set; }
    DateTimeOffset? LastBackupAtUtc { get; set; }
    bool DashboardShowBalance { get; set; }
    bool DashboardShowIncomeExpense { get; set; }
    bool DashboardShowBudget { get; set; }
    bool DashboardShowUpcoming { get; set; }
    bool DashboardShowCategories { get; set; }
    bool DashboardShowGoals { get; set; }
    bool DashboardShowRecent { get; set; }
    bool DashboardShowCashFlow { get; set; }
}

public interface IAppLockService
{
    Task<bool> HasPinAsync();
    Task<Result> SetPinAsync(string pin);
    Task<bool> VerifyPinAsync(string pin);
    Task ClearPinAsync();
    TimeSpan RemainingLockout { get; }
}

public interface IPrivacyLogger
{
    void Information(string eventName, IReadOnlyDictionary<string, object?>? properties = null);

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "Preserve the established privacy-logger contract without breaking existing implementations and callers.")]
    void Error(Exception exception, string eventName);

    Task<string> ExportSanitizedLogAsync(CancellationToken cancellationToken = default);
}
