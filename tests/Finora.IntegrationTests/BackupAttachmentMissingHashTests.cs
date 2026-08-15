using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class BackupAttachmentMissingHashTests
{
    [Fact]
    public async Task BackupCreation_RejectsReceiptWithoutChecksumMetadata()
    {
        await using var fixture = await BackupAttachmentFixture.CreateAsync();
        var attachment = Assert.Single(await fixture.Attachments.GetAttachmentsAsync(fixture.TransactionId));
        await using (var db = await fixture.Factory.CreateDbContextAsync())
        {
            await db.Attachments.Where(row => row.Id == attachment.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.Sha256, (byte[]?)null));
        }

        await Assert.ThrowsAsync<InvalidDataException>(() => fixture.Backup.CreateEncryptedBackupAsync(BackupAttachmentFixture.Password));
    }

    [Fact]
    public async Task AuthenticatedBackupWithoutReceiptChecksum_IsRejectedWithoutMutation()
    {
        await using var fixture = await BackupAttachmentFixture.CreateAsync();
        var valid = await fixture.CreateBackupAsync();
        var corrupted = BackupTestCipher.RewriteJson(valid, BackupAttachmentFixture.Password, root =>
        {
            var attachments = root["attachments"]?.AsArray()
                ?? throw new InvalidDataException("Backup fixture has no attachments array.");
            var attachment = attachments.Single()?.AsObject()
                ?? throw new InvalidDataException("Backup fixture has no attachment object.");
            attachment["sha256"] = null;
        });

        await using (var previewStream = new MemoryStream(corrupted, writable: false))
        {
            var preview = await fixture.Backup.PreviewEncryptedBackupAsync(previewStream, BackupAttachmentFixture.Password);
            Assert.False(preview.IsSuccess);
        }

        await using (var restoreStream = new MemoryStream(corrupted, writable: false))
        {
            var restore = await fixture.Backup.RestoreEncryptedBackupAsync(restoreStream, BackupAttachmentFixture.Password);
            Assert.False(restore.IsSuccess);
        }

        await fixture.AssertCurrentDataIntactAsync();
    }
}
