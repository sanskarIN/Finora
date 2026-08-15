namespace Finora.IntegrationTests;

public sealed class BackupAttachmentSizeTamperTests
{
    [Fact]
    public async Task AuthenticatedReceiptSizeDrift_IsRejectedWithoutMutation()
    {
        await using var fixture = await BackupAttachmentFixture.CreateAsync();
        var valid = await fixture.CreateBackupAsync();
        var corrupted = BackupTestCipher.RewriteJson(valid, BackupAttachmentFixture.Password, root =>
        {
            var attachments = root["attachments"]?.AsArray()
                ?? throw new InvalidDataException("Backup fixture has no attachments array.");
            var attachment = attachments.Single()?.AsObject()
                ?? throw new InvalidDataException("Backup fixture has no attachment object.");
            var originalSize = attachment["sizeBytes"]?.GetValue<long>()
                ?? throw new InvalidDataException("Backup fixture has no receipt size.");
            attachment["sizeBytes"] = originalSize + 1;
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
