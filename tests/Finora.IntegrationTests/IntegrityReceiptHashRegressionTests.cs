using Finora.Infrastructure;

namespace Finora.IntegrationTests;

public sealed class IntegrityReceiptHashRegressionTests
{
    [Fact]
    public async Task IntegrityCheck_DetectsReceiptBytesChangedWithoutMetadataUpdate()
    {
        await using var fixture = await BackupAttachmentFixture.CreateAsync();
        var attachment = Assert.Single(await fixture.Attachments.GetAttachmentsAsync(fixture.TransactionId));
        var localPath = await fixture.Attachments.GetLocalPathAsync(attachment.Id);
        Assert.True(localPath.IsSuccess);

        var bytes = await File.ReadAllBytesAsync(localPath.Value!);
        Assert.NotEmpty(bytes);
        bytes[0] ^= 0x01;
        await File.WriteAllBytesAsync(localPath.Value!, bytes);

        var report = await new DataIntegrityService(fixture.Factory, fixture.Root).CheckAsync();

        Assert.False(report.IsHealthy);
        Assert.Contains(report.Issues, issue =>
            issue.Code == "ATTACHMENT_HASH_MISMATCH" && issue.AffectedRecords == 1);
    }
}
