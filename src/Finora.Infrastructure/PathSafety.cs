namespace Finora.Infrastructure;

internal static class PathSafety
{
    public static StringComparison Comparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    public static StringComparer Comparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    public static string ResolveDescendant(string root, string relativePath, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("A storage root is required.", nameof(root));
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new InvalidDataException(errorMessage);

        var canonicalRoot = Path.GetFullPath(root);
        var canonicalPath = Path.GetFullPath(Path.Combine(canonicalRoot, relativePath));
        EnsureDescendant(canonicalRoot, canonicalPath, errorMessage);
        return canonicalPath;
    }

    public static void EnsureDescendant(string root, string candidate, string errorMessage)
    {
        var canonicalRoot = Path.GetFullPath(root);
        var canonicalCandidate = Path.GetFullPath(candidate);
        var rootWithSeparator = canonicalRoot.EndsWith(Path.DirectorySeparatorChar)
            ? canonicalRoot
            : canonicalRoot + Path.DirectorySeparatorChar;

        if (!canonicalCandidate.StartsWith(rootWithSeparator, Comparison))
            throw new InvalidDataException(errorMessage);
    }
}
