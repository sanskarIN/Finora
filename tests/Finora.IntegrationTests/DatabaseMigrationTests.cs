using Finora.Infrastructure;
using Finora.Shared;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class DatabaseMigrationTests
{
    [Fact]
    public async Task Version1_MigratesAtomicallyToVersion2()
    {
        var path = Path.Combine(Path.GetTempPath(), $"finora-migration-{Guid.NewGuid():N}.db");
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
                    INSERT INTO "AppSettings" ("Id","CreatedAtUtc","UpdatedAtUtc","Key","Value") VALUES (lower(hex(randomblob(16))),datetime('now'),datetime('now'),'schema.version','1');
                    """;
                await command.ExecuteNonQueryAsync();
            }

            var options = new DbContextOptionsBuilder<FinoraDbContext>().UseSqlite($"Data Source={path}").Options;
            var factory = new FinanceStoreTests.TestFactory(options);
            await using var db = await factory.CreateDbContextAsync();
            await new DatabaseMigrationRunner().MigrateAsync(db);
            var version = await db.AppSettings.SingleAsync(x => x.Key == "schema.version");
            Assert.Equal(AppConstants.DatabaseSchemaVersion.ToString(), version.Value);
            var columns = await db.Database.SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('Attachments')").ToListAsync();
            Assert.Contains("OriginalFileName", columns);
        }
        finally
        {
            try { File.Delete(path); } catch (IOException) { }
        }
    }
}
