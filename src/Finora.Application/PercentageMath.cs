namespace Finora.Application;

public static class PercentageMath
{
    public static long CeilingPercentOf(long amount, int percent)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (percent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(percent), "Percentage must be between 0 and 100.");
        if (amount == 0 || percent == 0) return 0;

        var wholeHundreds = amount / 100;
        var remainder = amount % 100;
        var whole = checked(wholeHundreds * percent);
        var fractionalNumerator = checked(remainder * percent);
        var fractional = checked((fractionalNumerator + 99) / 100);
        return checked(whole + fractional);
    }
}
