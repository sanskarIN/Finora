using Finora.Application;

namespace Finora.App;

public sealed class ReminderCoordinator(ILocalNotificationService notifications, IFinanceStore store, IAppSettingsService settings)
{
    private readonly ILocalNotificationService _notifications = notifications;
    private readonly IFinanceStore _store = store;
    private readonly IAppSettingsService _settings = settings;

    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.NotificationsEnabled) return;
        if (await _notifications.GetPermissionStateAsync(cancellationToken) != NotificationPermissionState.Granted) return;
        await _notifications.ReschedulePendingAsync(cancellationToken).ConfigureAwait(false);
        if (_settings.BackupRemindersEnabled)
        {
            var last = _settings.LastBackupAtUtc ?? DateTimeOffset.UtcNow; var next = last.AddDays(7); if (next <= DateTimeOffset.UtcNow) next = DateTimeOffset.UtcNow.AddMinutes(2);
            await _notifications.ScheduleAsync(LocalReminderKind.Backup, "Back up Finora", "Create an encrypted local backup so your finance data is recoverable if the app is removed or the device is lost.", next, "backup:weekly", cancellationToken).ConfigureAwait(false);
        }
        else await _notifications.CancelByDedupeKeyAsync("backup:weekly", cancellationToken).ConfigureAwait(false);
        var budgets = await _store.GetBudgetsAsync(DateOnly.FromDateTime(DateTime.Today), cancellationToken).ConfigureAwait(false);
        foreach (var budget in budgets)
        {
            var threshold = checked(budget.PlannedMinor * budget.WarningThresholdPercent / 100); var key = $"budget:{budget.Id}:threshold";
            if (budget.PlannedMinor > 0 && budget.ActualMinor >= threshold) await _notifications.ScheduleAsync(LocalReminderKind.BudgetThreshold, "Budget warning", $"{budget.Name} has reached its configured warning threshold.", DateTimeOffset.UtcNow.AddMinutes(1), key, cancellationToken).ConfigureAwait(false);
            else await _notifications.CancelByDedupeKeyAsync(key, cancellationToken).ConfigureAwait(false);
        }
    }
}
