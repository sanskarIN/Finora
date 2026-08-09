namespace Finora.Shared;

public static class PinAttemptPolicy
{
    public const int AttemptsBeforeLockout = 5;
    public const int MaximumTrackedFailures = 1_000;
    public const int MaximumLockoutMinutes = 30;

    public static int NextFailureCount(int storedFailures)
        => Math.Min(Math.Clamp(storedFailures, 0, MaximumTrackedFailures - 1) + 1, MaximumTrackedFailures);

    public static TimeSpan GetLockoutDuration(int failures)
    {
        failures = Math.Clamp(failures, 0, MaximumTrackedFailures);
        if (failures < AttemptsBeforeLockout) return TimeSpan.Zero;

        var exponent = Math.Clamp(failures - AttemptsBeforeLockout, 0, 5);
        var minutes = Math.Min(MaximumLockoutMinutes, 1 << exponent);
        return TimeSpan.FromMinutes(minutes);
    }
}
