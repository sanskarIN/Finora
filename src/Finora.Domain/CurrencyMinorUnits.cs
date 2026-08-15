namespace Finora.Domain;

public static class CurrencyMinorUnits
{
    private static readonly HashSet<string> ZeroDecimalCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "BIF", "CLP", "DJF", "GNF", "ISK", "JPY", "KMF", "KRW", "PYG", "RWF", "UGX", "VND", "VUV", "XAF", "XOF", "XPF"
    };

    private static readonly HashSet<string> ThreeDecimalCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "BHD", "IQD", "JOD", "KWD", "LYD", "OMR", "TND"
    };

    private static readonly HashSet<string> FourDecimalCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "CLF"
    };

    public static int GetDecimalPlaces(string currency)
    {
        DomainRules.ValidateCurrency(currency);
        var normalized = currency.Trim().ToUpperInvariant();
        if (ZeroDecimalCurrencies.Contains(normalized)) return 0;
        if (ThreeDecimalCurrencies.Contains(normalized)) return 3;
        if (FourDecimalCurrencies.Contains(normalized)) return 4;
        return 2;
    }
}
