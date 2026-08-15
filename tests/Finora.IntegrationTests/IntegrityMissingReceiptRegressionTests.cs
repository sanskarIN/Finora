using Finora.Infrastructure;

namespace Finora.IntegrationTests;

public sealed class IntegrityMissingReceiptRegressionTests
{
    [Fact]
    public async Task IntegrityCheck_DetectsReceiptMissingFromPrivateStorage()
    {
        await using var fixture = await BackupAttachmentFixture.CreateAsync();
        var attachment = Assert.Single(await fixture.Attachments.GetAttachmentsAsync(fixture.TransactionId));
        var localPath = await fixture.Attachments.GetLocalPathAsync(attachment.Id);
        Assert.True(localPath.IsSuccess);
        File.Delete(localPath.Value!);

        var report = await new DataIntegrityService(fixture.Factory, fixture.Root).CheckAsync();

        Assert.False(report.IsHealthy);
        Assert.Contains(report.Issues, issue =>
            issue.Code == "ATTACHMENT_FILE_MISSING" && issue.AffectedRecords == 1);
    }
}
