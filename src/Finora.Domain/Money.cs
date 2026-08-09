using System.Globalization;

namespace Finora.Domain;

public readonly record struct Money(long MinorUnits, string Currency)
{
    public Money
    {
        if (string.IsNullOrWhiteSpace(Currency) || Currency.Length is < 3 or > 8)
            throw new ArgumentException("Currency is required.", nameof(Currency));
        Currency = Currency.Trim().ToUpperInvariant();
    }

    public decimal ToMajorUnits(int decimalPlaces = 2) => MinorUnits / Pow10(decimalPlaces);

    public static long ToMinorUnits(decimal value, int decimalPlaces = 2)
    {
        var scaled = decimal.Round(value * Pow10(decimalPlaces), 0, MidpointRounding.AwayFromZero);
        return checked((long)scaled);
    }

    public static Money FromMajorUnits(decimal value, string currency, int decimalPlaces = 2)
        => new(ToMinorUnits(value, decimalPlaces), currency);

    public string Format(CultureInfo? culture = null, int decimalPlaces = 2)
        => $"{Currency} {ToMajorUnits(decimalPlaces).ToString($"N{decimalPlaces}", culture ?? CultureInfo.CurrentCulture)}";

    private static decimal Pow10(int exponent)
    {
        if (exponent is < 0 or > 6) throw new ArgumentOutOfRangeException(nameof(exponent));
        decimal result = 1;
        for (var i = 0; i < exponent; i++) result *= 10;
        return result;
    }
}
