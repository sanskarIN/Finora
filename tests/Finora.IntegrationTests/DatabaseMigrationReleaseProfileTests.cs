using System.Text;
using Finora.Application;
using Finora.Domain;
using Finora.Infrastructure;
using Finora.Shared;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class DatabaseMigrationReleaseProfileTests
{
    [Fact]
    public async Task Version1RepresentativeProfile_MigratesToHealthySchema2()
    {
        var root = Path.Combine(Path.GetTempPath(), $"finora-migration-profile-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var databasePath = Path.Combine(root, "finora.db");
        var connectionString = $"Data Source={databasePath}";

        try
        {
            var options = new DbContextOptionsBuilder<FinoraDbContext>()
                .UseSqlite(connectionString)
                .Options;
            var factory = new FinanceStoreTests.TestFactory(options);
            var initializer = new DatabaseInitializer(factory);
            await initializer.InitializeAsync();

            var store = new FinanceStore(factory, initializer);
            var sampleData = new SampleDataService(
                new FinanceDataResetService(factory),
                store,
                initializer);
            var sampleResult = await sampleData.ResetToSyntheticSampleDataAsync("INR");
            Assert.True(sampleResult.IsSuccess, sampleResult.Error);

            Guid transactionId;
            await using (var db = await factory.CreateDbContextAsync())
            {
                var transaction = await db.Transactions
                    .Where(item => item.Type == TransactionType.Expense && item.TransferGroupId == null && !item.IsDeleted)
                    .OrderBy(item => item.OccurredAtUtc)
                    .FirstAsync();
                transactionId = transaction.Id;

                var categories = await db.Categories
                    .Where(item => item.Name == "Food" || item.Name == "Transport")
                    .OrderBy(item => item.Name)
                    .ToListAsync();
                Assert.Equal(2, categories.Count);

                var firstSplitAmount = transaction.AmountMinor / 2;
                db.TransactionSplits.AddRange(
                    new TransactionSplit
                    {
                        TransactionId = transaction.Id,
                        CategoryId = categories[0].Id,
                        AmountMinor = firstSplitAmount,
                        Note = "Synthetic legacy split A"
                    },
                    new TransactionSplit
                    {
                        TransactionId = transaction.Id,
                        CategoryId = categories[1].Id,
                        AmountMinor = transaction.AmountMinor - firstSplitAmount,
                        Note = "Synthetic legacy split B"
                    });

                var tag = new Tag { Name = "Migration profile" };
                db.Tags.Add(tag);
                db.TransactionTags.Add(new TransactionTag
                {
                    TransactionId = transaction.Id,
                    TagId = tag.Id
                });
                await db.SaveChangesAsync();
            }

            var receiptBytes = Encoding.UTF8.GetBytes("%PDF-1.4\nSynthetic migration receipt\n%%EOF\n");
            var attachmentService = new AttachmentService(factory, root);
            await using (var receipt = new MemoryStream(receiptBytes, writable: false))
            {
                var attachmentResult = await attachmentService.AddAttachmentAsync(
                    transactionId,
                    receipt,
                    "legacy-receipt.pdf",
                    "application/pdf");
                Assert.True(attachmentResult.IsSuccess, attachmentResult.Error);
                Assert.NotNull(attachmentResult.Value);
            }

            Guid attachmentId;
            await using (var db = await factory.CreateDbContextAsync())
            {
                attachmentId = await db.Attachments.Select(item => item.Id).SingleAsync();

                await db.Database.ExecuteSqlRawAsync("""
                    DROP TABLE "TransactionRevisions";
                    DROP TABLE "AccountReconciliations";
                    DROP TABLE "NotificationSchedules";
                    ALTER TABLE "Attachments" DROP COLUMN "OriginalFileName";
                    UPDATE "AppSettings" SET "Value" = '1', "UpdatedAtUtc" = CURRENT_TIMESTAMP WHERE "Key" = 'schema.version';
                    """);
            }

            await using (var db = await factory.CreateDbContextAsync())
            {
                await new DatabaseMigrationRunner().MigrateAsync(db);
            }

            await using (var db = await factory.CreateDbContextAsync())
            {
                var version = await db.AppSettings.SingleAsync(item => item.Key == "schema.version");
                Assert.Equal(AppConstants.DatabaseSchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture), version.Value);

                Assert.True(await db.Accounts.CountAsync() >= 2);
                Assert.True(await db.Transactions.CountAsync() >= 6);
                Assert.Equal(2, await db.TransactionSplits.CountAsync());
                Assert.True(await db.Tags.AnyAsync(item => item.Name == "Migration profile"));
                Assert.Equal(1, await db.TransactionTags.CountAsync());
                Assert.True(await db.Budgets.AnyAsync());
                Assert.True(await db.SavingsGoals.AnyAsync());
                Assert.True(await db.RecurrenceRules.AnyAsync());

                var migratedAttachment = await db.Attachments.SingleAsync(item => item.Id == attachmentId);
                Assert.Equal("receipt", migratedAttachment.OriginalFileName);
                Assert.Equal(receiptBytes.Length, migratedAttachment.SizeBytes);
                Assert.NotNull(migratedAttachment.Sha256);
                Assert.Equal(32, migratedAttachment.Sha256!.Length);
            }

            var localPath = await attachmentService.GetLocalPathAsync(attachmentId);
            Assert.True(localPath.IsSuccess, localPath.Error);
            Assert.NotNull(localPath.Value);
            Assert.Equal(receiptBytes, await File.ReadAllBytesAsync(localPath.Value!));

            var integrity = await new DataIntegrityService(factory, root).CheckAsync();
            Assert.True(integrity.IsHealthy, integrity.ToSanitizedText());
            Assert.True(integrity.DatabaseIntegrityPassed);
            Assert.True(integrity.ForeignKeysPassed);
            Assert.True(integrity.AccountsChecked >= 2);
            Assert.True(integrity.TransactionsChecked >= 6);
            Assert.Equal(1, integrity.AttachmentsChecked);
        }
        finally
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
