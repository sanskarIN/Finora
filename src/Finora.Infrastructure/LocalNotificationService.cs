using Finora.Application;
using Finora.Domain;
using Finora.Shared;
using Microsoft.EntityFrameworkCore;

namespace Finora.Infrastructure;

public sealed class LocalNotificationService(IDbContextFactory<FinoraDbContext> factory, IPlatformNotificationGateway gateway) : ILocalNotificationService
{
    private readonly IDbContextFactory<FinoraDbContext> _factory = factory;
    private readonly IPlatformNotificationGateway _gateway = gateway;

    public Task<NotificationPermissionState> GetPermissionStateAsync(CancellationToken cancellationToken = default) => _gateway.GetPermissionStateAsync(cancellationToken);
    public Task<NotificationPermissionState> RequestPermissionAsync(CancellationToken cancellationToken = default) => _gateway.RequestPermissionAsync(cancellationToken);

    public async Task<Result<Guid>> ScheduleAsync(LocalReminderKind kind, string title, string body, DateTimeOffset triggerAtUtc, string? dedupeKey = null, CancellationToken cancellationToken = default)
    {
        title = Normalize(title, 160); body = Normalize(body, 500); dedupeKey = string.IsNullOrWhiteSpace(dedupeKey) ? null : Normalize(dedupeKey, 200);
        if (title.Length == 0 || body.Length == 0) return Result<Guid>.Failure("Notification title and body are required.");
        if (triggerAtUtc <= DateTimeOffset.UtcNow.AddSeconds(2)) return Result<Guid>.Failure("Notification time must be in the future.");
        if (await _gateway.GetPermissionStateAsync(cancellationToken).ConfigureAwait(false) != NotificationPermissionState.Granted) return Result<Guid>.Failure("Notification permission is not granted.");
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        if (dedupeKey is not null)
        {
            var existing = await db.NotificationSchedules.Where(x => x.DedupeKey == dedupeKey && x.IsEnabled).ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var schedule in existing) { await _gateway.CancelAsync(schedule.Id, cancellationToken).ConfigureAwait(false); schedule.IsEnabled = false; schedule.UpdatedAtUtc = DateTimeOffset.UtcNow; }
        }
        var entity = new NotificationSchedule { Kind = kind.ToString(), Title = title, Body = body, TriggerAtUtc = triggerAtUtc.ToUniversalTime(), DedupeKey = dedupeKey, IsEnabled = true };
        var scheduled = await _gateway.ScheduleAsync(ToReminder(entity), cancellationToken).ConfigureAwait(false);
        if (!scheduled.IsSuccess) return Result<Guid>.Failure(scheduled.Error ?? "The operating system did not accept the reminder.");
        db.NotificationSchedules.Add(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return Result<Guid>.Success(entity.Id);
    }

    public async Task CancelAsync(Guid reminderId, CancellationToken cancellationToken = default)
    {
        await _gateway.CancelAsync(reminderId, cancellationToken).ConfigureAwait(false);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var entity = await db.NotificationSchedules.SingleOrDefaultAsync(x => x.Id == reminderId, cancellationToken).ConfigureAwait(false);
        if (entity is null) return;
        entity.IsEnabled = false; entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task CancelByDedupeKeyAsync(string dedupeKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dedupeKey)) return;
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rows = await db.NotificationSchedules.Where(x => x.DedupeKey == dedupeKey && x.IsEnabled).ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var row in rows) { await _gateway.CancelAsync(row.Id, cancellationToken).ConfigureAwait(false); row.IsEnabled = false; row.UpdatedAtUtc = DateTimeOffset.UtcNow; }
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<LocalReminder>> GetScheduledAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;
        var rows = await db.NotificationSchedules.AsNoTracking().Where(x => x.IsEnabled && x.TriggerAtUtc > now).OrderBy(x => x.TriggerAtUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(ToReminder).ToList();
    }

    public async Task<int> ReschedulePendingAsync(CancellationToken cancellationToken = default)
    {
        if (await _gateway.GetPermissionStateAsync(cancellationToken).ConfigureAwait(false) != NotificationPermissionState.Granted) return 0;
        var reminders = await GetScheduledAsync(cancellationToken).ConfigureAwait(false); var scheduled = 0;
        foreach (var reminder in reminders) { cancellationToken.ThrowIfCancellationRequested(); if ((await _gateway.ScheduleAsync(reminder, cancellationToken).ConfigureAwait(false)).IsSuccess) scheduled++; }
        return scheduled;
    }

    private static LocalReminder ToReminder(NotificationSchedule x) => new(x.Id, Enum.TryParse<LocalReminderKind>(x.Kind, out var kind) ? kind : LocalReminderKind.Generic, x.Title, x.Body, x.TriggerAtUtc, x.DedupeKey, x.IsEnabled);
    private static string Normalize(string value, int maxLength) { var text = value?.Trim() ?? string.Empty; return text.Length <= maxLength ? text : text[..maxLength]; }
}
