namespace Finora.Infrastructure;

internal static class RestoreDirectoryRecovery
{
    public static bool TryRestore(string liveDirectory, ref string? rollbackDirectory)
    {
        if (string.IsNullOrWhiteSpace(rollbackDirectory) || !Directory.Exists(rollbackDirectory))
        {
            rollbackDirectory = null;
            return true;
        }

        try
        {
            if (Directory.Exists(liveDirectory))
            {
                if (PathSafety.IsSymbolicLink(liveDirectory)) Directory.Delete(liveDirectory);
                else Directory.Delete(liveDirectory, recursive: true);
            }

            if (Directory.Exists(liveDirectory)) return false;
            Directory.Move(rollbackDirectory, liveDirectory);
            rollbackDirectory = null;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
