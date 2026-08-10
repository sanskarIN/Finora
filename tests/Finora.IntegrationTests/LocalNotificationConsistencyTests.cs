using Finora.Application;
using Finora.Domain;
using Finora.Infrastructure;
using Finora.Shared;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class LocalNotificationConsistencyTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-notifications-{Guid.NewGuid():N}");
    private FinanceStoreTests.TestFactory _factory = null!;
    private FakeGateway _gateway = null!;
    private LocalNotificationService _service = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        var options = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_root, "finora.db")}")
            .Options;
        _factory = new FinanceStoreTests.TestFactory(options);
        await new DatabaseInitializer(_factory).InitializeAsync();
        _gateway = new FakeGateway();
        _service = new LocalNotificationService(_factory, _gateway);
    }

    public Task DisposeAsync()
    {
        try { Directory.Delete(_root, true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task FailedDedupeReplacement_PreservesExistingEnabledReminder()
    {
        var first = await _service.ScheduleAsync(
            LocalReminderKind.Backup,
            "Backup",
            "Create a local backup.",
            DateTimeOffset.UtcNow.AddMinutes(10),
            "backup:weekly");
        Assert.True(first.IsSuccess);
        var originalId = first.Value;

        _gateway.FailScheduling = true;
        var replacement = await _service.ScheduleAsync(
            LocalReminderKind.Backup,
            "Backup",
            "Create a local backup.",
            DateTimeOffset.UtcNow.AddMinutes(20),
            "backup:weekly");

        Assert.False(replacement.IsSuccess);
        await using var db = await _factory.CreateDbContextAsync();
        var rows = await db.NotificationSchedules.AsNoTracking().ToListAsync();
        var original = Assert.Single(rows);
        Assert.Equal(originalId, original.Id);
        Assert.True(original.IsEnabled);
        Assert.DoesNotContain(originalId, _gateway.CancelAttempts);
    }

    [Fact]
    public async Task SuccessfulDedupeReplacement_DisablesAndCancelsOriginalAfterNewSchedule()
    {
        var first = await _service.ScheduleAsync(
            LocalReminderKind.RecurringItem,
            "Reminder",
            "Open Finora to review a due item.",
            DateTimeOffset.UtcNow.AddMinutes(10),
            "recurring:test");
        Assert.True(first.IsSuccess);
        var originalId = first.Value;

        var second = await _service.ScheduleAsync(
            LocalReminderKind.RecurringItem,
            "Reminder",
            "Open Finora to review a due item.",
            DateTimeOffset.UtcNow.AddMinutes(20),
            "recurring:test");
        Assert.True(second.IsSuccess);
        var replacementId = second.Value;

        await using var db = await _factory.CreateDbContextAsync();
        var rows = await db.NotificationSchedules.AsNoTracking().OrderBy(x => x.CreatedAtUtc).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.False(rows.Single(x => x.Id == originalId).IsEnabled);
        Assert.True(rows.Single(x => x.Id == replacementId).IsEnabled);
        Assert.Contains(originalId, _gateway.CancelAttempts);
        Assert.Contains(replacementId, _gateway.ScheduledIds);
    }

    [Fact]
    public async Task Cancel_PersistsDisabledStateEvenWhenPlatformCancellationThrows()
    {
        var scheduled = await _service.ScheduleAsync(
            LocalReminderKind.Generic,
            "Reminder",
            "Open Finora.",
            DateTimeOffset.UtcNow.AddMinutes(10),
            "generic:test");
        Assert.True(scheduled.IsSuccess);
        var id = scheduled.Value;
        _gateway.ThrowOnCancel = true;

        await _service.CancelAsync(id);

        await using var db = await _factory.CreateDbContextAsync();
        Assert.False((await db.NotificationSchedules.AsNoTracking().SingleAsync(x => x.Id == id)).IsEnabled);
        Assert.Contains(id, _gateway.CancelAttempts);
    }

    [Fact]
    public async Task ReschedulePending_DisablesExpiredRowsAndRetriesDisabledCancellation()
    {
        var disabled = new NotificationSchedule
        {
            Kind = LocalReminderKind.Generic.ToString(),
            Title = "Disabled",
            Body = "Disabled reminder",
            TriggerAtUtc = DateTimeOffset.UtcNow.AddHours(1),
            IsEnabled = false
        };
        var expired = new NotificationSchedule
        {
            Kind = LocalReminderKind.Generic.ToString(),
            Title = "Expired",
            Body = "Expired reminder",
            TriggerAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            IsEnabled = true
        };
        var pending = new NotificationSchedule
        {
            Kind = LocalReminderKind.Generic.ToString(),
            Title = "Pending",
            Body = "Pending reminder",
            TriggerAtUtc = DateTimeOffset.UtcNow.AddHours(2),
            IsEnabled = true
        };
        await using (var db = await _factory.CreateDbContextAsync())
        {
            db.NotificationSchedules.AddRange(disabled, expired, pending);
            await db.SaveChangesAsync();
        }

        var count = await _service.ReschedulePendingAsync();

        Assert.Equal(1, count);
        Assert.Contains(disabled.Id, _gateway.CancelAttempts);
        Assert.Contains(expired.Id, _gateway.CancelAttempts);
        Assert.Contains(pending.Id, _gateway.ScheduledIds);
        await using var verify = await _factory.CreateDbContextAsync();
        Assert.False((await verify.NotificationSchedules.AsNoTracking().SingleAsync(x => x.Id == expired.Id)).IsEnabled);
        Assert.True((await verify.NotificationSchedules.AsNoTracking().SingleAsync(x => x.Id == pending.Id)).IsEnabled);
    }

    private sealed class FakeGateway : IPlatformNotificationGateway
    {
        public bool FailScheduling { get; set; }
        public bool ThrowOnCancel { get; set; }
        public List<Guid> ScheduledIds { get; } = [];
        public List<Guid> CancelAttempts { get; } = [];

        public Task<NotificationPermissionState> GetPermissionStateAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(NotificationPermissionState.Granted);

        public Task<NotificationPermissionState> RequestPermissionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(NotificationPermissionState.Granted);

        public Task<Result> ScheduleAsync(LocalReminder reminder, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (FailScheduling) return Task.FromResult(Result.Failure("Synthetic platform scheduling failure."));
            ScheduledIds.Add(reminder.Id);
            return Task.FromResult(Result.Success());
        }

        public Task CancelAsync(Guid reminderId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CancelAttempts.Add(reminderId);
            if (ThrowOnCancel) throw new InvalidOperationException("Synthetic platform cancellation failure.");
            return Task.CompletedTask;
        }
    }
}
