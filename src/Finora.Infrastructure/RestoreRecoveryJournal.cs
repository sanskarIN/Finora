using System.Text.Json;

namespace Finora.Infrastructure;

internal sealed record RestoreRecoveryState(
    string RestoreId,
    string StagedDirectoryName,
    string RollbackDirectoryName,
    bool HadLiveAttachmentRoot,
    bool RollbackCopyReady,
    bool MarkerMeansPending,
    DateTimeOffset CreatedAtUtc);

internal sealed class RestoreRecoveryJournal(string appDataRoot)
{
    private const int MaximumJournalBytes = 64 * 1024;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly string _appDataRoot = Path.GetFullPath(appDataRoot);
    private string JournalPath => PathSafety.ResolveDescendantWithoutLinks(_appDataRoot, "finora-restore-recovery.json", "Restore recovery journal path is invalid.");

    public async Task WriteAsync(RestoreRecoveryState state, CancellationToken cancellationToken)
    {
        ValidateState(state);
        Directory.CreateDirectory(_appDataRoot);
        PathSafety.EnsureNotLinkIfExists(_appDataRoot, "App-private recovery root cannot be a symbolic link or reparse point.");
        var journal = JournalPath;
        var temporary = PathSafety.ResolveDescendantWithoutLinks(_appDataRoot, "finora-restore-recovery.json.tmp", "Restore recovery temporary journal path is invalid.");
        PathSafety.EnsureNotLinkIfExists(journal, "Restore recovery journal cannot be a symbolic link or reparse point.");
        PathSafety.EnsureNotLinkIfExists(temporary, "Restore recovery temporary journal cannot be a symbolic link or reparse point.");

        var payload = JsonSerializer.SerializeToUtf8Bytes(state, Json);
        if (payload.Length > MaximumJournalBytes)
            throw new InvalidDataException("Restore recovery journal is unexpectedly large.");
        await File.WriteAllBytesAsync(temporary, payload, cancellationToken).ConfigureAwait(false);
        File.Move(temporary, journal, overwrite: true);
    }

    public async Task<RestoreRecoveryState?> ReadAsync(CancellationToken cancellationToken)
    {
        var journal = JournalPath;
        if (!File.Exists(journal)) return null;
        PathSafety.EnsureNotLinkIfExists(journal, "Restore recovery journal cannot be a symbolic link or reparse point.");
        var info = new FileInfo(journal);
        if (info.Length <= 0 || info.Length > MaximumJournalBytes)
            throw new InvalidDataException("Restore recovery journal has an invalid size.");
        var bytes = await File.ReadAllBytesAsync(journal, cancellationToken).ConfigureAwait(false);
        var state = JsonSerializer.Deserialize<RestoreRecoveryState>(bytes, Json)
            ?? throw new InvalidDataException("Restore recovery journal is empty.");
        ValidateState(state);
        return state;
    }

    public void Delete()
    {
        var journal = JournalPath;
        var temporary = PathSafety.ResolveDescendantWithoutLinks(_appDataRoot, "finora-restore-recovery.json.tmp", "Restore recovery temporary journal path is invalid.");
        PathSafety.EnsureNotLinkIfExists(journal, "Restore recovery journal cannot be a symbolic link or reparse point.");
        PathSafety.EnsureNotLinkIfExists(temporary, "Restore recovery temporary journal cannot be a symbolic link or reparse point.");
        if (File.Exists(journal)) File.Delete(journal);
        if (File.Exists(temporary)) File.Delete(temporary);
    }

    public string ResolveStagedDirectory(RestoreRecoveryState state)
        => ResolvePrivateChild(state.StagedDirectoryName, "attachments.restore.");

    public string ResolveRollbackDirectory(RestoreRecoveryState state)
        => ResolvePrivateChild(state.RollbackDirectoryName, "attachments.rollback.");

    private string ResolvePrivateChild(string directoryName, string requiredPrefix)
    {
        if (!directoryName.StartsWith(requiredPrefix, StringComparison.Ordinal) ||
            directoryName.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0 ||
            directoryName.Contains("..", StringComparison.Ordinal))
            throw new InvalidDataException("Restore recovery directory name is invalid.");

        return PathSafety.ResolveDescendantWithoutLinks(_appDataRoot, directoryName, "Restore recovery path escaped app-private storage or traversed a link.");
    }

    private static void ValidateState(RestoreRecoveryState state)
    {
        if (!Guid.TryParseExact(state.RestoreId, "N", out _))
            throw new InvalidDataException("Restore recovery identifier is invalid.");
        if (string.IsNullOrWhiteSpace(state.StagedDirectoryName) || string.IsNullOrWhiteSpace(state.RollbackDirectoryName))
            throw new InvalidDataException("Restore recovery directories are missing.");
        if (state.CreatedAtUtc == default)
            throw new InvalidDataException("Restore recovery timestamp is missing.");
    }
}
