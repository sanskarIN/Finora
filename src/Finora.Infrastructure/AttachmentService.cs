using System.Security.Cryptography;
using Finora.Application;
using Finora.Domain;
using Finora.Shared;
using Microsoft.EntityFrameworkCore;

namespace Finora.Infrastructure;

public sealed class AttachmentService(IDbContextFactory<FinoraDbContext> factory, string appDataRoot) : IAttachmentService
{
    private const long MaximumAttachmentBytes = 20L * 1024 * 1024;
    private readonly IDbContextFactory<FinoraDbContext> _factory = factory;
    private readonly string _attachmentRoot = Path.GetFullPath(Path.Combine(appDataRoot, "attachments"));
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/heic", "image/heif", "application/pdf"
    };

    public async Task<IReadOnlyList<AttachmentInfo>> GetAttachmentsAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rows = await db.Attachments.AsNoTracking().Where(x => x.TransactionId == transactionId).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(ToInfo).ToList();
    }

    public async Task<Result<AttachmentInfo>> AddAttachmentAsync(Guid transactionId, Stream source, string originalFileName, string contentType, CancellationToken cancellationToken = default)
    {
        if (source is null || !source.CanRead) return Result<AttachmentInfo>.Failure("The selected attachment cannot be read.");
        var normalizedContentType = contentType?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedContentType) || !AllowedContentTypes.Contains(normalizedContentType))
            return Result<AttachmentInfo>.Failure("Use a JPEG, PNG, WebP, HEIC/HEIF image, or PDF receipt.");

        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var exists = await db.Transactions.AsNoTracking().AnyAsync(x => x.Id == transactionId && !x.IsDeleted, cancellationToken).ConfigureAwait(false);
        if (!exists) return Result<AttachmentInfo>.Failure("The transaction no longer exists.");

        var attachmentId = Guid.NewGuid();
        var safeName = SanitizeFileName(originalFileName);
        var extension = ExtensionForContentType(normalizedContentType);
        var transactionDirectory = transactionId.ToString("N");
        var relativeWithinAttachments = Path.Combine(transactionDirectory, $"{attachmentId:N}{extension}");
        var finalPath = ResolveSafePath(relativeWithinAttachments);
        Directory.CreateDirectory(Path.GetDirectoryName(finalPath) ?? _attachmentRoot);
        var tempPath = finalPath + ".tmp";

        try
        {
            long copied = 0;
            await using (var output = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var buffer = new byte[81920];
                while (true)
                {
                    var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
                    if (read == 0) break;
                    copied = checked(copied + read);
                    if (copied > MaximumAttachmentBytes) return Result<AttachmentInfo>.Failure("Attachments are limited to 20 MB each.");
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                }
                await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            if (copied == 0) return Result<AttachmentInfo>.Failure("The selected attachment is empty.");
            byte[] hash;
            await using (var input = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
                hash = await SHA256.HashDataAsync(input, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, finalPath, false);
            var attachment = new Attachment
            {
                Id = attachmentId,
                TransactionId = transactionId,
                RelativePath = Path.Combine("attachments", relativeWithinAttachments).Replace('\\', '/'),
                OriginalFileName = safeName,
                ContentType = normalizedContentType,
                SizeBytes = copied,
                Sha256 = hash
            };
            db.Attachments.Add(attachment);
            db.AuditEntries.Add(new AuditEntry { EntityType = "Attachment", EntityId = attachment.Id, Action = "Created" });
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<AttachmentInfo>.Success(ToInfo(attachment));
        }
        catch (OperationCanceledException) { SafeDelete(tempPath); SafeDelete(finalPath); throw; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DbUpdateException or InvalidDataException)
        {
            SafeDelete(tempPath); SafeDelete(finalPath);
            return Result<AttachmentInfo>.Failure("Finora could not save the attachment safely.");
        }
        finally { SafeDelete(tempPath); }
    }

    public async Task<Result<string>> GetLocalPathAsync(Guid attachmentId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var relativePath = await db.Attachments.AsNoTracking().Where(x => x.Id == attachmentId).Select(x => x.RelativePath).SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (relativePath is null) return Result<string>.Failure("Attachment not found.");
        try
        {
            var path = ResolveStoredPath(relativePath);
            return File.Exists(path) ? Result<string>.Success(path) : Result<string>.Failure("The attachment file is missing from local storage.");
        }
        catch (InvalidDataException)
        {
            return Result<string>.Failure("The attachment path is invalid.");
        }
    }

    public async Task<Result> DeleteAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var attachment = await db.Attachments.SingleOrDefaultAsync(x => x.Id == attachmentId, cancellationToken).ConfigureAwait(false);
        if (attachment is null) return Result.Failure("Attachment not found.");

        string path;
        try { path = ResolveStoredPath(attachment.RelativePath); }
        catch (InvalidDataException) { return Result.Failure("The attachment path is invalid; run the data-integrity check before deleting metadata."); }

        db.Attachments.Remove(attachment);
        db.AuditEntries.Add(new AuditEntry { EntityType = "Attachment", EntityId = attachment.Id, Action = "Deleted" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        SafeDelete(path);
        DeleteDirectoryIfEmpty(Path.GetDirectoryName(path));
        return Result.Success();
    }

    public async Task<long> GetStorageUsageBytesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Attachments.AsNoTracking().SumAsync(x => (long?)x.SizeBytes, cancellationToken).ConfigureAwait(false) ?? 0L;
    }

    public async Task<int> CleanupOrphanedFilesAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_attachmentRoot);
        PathSafety.EnsureNotLinkIfExists(_attachmentRoot, "Attachment root cannot be a symbolic link or reparse point.");
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var storedPaths = await db.Attachments.AsNoTracking().Select(x => x.RelativePath).ToListAsync(cancellationToken).ConfigureAwait(false);
        var known = new HashSet<string>(PathSafety.Comparer);
        foreach (var storedPath in storedPaths)
        {
            try { known.Add(ResolveStoredPath(storedPath)); }
            catch (InvalidDataException) { }
        }

        var removed = 0;
        foreach (var full in PathSafety.EnumerateFilesWithoutLinks(_attachmentRoot, "Attachment cleanup encountered a linked or escaped path."))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (known.Contains(full)) continue;
            SafeDelete(full);
            removed++;
        }
        return removed;
    }

    private static AttachmentInfo ToInfo(Attachment x) => new(x.Id, x.TransactionId, x.OriginalFileName, x.ContentType, x.SizeBytes, x.Sha256 is null ? string.Empty : Convert.ToHexString(x.Sha256), x.CreatedAtUtc);

    private string ResolveStoredPath(string storedRelativePath)
    {
        var normalized = storedRelativePath.Replace('/', Path.DirectorySeparatorChar);
        var attachmentPrefix = "attachments" + Path.DirectorySeparatorChar;
        if (!normalized.StartsWith(attachmentPrefix, PathSafety.Comparison))
            throw new InvalidDataException("Attachment path is outside the attachment root.");
        return ResolveSafePath(normalized[attachmentPrefix.Length..]);
    }

    private string ResolveSafePath(string relativeWithinAttachments)
        => PathSafety.ResolveDescendantWithoutLinks(_attachmentRoot, relativeWithinAttachments, "Attachment path escaped app storage or traversed a link.");

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(string.IsNullOrWhiteSpace(fileName) ? "receipt" : fileName.Trim());
        foreach (var invalid in Path.GetInvalidFileNameChars()) name = name.Replace(invalid, '_');
        if (name.Length > 180) name = name[..180];
        return string.IsNullOrWhiteSpace(name) ? "receipt" : name;
    }

    private static string ExtensionForContentType(string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/heic" => ".heic",
        "image/heif" => ".heif",
        "application/pdf" => ".pdf",
        _ => ".jpg"
    };

    private static void SafeDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    private static void DeleteDirectoryIfEmpty(string? path) { if (string.IsNullOrWhiteSpace(path)) return; try { if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any()) Directory.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
}
