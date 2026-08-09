using System.Globalization;

namespace Finora.Domain;

public readonly record struct Money(long MinorUnits, string Currency)
{
    public Money
    {
        DomainRules.ValidateCurrency(Currency);
        Currency = Currency.Trim().ToUpperInvariant();
    }

    public int DecimalPlaces => CurrencyMinorUnits.GetDecimalPlaces(Currency);

    public decimal ToMajorUnits(int? decimalPlaces = null)
        => MinorUnits / Pow10(decimalPlaces ?? DecimalPlaces);

    public static long ToMinorUnits(decimal value, int decimalPlaces)
    {
        var scaled = decimal.Round(value * Pow10(decimalPlaces), 0, MidpointRounding.AwayFromZero);
        return checked((long)scaled);
    }

    public static long ToMinorUnits(decimal value, string currency, int? decimalPlaces = null)
        => ToMinorUnits(value, decimalPlaces ?? CurrencyMinorUnits.GetDecimalPlaces(currency));

    public static Money FromMajorUnits(decimal value, string currency, int? decimalPlaces = null)
        => new(ToMinorUnits(value, currency, decimalPlaces), currency);

    public string Format(CultureInfo? culture = null, int? decimalPlaces = null)
    {
        var places = decimalPlaces ?? DecimalPlaces;
        return $"{Currency} {ToMajorUnits(places).ToString($"N{places}", culture ?? CultureInfo.CurrentCulture)}";
    }

    private static decimal Pow10(int exponent)
    {
        if (exponent is < 0 or > 6) throw new ArgumentOutOfRangeException(nameof(exponent));
        decimal result = 1;
        for (var i = 0; i < exponent; i++) result *= 10;
        return result;
    }
}
