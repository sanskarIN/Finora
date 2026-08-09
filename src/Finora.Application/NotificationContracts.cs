using Finora.Shared;

namespace Finora.Application;

public enum NotificationPermissionState { Unknown, Granted, Denied, Unsupported }
public enum LocalReminderKind { RecurringItem, BudgetThreshold, Backup, SavingsGoal, Generic }

public sealed record LocalReminder(Guid Id, LocalReminderKind Kind, string Title, string Body, DateTimeOffset TriggerAtUtc, string? DedupeKey, bool IsEnabled);

public interface IPlatformNotificationGateway
{
    Task<NotificationPermissionState> GetPermissionStateAsync(CancellationToken cancellationToken = default);
    Task<NotificationPermissionState> RequestPermissionAsync(CancellationToken cancellationToken = default);
    Task<Result> ScheduleAsync(LocalReminder reminder, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid reminderId, CancellationToken cancellationToken = default);
}

public interface ILocalNotificationService
{
    Task<NotificationPermissionState> GetPermissionStateAsync(CancellationToken cancellationToken = default);
    Task<NotificationPermissionState> RequestPermissionAsync(CancellationToken cancellationToken = default);
    Task<Result<Guid>> ScheduleAsync(LocalReminderKind kind, string title, string body, DateTimeOffset triggerAtUtc, string? dedupeKey = null, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid reminderId, CancellationToken cancellationToken = default);
    Task CancelByDedupeKeyAsync(string dedupeKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocalReminder>> GetScheduledAsync(CancellationToken cancellationToken = default);
    Task<int> ReschedulePendingAsync(CancellationToken cancellationToken = default);
}
