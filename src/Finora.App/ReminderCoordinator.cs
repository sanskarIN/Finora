using Finora.Application;
using Finora.Domain;

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
        await SyncBackupReminderAsync(cancellationToken).ConfigureAwait(false);
        await SyncBudgetRemindersAsync(cancellationToken).ConfigureAwait(false);
        await SyncRecurringRemindersAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task SyncBackupReminderAsync(CancellationToken cancellationToken)
    {
        if (_settings.BackupRemindersEnabled)
        {
            var last = _settings.LastBackupAtUtc ?? DateTimeOffset.UtcNow;
            var next = last.AddDays(7);
            if (next <= DateTimeOffset.UtcNow) next = DateTimeOffset.UtcNow.AddMinutes(2);
            await _notifications.ScheduleAsync(
                LocalReminderKind.Backup,
                "Back up Finora",
                "Create an encrypted local backup so your finance data is recoverable if the app is removed or the device is lost.",
                next,
                "backup:weekly",
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _notifications.CancelByDedupeKeyAsync("backup:weekly", cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SyncBudgetRemindersAsync(CancellationToken cancellationToken)
    {
        var budgets = await _store.GetBudgetsAsync(DateOnly.FromDateTime(DateTime.Today), cancellationToken).ConfigureAwait(false);
        var activeKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var budget in budgets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = $"budget:{budget.Id}:threshold";
            activeKeys.Add(key);
            var threshold = PercentOf(budget.PlannedMinor, budget.WarningThresholdPercent);
            if (budget.PlannedMinor > 0 && budget.ActualMinor >= threshold)
            {
                await _notifications.ScheduleAsync(
                    LocalReminderKind.BudgetThreshold,
                    "Budget warning",
                    "A Finora budget has reached its configured warning threshold.",
                    DateTimeOffset.UtcNow.AddMinutes(1),
                    key,
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _notifications.CancelByDedupeKeyAsync(key, cancellationToken).ConfigureAwait(false);
            }
        }

        await CancelStaleByPrefixAsync("budget:", activeKeys, cancellationToken).ConfigureAwait(false);
    }

    private async Task SyncRecurringRemindersAsync(CancellationToken cancellationToken)
    {
        var rules = await _store.GetRecurrenceRulesAsync(cancellationToken).ConfigureAwait(false);
        var activeKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var rule in rules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = $"recurrence:{rule.Id}";
            if (rule.Status != RecurrenceStatus.Active || rule.NextDueOn is not DateOnly dueOn)
            {
                await _notifications.CancelByDedupeKeyAsync(key, cancellationToken).ConfigureAwait(false);
                continue;
            }

            activeKeys.Add(key);
            var localDue = dueOn.ToDateTime(new TimeOnly(9, 0));
            var localTrigger = localDue.AddMinutes(-Math.Clamp(rule.ReminderMinutesBefore, 0, 10_080));
            var trigger = new DateTimeOffset(DateTime.SpecifyKind(localTrigger, DateTimeKind.Local)).ToUniversalTime();
            if (trigger <= DateTimeOffset.UtcNow.AddSeconds(2))
            {
                await _notifications.CancelByDedupeKeyAsync(key, cancellationToken).ConfigureAwait(false);
                continue;
            }

            await _notifications.ScheduleAsync(
                LocalReminderKind.RecurringItem,
                "Finora recurring reminder",
                "A recurring Finora item is approaching.",
                trigger,
                key,
                cancellationToken).ConfigureAwait(false);
        }

        await CancelStaleByPrefixAsync("recurrence:", activeKeys, cancellationToken).ConfigureAwait(false);
    }

    private async Task CancelStaleByPrefixAsync(string prefix, IReadOnlySet<string> activeKeys, CancellationToken cancellationToken)
    {
        var scheduled = await _notifications.GetScheduledAsync(cancellationToken).ConfigureAwait(false);
        foreach (var reminder in scheduled)
        {
            if (reminder.DedupeKey is not string key || !key.StartsWith(prefix, StringComparison.Ordinal) || activeKeys.Contains(key)) continue;
            await _notifications.CancelAsync(reminder.Id, cancellationToken).ConfigureAwait(false);
        }
    }

    private static long PercentOf(long amountMinor, int percent)
    {
        if (amountMinor < 0) throw new InvalidDataException("Budget planned amount cannot be negative.");
        percent = Math.Clamp(percent, 0, 100);
        var whole = amountMinor / 100;
        var remainder = amountMinor % 100;
        return checked(checked(whole * percent) + checked(remainder * percent) / 100);
    }
}
