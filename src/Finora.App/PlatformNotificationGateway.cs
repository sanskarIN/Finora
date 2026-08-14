using Finora.Application;
using FinoraResult = Finora.Shared.Result;

#if ANDROID
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui.ApplicationModel;
#elif IOS || MACCATALYST
using Foundation;
using UserNotifications;
#elif WINDOWS
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
#endif

namespace Finora.App;

public sealed class PlatformNotificationGateway : IPlatformNotificationGateway
{
#if ANDROID
    private const string ChannelId = "finora-reminders";
#endif

    public async Task<NotificationPermissionState> GetPermissionStateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
#if ANDROID
        if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu) return NotificationPermissionState.Granted;
        var status = await Permissions.CheckStatusAsync<PostNotificationsPermission>().ConfigureAwait(false);
        return status == PermissionStatus.Granted ? NotificationPermissionState.Granted : status == PermissionStatus.Denied ? NotificationPermissionState.Denied : NotificationPermissionState.Unknown;
#elif IOS || MACCATALYST
        var settings = await UNUserNotificationCenter.Current.GetNotificationSettingsAsync().ConfigureAwait(false);
        return settings.AuthorizationStatus switch { UNAuthorizationStatus.Authorized or UNAuthorizationStatus.Provisional or UNAuthorizationStatus.Ephemeral => NotificationPermissionState.Granted, UNAuthorizationStatus.Denied => NotificationPermissionState.Denied, _ => NotificationPermissionState.Unknown };
#elif WINDOWS
        return NotificationPermissionState.Granted;
#else
        return NotificationPermissionState.Unsupported;
#endif
    }

    public async Task<NotificationPermissionState> RequestPermissionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
#if ANDROID
        if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu) return NotificationPermissionState.Granted;
        var status = await Permissions.RequestAsync<PostNotificationsPermission>().ConfigureAwait(false);
        return status == PermissionStatus.Granted ? NotificationPermissionState.Granted : NotificationPermissionState.Denied;
#elif IOS || MACCATALYST
        var result = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound | UNAuthorizationOptions.Badge).ConfigureAwait(false);
        return result.Item1 ? NotificationPermissionState.Granted : NotificationPermissionState.Denied;
#elif WINDOWS
        return NotificationPermissionState.Granted;
#else
        return NotificationPermissionState.Unsupported;
#endif
    }

    public Task<FinoraResult> ScheduleAsync(LocalReminder reminder, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (reminder.TriggerAtUtc <= DateTimeOffset.UtcNow) return Task.FromResult(FinoraResult.Failure("Reminder time is in the past."));
#if ANDROID
        try
        {
            var context = Android.App.Application.Context; EnsureAndroidChannel(context);
            var intent = new Intent(context, typeof(FinoraReminderReceiver)); intent.SetAction($"in.sanskar.finora.REMINDER.{reminder.Id:N}"); intent.PutExtra("id", reminder.Id.ToString("N")); intent.PutExtra("title", reminder.Title); intent.PutExtra("body", reminder.Body);
            var pending = PendingIntent.GetBroadcast(context, RequestCode(reminder.Id), intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable); var alarm = (AlarmManager?)context.GetSystemService(Context.AlarmService);
            if (alarm is null || pending is null) return Task.FromResult(FinoraResult.Failure("Android alarm service is unavailable."));
            alarm.SetAndAllowWhileIdle(AlarmType.RtcWakeup, reminder.TriggerAtUtc.ToUnixTimeMilliseconds(), pending); return Task.FromResult(FinoraResult.Success());
        }
        catch (Exception ex) when (ex is Java.Lang.Exception or InvalidOperationException) { return Task.FromResult(FinoraResult.Failure("Android could not schedule the local reminder.")); }
#elif IOS || MACCATALYST
        return ScheduleAppleAsync(reminder, cancellationToken);
#elif WINDOWS
        try
        {
            var xml = new XmlDocument(); xml.LoadXml($"<toast><visual><binding template=\"ToastGeneric\"><text>{EscapeXml(reminder.Title)}</text><text>{EscapeXml(reminder.Body)}</text></binding></visual></toast>");
            var scheduled = new ScheduledToastNotification(xml, reminder.TriggerAtUtc) { Id = reminder.Id.ToString("N") }; ToastNotificationManager.CreateToastNotifier().AddToSchedule(scheduled); return Task.FromResult(FinoraResult.Success());
        }
        catch (Exception) { return Task.FromResult(FinoraResult.Failure("Windows could not schedule the local reminder. Packaged app identity may be required.")); }
#else
        return Task.FromResult(FinoraResult.Failure("Local notifications are unsupported on this platform."));
#endif
    }

    public Task CancelAsync(Guid reminderId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
#if ANDROID
        var context = Android.App.Application.Context;
        var intent = new Intent(context, typeof(FinoraReminderReceiver));
        intent.SetAction($"in.sanskar.finora.REMINDER.{reminderId:N}");
        var pending = PendingIntent.GetBroadcast(context, RequestCode(reminderId), intent, PendingIntentFlags.NoCreate | PendingIntentFlags.Immutable);
        if (pending is not null)
        {
            var alarm = (AlarmManager?)context.GetSystemService(Context.AlarmService);
            alarm?.Cancel(pending);
            pending.Cancel();
        }
#elif IOS || MACCATALYST
        UNUserNotificationCenter.Current.RemovePendingNotificationRequests([reminderId.ToString("N")]);
#elif WINDOWS
        var notifier = ToastNotificationManager.CreateToastNotifier(); foreach (var item in notifier.GetScheduledToastNotifications().Where(x => string.Equals(x.Id, reminderId.ToString("N"), StringComparison.Ordinal))) notifier.RemoveFromSchedule(item);
#endif
        return Task.CompletedTask;
    }

#if ANDROID
    private static void EnsureAndroidChannel(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return; var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService); if (manager?.GetNotificationChannel(ChannelId) is not null) return;
        var channel = new NotificationChannel(ChannelId, "Finora reminders", NotificationImportance.Default) { Description = "Local reminders for budgets, recurring items, savings goals, and backups." }; manager?.CreateNotificationChannel(channel);
    }
    private static int RequestCode(Guid id) => BitConverter.ToInt32(id.ToByteArray(), 0) & int.MaxValue;
    private sealed class PostNotificationsPermission : Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions => Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu ? [(Android.Manifest.Permission.PostNotifications, true)] : [];
    }
#elif IOS || MACCATALYST
    private static async Task<FinoraResult> ScheduleAppleAsync(LocalReminder reminder, CancellationToken cancellationToken)
    {
        try
        {
            var seconds = Math.Max(1, (reminder.TriggerAtUtc - DateTimeOffset.UtcNow).TotalSeconds); var content = new UNMutableNotificationContent { Title = reminder.Title, Body = reminder.Body, Sound = UNNotificationSound.Default }; var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(seconds, false); var request = UNNotificationRequest.FromIdentifier(reminder.Id.ToString("N"), content, trigger);
            await UNUserNotificationCenter.Current.AddNotificationRequestAsync(request).ConfigureAwait(false); cancellationToken.ThrowIfCancellationRequested(); return FinoraResult.Success();
        }
        catch (Exception) { return FinoraResult.Failure("Apple notification scheduling failed."); }
    }
#elif WINDOWS
    private static string EscapeXml(string value) => value.Replace("&", "&amp;", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal).Replace(">", "&gt;", StringComparison.Ordinal).Replace("\"", "&quot;", StringComparison.Ordinal).Replace("'", "&apos;", StringComparison.Ordinal);
#endif
}

#if ANDROID
[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class FinoraReminderReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null) return;
        var idText = intent.GetStringExtra("id") ?? Guid.NewGuid().ToString("N"); var title = intent.GetStringExtra("title") ?? "Finora reminder"; var body = intent.GetStringExtra("body") ?? "Open Finora to review your local reminder.";
        var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName ?? string.Empty); PendingIntent? contentIntent = null;
        if (launchIntent is not null) { launchIntent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop); contentIntent = PendingIntent.GetActivity(context, 0, launchIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable); }
        var builder = new Notification.Builder(context, "finora-reminders").SetContentTitle(title).SetContentText(body).SetSmallIcon(context.ApplicationInfo?.Icon ?? Android.Resource.Drawable.IcDialogInfo).SetAutoCancel(true);
        if (contentIntent is not null) builder.SetContentIntent(contentIntent); var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService); manager?.Notify(idText.GetHashCode(StringComparison.Ordinal) & int.MaxValue, builder.Build());
    }
}
#endif
