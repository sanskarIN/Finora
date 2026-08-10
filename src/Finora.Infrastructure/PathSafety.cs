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

    public static string ResolveDescendantWithoutLinks(string root, string relativePath, string errorMessage)
    {
        var path = ResolveDescendant(root, relativePath, errorMessage);
        EnsureNoLinkTraversal(root, path, errorMessage);
        return path;
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

    public static void EnsureNoLinkTraversal(string root, string candidate, string errorMessage)
    {
        var canonicalRoot = Path.GetFullPath(root);
        var canonicalCandidate = Path.GetFullPath(candidate);
        EnsureDescendant(canonicalRoot, canonicalCandidate, errorMessage);

        EnsureNotLinkIfExists(canonicalRoot, errorMessage);
        var relative = Path.GetRelativePath(canonicalRoot, canonicalCandidate);
        var segments = relative.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);

        var current = canonicalRoot;
        foreach (var segment in segments)
        {
            if (segment is "." or "..")
                throw new InvalidDataException(errorMessage);
            current = Path.Combine(current, segment);
            EnsureNotLinkIfExists(current, errorMessage);
        }
    }

    public static void EnsureNotLinkIfExists(string path, string errorMessage)
    {
        if (!File.Exists(path) && !Directory.Exists(path)) return;
        if (IsSymbolicLink(path))
            throw new InvalidDataException(errorMessage);
    }

    public static bool IsSymbolicLink(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path)) return false;
        var attributes = File.GetAttributes(path);
        if ((attributes & FileAttributes.ReparsePoint) != 0) return true;

        FileSystemInfo info = (attributes & FileAttributes.Directory) != 0
            ? new DirectoryInfo(path)
            : new FileInfo(path);
        return info.LinkTarget is not null;
    }

    public static IEnumerable<string> EnumerateFilesWithoutLinks(string root, string errorMessage)
    {
        var canonicalRoot = Path.GetFullPath(root);
        if (!Directory.Exists(canonicalRoot)) yield break;
        EnsureNotLinkIfExists(canonicalRoot, errorMessage);

        var pending = new Stack<string>();
        pending.Push(canonicalRoot);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            foreach (var entry in Directory.EnumerateFileSystemEntries(current, "*", SearchOption.TopDirectoryOnly))
            {
                var full = Path.GetFullPath(entry);
                EnsureDescendant(canonicalRoot, full, errorMessage);
                EnsureNotLinkIfExists(full, errorMessage);
                var attributes = File.GetAttributes(full);
                if ((attributes & FileAttributes.Directory) != 0) pending.Push(full);
                else yield return full;
            }
        }
    }
}
