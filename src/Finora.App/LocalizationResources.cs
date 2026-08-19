using System.Collections;
using System.Globalization;
using System.Resources;

namespace Finora.App;

public static class LocalizationResources
{
    public const string ResourcePrefix = "Text.";

    private static readonly ResourceManager Manager = new(
        "Finora.App.Resources.Strings.AppResources",
        typeof(LocalizationResources).Assembly);

    public static string Get(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var localized = Manager.GetString(key, CultureInfo.CurrentUICulture);
        if (!string.IsNullOrEmpty(localized))
            return localized;

        var neutral = Manager.GetString(key, CultureInfo.InvariantCulture);
        return string.IsNullOrEmpty(neutral) ? key : neutral;
    }

    public static void Apply(ResourceDictionary target)
    {
        ArgumentNullException.ThrowIfNull(target);

        var neutralSet = Manager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true)
            ?? throw new MissingManifestResourceException("Finora localization resources could not be loaded.");

        foreach (DictionaryEntry entry in neutralSet)
        {
            if (entry.Key is not string key || entry.Value is not string)
                continue;

            target[ResourcePrefix + key] = Get(key);
        }
    }
}
