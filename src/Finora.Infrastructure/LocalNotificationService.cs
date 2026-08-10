using Finora.Application;
using Finora.Domain;
using Finora.Shared;
using Microsoft.EntityFrameworkCore;

namespace Finora.Infrastructure;

public sealed class LocalNotificationService(IDbContextFactory<FinoraDbContext> factory, IPlatformNotificationGateway gateway) : ILocalNotificationService
{
    private readonly IDbContextFactory<FinoraDbContext> _factory = factory;
    private readonly IPlatformNotificationGateway _gateway = gateway;

    public Task<NotificationPermissionState> GetPermissionStateAsync(CancellationToken cancellationToken = default)
        => _gateway.GetPermissionStateAsync(cancellationToken);

    public Task<NotificationPermissionState> RequestPermissionAsync(CancellationToken cancellationToken = default)
        => _gateway.RequestPermissionAsync(cancellationToken);

    public async Task<Result<Guid>> ScheduleAsync(
        LocalReminderKind kind,
        string title,
        string body,
        DateTimeOffset triggerAtUtc,
        string? dedupeKey = null,
        CancellationToken cancellationToken = default)
    {
        title = Normalize(title, 160);
        body = Normalize(body, 500);
        dedupeKey = string.IsNullOrWhiteSpace(dedupeKey) ? null : Normalize(dedupeKey, 200);

        if (title.Length == 0 || body.Length == 0)
            return Result<Guid>.Failure("Notification title and body are required.");
        if (triggerAtUtc <= DateTimeOffset.UtcNow.AddSeconds(2))
            return Result<Guid>.Failure("Notification time must be in the future.");
        if (await _gateway.GetPermissionStateAsync(cancellationToken).ConfigureAwait(false) != NotificationPermissionState.Granted)
            return Result<Guid>.Failure("Notification permission is not granted.");

        var entity = new NotificationSchedule
        {
            Kind = kind.ToString(),
            Title = title,
            Body = body,
            TriggerAtUtc = triggerAtUtc.ToUniversalTime(),
            DedupeKey = dedupeKey,
            IsEnabled = true
        };

        var scheduled = await _gateway.ScheduleAsync(ToReminder(entity), cancellationToken).ConfigureAwait(false);
        if (!scheduled.IsSuccess)
            return Result<Guid>.Failure(scheduled.Error ?? "The operating system did not accept the reminder.");

        List<Guid> staleIds = [];
        try
        {
            await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            if (dedupeKey is not null)
            {
                var existing = await db.NotificationSchedules
                    .Where(x => x.DedupeKey == dedupeKey && x.IsEnabled)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
                foreach (var schedule in existing)
                {
                    schedule.IsEnabled = false;
                    schedule.UpdatedAtUtc = DateTimeOffset.UtcNow;
                    staleIds.Add(schedule.Id);
                }
            }

            db.NotificationSchedules.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await BestEffortCancelAsync(entity.Id).ConfigureAwait(false);
            throw;
        }

        foreach (var staleId in staleIds)
            await BestEffortCancelAsync(staleId).ConfigureAwait(false);

        return Result<Guid>.Success(entity.Id);
    }

    public async Task CancelAsync(Guid reminderId, CancellationToken cancellationToken = default)
    {
        await using (var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false))
        {
            var entity = await db.NotificationSchedules
                .SingleOrDefaultAsync(x => x.Id == reminderId, cancellationToken)
                .ConfigureAwait(false);
            if (entity is not null && entity.IsEnabled)
            {
                entity.IsEnabled = false;
                entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        await BestEffortCancelAsync(reminderId).ConfigureAwait(false);
    }

    public async Task CancelByDedupeKeyAsync(string dedupeKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dedupeKey)) return;
        dedupeKey = Normalize(dedupeKey, 200);

        List<Guid> ids;
        await using (var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false))
        {
            var rows = await db.NotificationSchedules
                .Where(x => x.DedupeKey == dedupeKey && x.IsEnabled)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            ids = rows.Select(x => x.Id).ToList();
            foreach (var row in rows)
            {
                row.IsEnabled = false;
                row.UpdatedAtUtc = DateTimeOffset.UtcNow;
            }
            if (rows.Count > 0)
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (var id in ids)
            await BestEffortCancelAsync(id).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<LocalReminder>> GetScheduledAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;
        var rows = await db.NotificationSchedules.AsNoTracking()
            .Where(x => x.IsEnabled && x.TriggerAtUtc > now)
            .OrderBy(x => x.TriggerAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.Select(ToReminder).ToList();
    }

    public async Task<int> ReschedulePendingAsync(CancellationToken cancellationToken = default)
    {
        if (await _gateway.GetPermissionStateAsync(cancellationToken).ConfigureAwait(false) != NotificationPermissionState.Granted)
            return 0;

        List<Guid> disabledIds;
        List<LocalReminder> pending;
        await using (var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false))
        {
            var now = DateTimeOffset.UtcNow;
            var rows = await db.NotificationSchedules.ToListAsync(cancellationToken).ConfigureAwait(false);
            disabledIds = rows.Where(x => !x.IsEnabled).Select(x => x.Id).ToList();

            foreach (var expired in rows.Where(x => x.IsEnabled && x.TriggerAtUtc <= now))
            {
                expired.IsEnabled = false;
                expired.UpdatedAtUtc = now;
                disabledIds.Add(expired.Id);
            }

            if (db.ChangeTracker.HasChanges())
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            pending = rows
                .Where(x => x.IsEnabled && x.TriggerAtUtc > now)
                .OrderBy(x => x.TriggerAtUtc)
                .Select(ToReminder)
                .ToList();
        }

        foreach (var disabledId in disabledIds.Distinct())
            await BestEffortCancelAsync(disabledId).ConfigureAwait(false);

        var scheduled = 0;
        foreach (var reminder in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if ((await _gateway.ScheduleAsync(reminder, cancellationToken).ConfigureAwait(false)).IsSuccess)
                scheduled++;
        }
        return scheduled;
    }

    private async Task BestEffortCancelAsync(Guid reminderId)
    {
        try
        {
            await _gateway.CancelAsync(reminderId, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            // The database remains the source of truth. ReschedulePendingAsync retries cancellation
            // of disabled IDs during the next reminder reconciliation.
        }
    }

    private static LocalReminder ToReminder(NotificationSchedule x)
        => new(
            x.Id,
            Enum.TryParse<LocalReminderKind>(x.Kind, out var kind) ? kind : LocalReminderKind.Generic,
            x.Title,
            x.Body,
            x.TriggerAtUtc,
            x.DedupeKey,
            x.IsEnabled);

    private static string Normalize(string value, int maxLength)
    {
        var text = value?.Trim() ?? string.Empty;
        return text.Length <= maxLength ? text : text[..maxLength];
    }
}
