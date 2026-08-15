using System.Globalization;
using Finora.Infrastructure;
using Finora.Shared;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class DatabaseMigrationDataPreservationTests
{
    [Fact]
    public async Task Version1_PreservesAttachmentAndSecondRunIsIdempotent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"finora-migration-preserve-{Guid.NewGuid():N}.db");
        const string attachmentId = "attachment-1";
        try
        {
            await using (var connection = new SqliteConnection($"Data Source={path}"))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = """
                    CREATE TABLE "AppSettings" ("Id" TEXT NOT NULL PRIMARY KEY,"CreatedAtUtc" TEXT NOT NULL,"UpdatedAtUtc" TEXT NOT NULL,"Key" TEXT NOT NULL,"Value" TEXT NOT NULL);
                    CREATE TABLE "Accounts" ("Id" TEXT NOT NULL PRIMARY KEY);
                    CREATE TABLE "Transactions" ("Id" TEXT NOT NULL PRIMARY KEY);
                    CREATE TABLE "Attachments" ("Id" TEXT NOT NULL PRIMARY KEY,"CreatedAtUtc" TEXT NOT NULL,"UpdatedAtUtc" TEXT NOT NULL,"TransactionId" TEXT NOT NULL,"RelativePath" TEXT NOT NULL,"ContentType" TEXT NOT NULL,"SizeBytes" INTEGER NOT NULL,"Sha256" BLOB NULL);
                    INSERT INTO "AppSettings" ("Id","CreatedAtUtc","UpdatedAtUtc","Key","Value") VALUES ('schema-setting',datetime('now'),datetime('now'),'schema.version','1');
                    INSERT INTO "Accounts" ("Id") VALUES ('account-1');
                    INSERT INTO "Transactions" ("Id") VALUES ('transaction-1');
                    INSERT INTO "Attachments" ("Id","CreatedAtUtc","UpdatedAtUtc","TransactionId","RelativePath","ContentType","SizeBytes","Sha256")
                    VALUES ('attachment-1',datetime('now'),datetime('now'),'transaction-1','receipts/one.jpg','image/jpeg',123,NULL);
                    """;
                await command.ExecuteNonQueryAsync();
            }

            var options = new DbContextOptionsBuilder<FinoraDbContext>().UseSqlite($"Data Source={path}").Options;
            await using var db = new FinoraDbContext(options);
            var runner = new DatabaseMigrationRunner();

            await runner.MigrateAsync(db);
            await runner.MigrateAsync(db);

            var version = await db.AppSettings.SingleAsync(x => x.Key == "schema.version");
            Assert.Equal(AppConstants.DatabaseSchemaVersion.ToString(CultureInfo.InvariantCulture), version.Value);

            await using var verification = new SqliteConnection($"Data Source={path}");
            await verification.OpenAsync();
            var attachmentCommand = verification.CreateCommand();
            attachmentCommand.CommandText = "SELECT \"Id\", \"RelativePath\", \"ContentType\", \"SizeBytes\", \"OriginalFileName\" FROM \"Attachments\" WHERE \"Id\" = $id;";
            attachmentCommand.Parameters.AddWithValue("$id", attachmentId);
            await using var reader = await attachmentCommand.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(attachmentId, reader.GetString(0));
            Assert.Equal("receipts/one.jpg", reader.GetString(1));
            Assert.Equal("image/jpeg", reader.GetString(2));
            Assert.Equal(123, reader.GetInt64(3));
            Assert.Equal("receipt", reader.GetString(4));
            Assert.False(await reader.ReadAsync());
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    private static void DeleteDatabase(string path)
    {
        try
        {
            File.Delete(path);
            File.Delete(path + "-wal");
            File.Delete(path + "-shm");
        }
        catch (IOException)
        {
        }
    }
}