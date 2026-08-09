using System.Text.Json;

namespace Finora.Infrastructure;

internal sealed record RestoreRecoveryState(
    string RestoreId,
    string StagedDirectoryName,
    string RollbackDirectoryName,
    bool HadLiveAttachmentRoot,
    DateTimeOffset CreatedAtUtc);

internal sealed class RestoreRecoveryJournal(string appDataRoot)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly string _appDataRoot = Path.GetFullPath(appDataRoot);
    private string JournalPath => Path.Combine(_appDataRoot, "finora-restore-recovery.json");

    public async Task WriteAsync(RestoreRecoveryState state, CancellationToken cancellationToken)
    {
        ValidateState(state);
        Directory.CreateDirectory(_appDataRoot);
        var temporary = JournalPath + ".tmp";
        var payload = JsonSerializer.SerializeToUtf8Bytes(state, Json);
        await File.WriteAllBytesAsync(temporary, payload, cancellationToken).ConfigureAwait(false);
        File.Move(temporary, JournalPath, overwrite: true);
    }

    public async Task<RestoreRecoveryState?> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(JournalPath)) return null;
        var bytes = await File.ReadAllBytesAsync(JournalPath, cancellationToken).ConfigureAwait(false);
        var state = JsonSerializer.Deserialize<RestoreRecoveryState>(bytes, Json)
            ?? throw new InvalidDataException("Restore recovery journal is empty.");
        ValidateState(state);
        return state;
    }

    public void Delete()
    {
        if (File.Exists(JournalPath)) File.Delete(JournalPath);
        var temporary = JournalPath + ".tmp";
        if (File.Exists(temporary)) File.Delete(temporary);
    }

    public string ResolveStagedDirectory(RestoreRecoveryState state)
        => ResolvePrivateChild(state.StagedDirectoryName, "attachments.restore.");

    public string ResolveRollbackDirectory(RestoreRecoveryState state)
        => ResolvePrivateChild(state.RollbackDirectoryName, "attachments.rollback.");

    private string ResolvePrivateChild(string directoryName, string requiredPrefix)
    {
        if (!directoryName.StartsWith(requiredPrefix, StringComparison.Ordinal) ||
            directoryName.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0)
            throw new InvalidDataException("Restore recovery directory name is invalid.");

        var path = Path.GetFullPath(Path.Combine(_appDataRoot, directoryName));
        var root = _appDataRoot.EndsWith(Path.DirectorySeparatorChar)
            ? _appDataRoot
            : _appDataRoot + Path.DirectorySeparatorChar;
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Restore recovery path escaped app-private storage.");
        return path;
    }

    private static void ValidateState(RestoreRecoveryState state)
    {
        if (!Guid.TryParseExact(state.RestoreId, "N", out _))
            throw new InvalidDataException("Restore recovery identifier is invalid.");
        if (string.IsNullOrWhiteSpace(state.StagedDirectoryName) || string.IsNullOrWhiteSpace(state.RollbackDirectoryName))
            throw new InvalidDataException("Restore recovery directories are missing.");
    }
}
