using Finora.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class DatabaseMigrationRollbackTests
{
    [Fact]
    public async Task MalformedPreexistingSchema2Table_RollsBackMigrationAndVersion()
    {
        var path = Path.Combine(Path.GetTempPath(), $"finora-migration-rollback-{Guid.NewGuid():N}.db");
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
                    CREATE TABLE "NotificationSchedules" ("Id" TEXT NOT NULL PRIMARY KEY,"TriggerAtUtc" TEXT NOT NULL,"DedupeKey" TEXT NULL);
                    INSERT INTO "AppSettings" ("Id","CreatedAtUtc","UpdatedAtUtc","Key","Value") VALUES ('schema-setting',datetime('now'),datetime('now'),'schema.version','1');
                    """;
                await command.ExecuteNonQueryAsync();
            }

            var options = new DbContextOptionsBuilder<FinoraDbContext>().UseSqlite($"Data Source={path}").Options;
            await using var db = new FinoraDbContext(options);

            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => new DatabaseMigrationRunner().MigrateAsync(db));
            Assert.Contains("NotificationSchedules", exception.Message, StringComparison.Ordinal);

            await using var verification = new SqliteConnection($"Data Source={path}");
            await verification.OpenAsync();

            var versionCommand = verification.CreateCommand();
            versionCommand.CommandText = "SELECT \"Value\" FROM \"AppSettings\" WHERE \"Key\" = 'schema.version';";
            Assert.Equal("1", (string)(await versionCommand.ExecuteScalarAsync())!);

            var attachmentColumns = await ReadColumnsAsync(verification, "Attachments");
            Assert.DoesNotContain("OriginalFileName", attachmentColumns);

            var tableCommand = verification.CreateCommand();
            tableCommand.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name IN ('TransactionRevisions','AccountReconciliations');";
            Assert.Equal(0L, (long)(await tableCommand.ExecuteScalarAsync())!);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    private static async Task<HashSet<string>> ReadColumnsAsync(SqliteConnection connection, string tableName)
    {
        var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName}\");";
        await using var reader = await command.ExecuteReaderAsync();
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(1));
        }

        return columns;
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