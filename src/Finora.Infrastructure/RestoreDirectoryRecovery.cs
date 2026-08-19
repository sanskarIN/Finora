namespace Finora.Infrastructure;

internal static class RestoreDirectoryRecovery
{
    public static bool TryRestore(string liveDirectory, ref string? rollbackDirectory, bool stagedDirectoryPromoted)
    {
        if (string.IsNullOrWhiteSpace(rollbackDirectory))
        {
            if (!stagedDirectoryPromoted) return true;
            return TryDeleteLiveDirectory(liveDirectory);
        }

        if (!Directory.Exists(rollbackDirectory)) return false;

        try
        {
            if (!TryDeleteLiveDirectory(liveDirectory)) return false;
            Directory.Move(rollbackDirectory, liveDirectory);
            rollbackDirectory = null;
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool TryDeleteLiveDirectory(string liveDirectory)
    {
        try
        {
            if (!Directory.Exists(liveDirectory)) return true;
            if (PathSafety.IsSymbolicLink(liveDirectory)) Directory.Delete(liveDirectory);
            else Directory.Delete(liveDirectory, recursive: true);
            return !Directory.Exists(liveDirectory);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
