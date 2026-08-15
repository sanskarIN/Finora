namespace Finora.IntegrationTests;

public sealed class BackupAttachmentHashTamperTests
{
    [Fact]
    public async Task AuthenticatedReceiptHashDrift_IsRejectedWithoutMutation()
    {
        await using var fixture = await BackupAttachmentFixture.CreateAsync();
        var valid = await fixture.CreateBackupAsync();
        var corrupted = BackupTestCipher.RewriteJson(valid, BackupAttachmentFixture.Password, root =>
        {
            var attachments = root["attachments"]?.AsArray()
                ?? throw new InvalidDataException("Backup fixture has no attachments array.");
            var attachment = attachments.Single()?.AsObject()
                ?? throw new InvalidDataException("Backup fixture has no attachment object.");
            attachment["sha256"] = Convert.ToBase64String(new byte[32]);
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
