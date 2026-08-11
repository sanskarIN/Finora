namespace Finora.Shared;

public readonly record struct UtcDateRange(DateTimeOffset FromUtc, DateTimeOffset ToExclusiveUtc)
{
    public TimeSpan Duration => ToExclusiveUtc - FromUtc;
}

public static class LocalDateRange
{
    public static UtcDateRange ToUtc(DateOnly from, DateOnly through, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        if (from == default || through == default) throw new ArgumentException("Date range requires valid dates.");
        if (through < from) throw new ArgumentException("Date range end cannot precede its start.");

        var fromUtc = ConvertBoundaryToUtc(from, timeZone);
        var toExclusiveUtc = ConvertBoundaryToUtc(through.AddDays(1), timeZone);
        if (toExclusiveUtc <= fromUtc) throw new InvalidOperationException("Resolved UTC date range is not positive.");
        return new UtcDateRange(fromUtc, toExclusiveUtc);
    }

    private static DateTimeOffset ConvertBoundaryToUtc(DateOnly date, TimeZoneInfo timeZone)
    {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var attempts = 0;
        while (timeZone.IsInvalidTime(local) && attempts < 180)
        {
            local = local.AddMinutes(1);
            attempts++;
        }
        if (timeZone.IsInvalidTime(local))
            throw new InvalidOperationException("The local date boundary could not be resolved in the selected time zone.");

        if (timeZone.IsAmbiguousTime(local))
        {
            var offsets = timeZone.GetAmbiguousTimeOffsets(local);
            var earliestUtcOffset = offsets.Max();
            return new DateTimeOffset(local, earliestUtcOffset).ToUniversalTime();
        }

        return new DateTimeOffset(local, timeZone.GetUtcOffset(local)).ToUniversalTime();
    }
}
