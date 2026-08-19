using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Finora.Application;
using Finora.Domain;
using Finora.Shared;
using Microsoft.EntityFrameworkCore;

namespace Finora.Infrastructure;

public sealed class BackupService(IDbContextFactory<FinoraDbContext> factory, string appDataRoot) : IBackupService
{
    private const int MaximumEncryptedBytes = 512 * 1024 * 1024;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly IDbContextFactory<FinoraDbContext> _factory = factory;
    private readonly string _appDataRoot = Path.GetFullPath(appDataRoot);
    private string AttachmentRoot => Path.GetFullPath(Path.Combine(_appDataRoot, "attachments"));

    public async Task<byte[]> CreateEncryptedBackupAsync(string password, CancellationToken cancellationToken = default)
    {
        ValidatePassword(password);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var attachments = await db.Attachments.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var blobs = new List<AttachmentBlob>(attachments.Count);
        try
        {
            foreach (var attachment in attachments)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var path = ResolveAttachmentPath(attachment.RelativePath);
                if (!File.Exists(path)) throw new InvalidDataException($"Attachment '{attachment.OriginalFileName}' is missing; backup was not created.");
                var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
                try
                {
                    if (attachment.SizeBytes != bytes.LongLength)
                        throw new InvalidDataException($"Attachment '{attachment.OriginalFileName}' size does not match the database record.");
                    if (attachment.Sha256 is not { Length: 32 })
                        throw new InvalidDataException($"Attachment '{attachment.OriginalFileName}' checksum metadata is missing or invalid.");
                    var hash = SHA256.HashData(bytes);
                    if (!CryptographicOperations.FixedTimeEquals(hash, attachment.Sha256))
                        throw new InvalidDataException($"Attachment '{attachment.OriginalFileName}' failed integrity verification.");
                    blobs.Add(new AttachmentBlob(attachment.Id, bytes));
                    bytes = [];
                }
                finally
                {
                    if (bytes.Length > 0) CryptographicOperations.ZeroMemory(bytes);
                }
            }

            var snapshot = new Snapshot(
                AppConstants.DatabaseSchemaVersion,
                DateTimeOffset.UtcNow,
                await db.Accounts.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false),
                await db.Transactions.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false),
                await db.TransactionSplits.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false),
                await db.Categories.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false),
                await db.Tags.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false),
                await db.TransactionTags.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false),
                await db.Budgets.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false),
                await db.BudgetPeriods.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false),
                await db.SavingsGoals.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false),
                await db.GoalContributions.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false),
                await db.RecurrenceRules.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false),
                await db.RecurrenceOccurrences.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false),
                attachments,
                blobs,
                await db.TransactionRevisions.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false),
                await db.AccountReconciliations.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false),
                await db.NotificationSchedules.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false),
                await db.AppSettings.AsNoTracking().Where(x => x.Key != "schema.version" && !x.Key.StartsWith("internal.")).ToListAsync(cancellationToken).ConfigureAwait(false));

            byte[]? plaintext = null;
            byte[] encrypted;
            try
            {
                ValidateUniqueIds(snapshot);
                ValidateSnapshot(snapshot);
                plaintext = JsonSerializer.SerializeToUtf8Bytes(snapshot, Json);
                encrypted = Encrypt(plaintext, password);
            }
            finally
            {
                if (plaintext is not null) CryptographicOperations.ZeroMemory(plaintext);
            }

            var metadata = new BackupMetadata
            {
                BackupId = Guid.NewGuid().ToString("N"),
                SchemaVersion = AppConstants.DatabaseSchemaVersion,
                CreatedOnUtc = snapshot.CreatedAtUtc,
                Sha256Hex = Convert.ToHexString(SHA256.HashData(encrypted))
            };
            db.BackupMetadata.Add(metadata);
            db.AuditEntries.Add(new AuditEntry { EntityType = "Backup", EntityId = metadata.Id, Action = "CreatedEncryptedBackup" });
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return encrypted;
        }
        finally
        {
            ZeroAttachmentBlobs(blobs);
        }
    }

    public async Task<Result<BackupPreview>> PreviewEncryptedBackupAsync(Stream backupStream, string password, CancellationToken cancellationToken = default)
    {
        Snapshot? snapshot = null;
        try
        {
            snapshot = await ReadAndValidateAsync(backupStream, password, cancellationToken).ConfigureAwait(false);
            if (snapshot.SchemaVersion > AppConstants.DatabaseSchemaVersion)
                return Result<BackupPreview>.Failure("Backup was created by a newer Finora schema.");
            return Result<BackupPreview>.Success(new BackupPreview(snapshot.SchemaVersion, snapshot.CreatedAtUtc, snapshot.Accounts.Count, snapshot.Transactions.Count, snapshot.Budgets.Count, snapshot.Goals.Count));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is CryptographicException or JsonException or InvalidDataException or ArgumentException or IOException or InvalidOperationException or OverflowException)
        {
            return Result<BackupPreview>.Failure("The backup could not be verified. Check the file and password.");
        }
        finally
        {
            if (snapshot is not null) ZeroAttachmentBlobs(snapshot.AttachmentBlobs);
        }
    }

    public async Task<Result> RestoreEncryptedBackupAsync(Stream backupStream, string password, CancellationToken cancellationToken = default)
    {
        string? stagedDirectory = null;
        string? rollbackDirectory = null;
        var attachmentsPromoted = false;
        Snapshot? snapshot = null;
        try
        {
            snapshot = await ReadAndValidateAsync(backupStream, password, cancellationToken).ConfigureAwait(false);
            if (snapshot.SchemaVersion != AppConstants.DatabaseSchemaVersion)
                return Result.Failure("This build restores schema-v2 backups. Migrate older backups with the matching Finora version first.");

            PathSafety.EnsureNotLinkIfExists(AttachmentRoot, "Finora receipt storage cannot be a symbolic link or reparse point during restore.");
            stagedDirectory = PathSafety.ResolveDescendantWithoutLinks(_appDataRoot, $"attachments.restore.{Guid.NewGuid():N}", "Restore staging path is invalid.");
            Directory.CreateDirectory(stagedDirectory);
            foreach (var attachment in snapshot.Attachments)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var blob = snapshot.AttachmentBlobs.Single(x => x.AttachmentId == attachment.Id);
                var destination = ResolveStagedPath(stagedDirectory, attachment.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                await File.WriteAllBytesAsync(destination, blob.Data, cancellationToken).ConfigureAwait(false);
            }

            await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            db.TransactionRevisions.RemoveRange(db.TransactionRevisions);
            db.AccountReconciliations.RemoveRange(db.AccountReconciliations);
            db.NotificationSchedules.RemoveRange(db.NotificationSchedules);
            db.TransactionTags.RemoveRange(db.TransactionTags);
            db.TransactionSplits.RemoveRange(db.TransactionSplits);
            db.Attachments.RemoveRange(db.Attachments);
            db.RecurrenceOccurrences.RemoveRange(db.RecurrenceOccurrences);
            db.GoalContributions.RemoveRange(db.GoalContributions);
            db.BudgetPeriods.RemoveRange(db.BudgetPeriods);
            db.Transactions.RemoveRange(db.Transactions);
            db.RecurrenceRules.RemoveRange(db.RecurrenceRules);
            db.Budgets.RemoveRange(db.Budgets);
            db.SavingsGoals.RemoveRange(db.SavingsGoals);
            db.Tags.RemoveRange(db.Tags);
            db.Categories.RemoveRange(db.Categories);
            db.Accounts.RemoveRange(db.Accounts);
            db.AppSettings.RemoveRange(db.AppSettings.Where(x => x.Key != "schema.version"));
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            db.Accounts.AddRange(snapshot.Accounts);
            db.Categories.AddRange(snapshot.Categories);
            db.Tags.AddRange(snapshot.Tags);
            db.Budgets.AddRange(snapshot.Budgets);
            db.BudgetPeriods.AddRange(snapshot.BudgetPeriods);
            db.SavingsGoals.AddRange(snapshot.Goals);
            db.GoalContributions.AddRange(snapshot.Contributions);
            db.RecurrenceRules.AddRange(snapshot.Rules);
            db.Transactions.AddRange(snapshot.Transactions);
            db.TransactionSplits.AddRange(snapshot.Splits);
            db.TransactionTags.AddRange(snapshot.TransactionTags);
            db.RecurrenceOccurrences.AddRange(snapshot.Occurrences);
            db.Attachments.AddRange(snapshot.Attachments);
            db.TransactionRevisions.AddRange(snapshot.Revisions);
            db.AccountReconciliations.AddRange(snapshot.Reconciliations);
            db.NotificationSchedules.AddRange(snapshot.Notifications);
            db.AppSettings.AddRange(snapshot.Settings);
            db.AuditEntries.Add(new AuditEntry { EntityType = "Backup", EntityId = Guid.NewGuid(), Action = "RestoredEncryptedBackup" });
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var rollbackCandidate = PathSafety.ResolveDescendantWithoutLinks(_appDataRoot, $"attachments.rollback.{Guid.NewGuid():N}", "Restore rollback path is invalid.");
            if (Directory.Exists(AttachmentRoot))
            {
                Directory.Move(AttachmentRoot, rollbackCandidate);
                rollbackDirectory = rollbackCandidate;
            }
            Directory.Move(stagedDirectory, AttachmentRoot);
            stagedDirectory = null;
            attachmentsPromoted = true;
            try
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                Cleanup(rollbackDirectory);
                rollbackDirectory = null;
            }
            catch
            {
                if (!RestoreDirectoryRecovery.TryRestore(AttachmentRoot, ref rollbackDirectory, attachmentsPromoted))
                    throw new IOException("Restore database commit failed and receipt storage could not be returned to its pre-restore state.");
                attachmentsPromoted = false;
                throw;
            }
            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            _ = RestoreDirectoryRecovery.TryRestore(AttachmentRoot, ref rollbackDirectory, attachmentsPromoted);
            Cleanup(stagedDirectory);
            throw;
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or InvalidDataException or DbUpdateException or ArgumentException or IOException or UnauthorizedAccessException or InvalidOperationException or OverflowException)
        {
            var receiptsRestored = RestoreDirectoryRecovery.TryRestore(AttachmentRoot, ref rollbackDirectory, attachmentsPromoted);
            Cleanup(stagedDirectory);
            return receiptsRestored
                ? Result.Failure("Restore failed safely; the existing database and receipt storage were not left in a partial restored state.")
                : Result.Failure("Restore failed; receipt storage could not be returned automatically to its pre-restore state. Recovery data was preserved where available.");
        }
        finally
        {
            if (snapshot is not null) ZeroAttachmentBlobs(snapshot.AttachmentBlobs);
        }
    }

    private async Task<Snapshot> ReadAndValidateAsync(Stream stream, string password, CancellationToken cancellationToken)
    {
        ValidatePassword(password);
        if (!stream.CanRead) throw new InvalidDataException("Backup cannot be read.");
        if (stream.CanSeek && stream.Length > MaximumEncryptedBytes) throw new InvalidDataException("Backup file is too large.");
        if (stream.CanSeek) stream.Position = 0;
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        if (buffer.Length > MaximumEncryptedBytes) throw new InvalidDataException("Backup file is too large.");
        var plaintext = Decrypt(buffer.ToArray(), password);
        Snapshot? snapshot = null;
        try
        {
            snapshot = JsonSerializer.Deserialize<Snapshot>(plaintext, Json) ?? throw new InvalidDataException("Backup payload is empty.");
            if (snapshot.SchemaVersion <= 0) throw new InvalidDataException("Backup schema is invalid.");
            ValidateUniqueIds(snapshot);
            ValidateSnapshot(snapshot);
            if (snapshot.Attachments.Count != snapshot.AttachmentBlobs.Count)
                throw new InvalidDataException("Backup attachment metadata is incomplete.");
            foreach (var attachment in snapshot.Attachments)
            {
                _ = ResolveAttachmentPath(attachment.RelativePath);
                var blob = snapshot.AttachmentBlobs.Single(x => x.AttachmentId == attachment.Id);
                if (blob.Data.LongLength != attachment.SizeBytes) throw new InvalidDataException("Backup attachment size is invalid.");
                if (attachment.Sha256 is not { Length: 32 }) throw new InvalidDataException("Backup attachment checksum metadata is missing or invalid.");
                var hash = SHA256.HashData(blob.Data);
                if (!CryptographicOperations.FixedTimeEquals(hash, attachment.Sha256))
                    throw new InvalidDataException("Backup attachment integrity check failed.");
            }
            return snapshot;
        }
        catch
        {
            if (snapshot is not null) ZeroAttachmentBlobs(snapshot.AttachmentBlobs);
            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    private static void ValidateSnapshot(Snapshot snapshot)
        => BackupGraphValidator.Validate(snapshot.Accounts, snapshot.Transactions, snapshot.Splits, snapshot.Categories, snapshot.Tags, snapshot.TransactionTags, snapshot.Budgets, snapshot.BudgetPeriods, snapshot.Goals, snapshot.Contributions, snapshot.Rules, snapshot.Occurrences, snapshot.Attachments, snapshot.Revisions, snapshot.Reconciliations, snapshot.Notifications, snapshot.Settings);

    private static void ValidateUniqueIds(Snapshot snapshot)
    {
        EnsureUnique(snapshot.Accounts.Select(x => x.Id), "accounts");
        EnsureUnique(snapshot.Transactions.Select(x => x.Id), "transactions");
        EnsureUnique(snapshot.Splits.Select(x => x.Id), "transaction splits");
        EnsureUnique(snapshot.Categories.Select(x => x.Id), "categories");
        EnsureUnique(snapshot.Tags.Select(x => x.Id), "tags");
        EnsureUnique(snapshot.Budgets.Select(x => x.Id), "budgets");
        EnsureUnique(snapshot.BudgetPeriods.Select(x => x.Id), "budget periods");
        EnsureUnique(snapshot.Goals.Select(x => x.Id), "savings goals");
        EnsureUnique(snapshot.Contributions.Select(x => x.Id), "goal contributions");
        EnsureUnique(snapshot.Rules.Select(x => x.Id), "recurrence rules");
        EnsureUnique(snapshot.Occurrences.Select(x => x.Id), "recurrence occurrences");
        EnsureUnique(snapshot.Attachments.Select(x => x.Id), "attachments");
        EnsureUnique(snapshot.AttachmentBlobs.Select(x => x.AttachmentId), "attachment blobs");
        EnsureUnique(snapshot.Revisions.Select(x => x.Id), "transaction revisions");
        EnsureUnique(snapshot.Reconciliations.Select(x => x.Id), "reconciliations");
        EnsureUnique(snapshot.Notifications.Select(x => x.Id), "notification schedules");
        if (snapshot.Settings.GroupBy(x => x.Key, StringComparer.Ordinal).Any(group => group.Count() != 1))
            throw new InvalidDataException("Backup contains duplicate setting keys.");
    }

    private static void EnsureUnique(IEnumerable<Guid> ids, string entityName)
    {
        var seen = new HashSet<Guid>();
        foreach (var id in ids)
        {
            if (id == Guid.Empty || !seen.Add(id))
                throw new InvalidDataException($"Backup contains invalid or duplicate {entityName} identifiers.");
        }
    }

    private static void ZeroAttachmentBlobs(IEnumerable<AttachmentBlob> blobs)
    {
        foreach (var blob in blobs) CryptographicOperations.ZeroMemory(blob.Data);
    }

    private static byte[] Encrypt(byte[] plaintext, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16); var nonce = RandomNumberGenerator.GetBytes(12); var tag = new byte[16]; var ciphertext = new byte[plaintext.Length];
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, 210_000, HashAlgorithmName.SHA256, 32);
        try { using var aes = new AesGcm(key, 16); aes.Encrypt(nonce, plaintext, ciphertext, tag, Encoding.ASCII.GetBytes(AppConstants.BackupMagic)); }
        finally { CryptographicOperations.ZeroMemory(key); }
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
        writer.Write(Encoding.ASCII.GetBytes(AppConstants.BackupMagic)); writer.Write(salt); writer.Write(nonce); writer.Write(tag); writer.Write(ciphertext.Length); writer.Write(ciphertext); return stream.ToArray();
    }

    private static byte[] Decrypt(byte[] data, string password)
    {
        using var stream = new MemoryStream(data); using var reader = new BinaryReader(stream);
        var magic = Encoding.ASCII.GetString(reader.ReadBytes(AppConstants.BackupMagic.Length)); if (magic != AppConstants.BackupMagic) throw new InvalidDataException("Not a Finora backup.");
        var salt = reader.ReadBytes(16); var nonce = reader.ReadBytes(12); var tag = reader.ReadBytes(16);
        if (salt.Length != 16 || nonce.Length != 12 || tag.Length != 16) throw new InvalidDataException("Backup header is truncated.");
        var length = reader.ReadInt32();
        if (length < 0 || length > MaximumEncryptedBytes || reader.BaseStream.Length - reader.BaseStream.Position != length) throw new InvalidDataException("Backup length is invalid.");
        var ciphertext = reader.ReadBytes(length); var plaintext = new byte[length]; var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, 210_000, HashAlgorithmName.SHA256, 32);
        try { using var aes = new AesGcm(key, 16); aes.Decrypt(nonce, ciphertext, tag, plaintext, Encoding.ASCII.GetBytes(AppConstants.BackupMagic)); }
        finally { CryptographicOperations.ZeroMemory(key); }
        return plaintext;
    }

    private string ResolveAttachmentPath(string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var prefix = "attachments" + Path.DirectorySeparatorChar;
        if (!normalized.StartsWith(prefix, PathSafety.Comparison))
            throw new InvalidDataException("Attachment path is outside Finora receipt storage.");
        return PathSafety.ResolveDescendantWithoutLinks(AttachmentRoot, normalized[prefix.Length..], "Attachment path escaped Finora storage or traversed a link.");
    }

    private string ResolveStagedPath(string stagedRoot, string relativePath)
    {
        var livePath = ResolveAttachmentPath(relativePath);
        var attachmentRelative = Path.GetRelativePath(AttachmentRoot, livePath);
        return PathSafety.ResolveDescendantWithoutLinks(stagedRoot, attachmentRelative, "Staged attachment path is invalid or traversed a link.");
    }

    private static void ValidatePassword(string password) { if (string.IsNullOrWhiteSpace(password) || password.Length < 8) throw new ArgumentException("Backup password must be at least 8 characters."); }

    private static void Cleanup(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return;
        try
        {
            if (PathSafety.IsSymbolicLink(directory)) Directory.Delete(directory);
            else Directory.Delete(directory, recursive: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private sealed record AttachmentBlob(Guid AttachmentId, byte[] Data);
    private sealed record Snapshot(int SchemaVersion, DateTimeOffset CreatedAtUtc, List<Account> Accounts, List<FinanceTransaction> Transactions, List<TransactionSplit> Splits, List<Category> Categories, List<Tag> Tags, List<TransactionTag> TransactionTags, List<Budget> Budgets, List<BudgetPeriod> BudgetPeriods, List<SavingsGoal> Goals, List<GoalContribution> Contributions, List<RecurrenceRule> Rules, List<RecurrenceOccurrence> Occurrences, List<Attachment> Attachments, List<AttachmentBlob> AttachmentBlobs, List<TransactionRevision> Revisions, List<AccountReconciliation> Reconciliations, List<NotificationSchedule> Notifications, List<AppSetting> Settings);
}
