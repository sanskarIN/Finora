using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class IntegrityReceiptHashMetadataRegressionTests
{
    [Fact]
    public async Task IntegrityCheck_DetectsMissingReceiptChecksumMetadata()
    {
        await using var fixture = await BackupAttachmentFixture.CreateAsync();
        var attachment = Assert.Single(await fixture.Attachments.GetAttachmentsAsync(fixture.TransactionId));
        await using (var db = await fixture.Factory.CreateDbContextAsync())
        {
            await db.Attachments.Where(row => row.Id == attachment.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.Sha256, (byte[]?)null));
        }

        var report = await new DataIntegrityService(fixture.Factory, fixture.Root).CheckAsync();

        Assert.False(report.IsHealthy);
        Assert.Contains(report.Issues, issue =>
            issue.Code == "ATTACHMENT_HASH_INVALID" && issue.AffectedRecords == 1);
    }
}
