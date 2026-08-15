using Finora.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class DatabaseMigrationForeignKeyTests
{
    [Fact]
    public async Task LegacyForeignKeyViolation_RollsBackMigrationAndVersion()
    {
        var path = Path.Combine(Path.GetTempPath(), $"finora-migration-fk-{Guid.NewGuid():N}.db");
        try
        {
            await using (var connection = new SqliteConnection($"Data Source={path}"))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = """
                    PRAGMA foreign_keys = OFF;
                    CREATE TABLE "AppSettings" ("Id" TEXT NOT NULL PRIMARY KEY,"CreatedAtUtc" TEXT NOT NULL,"UpdatedAtUtc" TEXT NOT NULL,"Key" TEXT NOT NULL,"Value" TEXT NOT NULL);
                    CREATE TABLE "Accounts" ("Id" TEXT NOT NULL PRIMARY KEY);
                    CREATE TABLE "Transactions" ("Id" TEXT NOT NULL PRIMARY KEY);
                    CREATE TABLE "Attachments" (
                        "Id" TEXT NOT NULL PRIMARY KEY,
                        "CreatedAtUtc" TEXT NOT NULL,
                        "UpdatedAtUtc" TEXT NOT NULL,
                        "TransactionId" TEXT NOT NULL,
                        "RelativePath" TEXT NOT NULL,
                        "ContentType" TEXT NOT NULL,
                        "SizeBytes" INTEGER NOT NULL,
                        "Sha256" BLOB NULL,
                        CONSTRAINT "FK_Attachments_Transactions_TransactionId" FOREIGN KEY ("TransactionId") REFERENCES "Transactions" ("Id") ON DELETE CASCADE
                    );
                    INSERT INTO "AppSettings" ("Id","CreatedAtUtc","UpdatedAtUtc","Key","Value") VALUES ('11111111-1111-1111-1111-111111111111',datetime('now'),datetime('now'),'schema.version','1');
                    INSERT INTO "Attachments" ("Id","CreatedAtUtc","UpdatedAtUtc","TransactionId","RelativePath","ContentType","SizeBytes","Sha256")
                    VALUES ('attachment-1',datetime('now'),datetime('now'),'missing-transaction','receipts/orphan.jpg','image/jpeg',10,NULL);
                    """;
                await command.ExecuteNonQueryAsync();
            }

            var options = new DbContextOptionsBuilder<FinoraDbContext>().UseSqlite($"Data Source={path}").Options;
            await using var db = new FinoraDbContext(options);

            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => new DatabaseMigrationRunner().MigrateAsync(db));
            Assert.Contains("foreign-key violation", exception.Message, StringComparison.OrdinalIgnoreCase);

            await using var verification = new SqliteConnection($"Data Source={path}");
            await verification.OpenAsync();
            var versionCommand = verification.CreateCommand();
            versionCommand.CommandText = "SELECT \"Value\" FROM \"AppSettings\" WHERE \"Key\" = 'schema.version';";
            Assert.Equal("1", (string)(await versionCommand.ExecuteScalarAsync())!);

            var columnCommand = verification.CreateCommand();
            columnCommand.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Attachments') WHERE name = 'OriginalFileName';";
            Assert.Equal(0L, (long)(await columnCommand.ExecuteScalarAsync())!);
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
