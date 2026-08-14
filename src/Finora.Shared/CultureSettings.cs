using System.Globalization;

namespace Finora.Shared;

public static class CultureSettings
{
    public static bool TryResolve(string? locale, out CultureInfo culture)
    {
        culture = CultureInfo.CurrentCulture;
        if (string.IsNullOrWhiteSpace(locale)) return false;

        var requested = locale.Trim();
        try
        {
            var resolved = CultureInfo.GetCultureInfo(requested);
            if (!string.Equals(resolved.Name, requested, StringComparison.OrdinalIgnoreCase))
                return false;

            culture = (CultureInfo)resolved.Clone();
            return true;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    public static bool TryApply(string? locale)
    {
        if (!TryResolve(locale, out var culture)) return false;

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        return true;
    }

    public static string NormalizeOrFallback(string? locale, string? fallback = null)
    {
        if (TryResolve(locale, out var culture)) return culture.Name;
        if (TryResolve(fallback, out culture)) return culture.Name;
        return CultureInfo.CurrentCulture.Name;
    }
}
