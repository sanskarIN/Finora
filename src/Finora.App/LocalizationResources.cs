using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Resources;
using System.Text;

namespace Finora.App;

public static class LocalizationResources
{
    public const string ResourcePrefix = "Text.";

    private const string ResourceNamespacePrefix = "Finora.App.Resources.Strings.";
    private const string CompiledResourceSuffix = ".resources";
    private static readonly ResourceManager[] Managers = CreateManagers();
    private static readonly ConcurrentDictionary<
        (string UiCulture, string Key, string Template),
        CompositeFormat> FormatCache = new();

    public static string Get(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        foreach (var manager in Managers)
        {
            var value = manager.GetString(key, CultureInfo.CurrentUICulture);
            if (!string.IsNullOrEmpty(value))
                return value;
        }

        return key;
    }

    public static string Format(string key, params object?[] args)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(args);

        var uiCulture = CultureInfo.CurrentUICulture;
        var template = Get(key);
        var format = FormatCache.GetOrAdd(
            (uiCulture.Name, key, template),
            static item => CompositeFormat.Parse(item.Template));

        return string.Format(CultureInfo.CurrentCulture, format, args);
    }

    public static void Apply(ResourceDictionary target)
    {
        ArgumentNullException.ThrowIfNull(target);

        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var manager in Managers)
        {
            var neutralSet = manager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true)
                ?? throw new MissingManifestResourceException("Finora localization resources could not be loaded.");

            foreach (DictionaryEntry entry in neutralSet)
            {
                if (entry.Key is not string key || entry.Value is not string neutralValue)
                    continue;

                if (!seenKeys.Add(key))
                    throw new InvalidOperationException($"Duplicate Finora localization key '{key}'.");

                target[ResourcePrefix + key] = GetLocalizedValue(manager, key, neutralValue);
            }
        }
    }

    private static ResourceManager[] CreateManagers()
    {
        var assembly = typeof(LocalizationResources).Assembly;
        var baseNames = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ResourceNamespacePrefix, StringComparison.Ordinal))
            .Where(name => name.EndsWith($"Resources{CompiledResourceSuffix}", StringComparison.Ordinal))
            .Select(name => name[..^CompiledResourceSuffix.Length])
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        if (baseNames.Length == 0)
            throw new MissingManifestResourceException("No Finora localization resource bundles were found.");

        return baseNames.Select(baseName => new ResourceManager(baseName, assembly)).ToArray();
    }

    private static string GetLocalizedValue(ResourceManager manager, string key, string neutralValue)
    {
        var localized = manager.GetString(key, CultureInfo.CurrentUICulture);
        return string.IsNullOrEmpty(localized) ? neutralValue : localized;
    }
}
