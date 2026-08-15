using Finora.Infrastructure;
using Finora.Shared;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class DatabaseMigrationVersionGuardTests
{
    [Theory]
    [InlineData("")]
    [InlineData("not-a-number")]
    [InlineData("2.0")]
    public async Task InvalidSchemaVersion_IsRejectedWithoutMutation(string value)
    {
        var path = Path.Combine(Path.GetTempPath(), $"finora-migration-version-{Guid.NewGuid():N}.db");
        try
        {
            await CreateVersionDatabaseAsync(path, value);
            var options = new DbContextOptionsBuilder<FinoraDbContext>().UseSqlite($"Data Source={path}").Options;
            await using var db = new FinoraDbContext(options);

            await Assert.ThrowsAsync<InvalidDataException>(() => new DatabaseMigrationRunner().MigrateAsync(db));
            Assert.Equal(value, await ReadVersionAsync(path));
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task NewerSchemaVersion_IsRejectedWithoutMutation()
    {
        var path = Path.Combine(Path.GetTempPath(), $"finora-migration-newer-{Guid.NewGuid():N}.db");
        var newerVersion = (AppConstants.DatabaseSchemaVersion + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        try
        {
            await CreateVersionDatabaseAsync(path, newerVersion);
            var options = new DbContextOptionsBuilder<FinoraDbContext>().UseSqlite($"Data Source={path}").Options;
            await using var db = new FinoraDbContext(options);

            await Assert.ThrowsAsync<InvalidOperationException>(() => new DatabaseMigrationRunner().MigrateAsync(db));
            Assert.Equal(newerVersion, await ReadVersionAsync(path));
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task MissingSchemaVersion_IsRejected()
    {
        var path = Path.Combine(Path.GetTempPath(), $"finora-migration-missing-{Guid.NewGuid():N}.db");
        try
        {
            await using (var connection = new SqliteConnection($"Data Source={path}"))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "CREATE TABLE \"AppSettings\" (\"Id\" TEXT NOT NULL PRIMARY KEY,\"CreatedAtUtc\" TEXT NOT NULL,\"UpdatedAtUtc\" TEXT NOT NULL,\"Key\" TEXT NOT NULL,\"Value\" TEXT NOT NULL);";
                await command.ExecuteNonQueryAsync();
            }

            var options = new DbContextOptionsBuilder<FinoraDbContext>().UseSqlite($"Data Source={path}").Options;
            await using var db = new FinoraDbContext(options);
            await Assert.ThrowsAsync<InvalidOperationException>(() => new DatabaseMigrationRunner().MigrateAsync(db));
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    private static async Task CreateVersionDatabaseAsync(string path, string value)
    {
        await using var connection = new SqliteConnection($"Data Source={path}");
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE "AppSettings" ("Id" TEXT NOT NULL PRIMARY KEY,"CreatedAtUtc" TEXT NOT NULL,"UpdatedAtUtc" TEXT NOT NULL,"Key" TEXT NOT NULL,"Value" TEXT NOT NULL);
            INSERT INTO "AppSettings" ("Id","CreatedAtUtc","UpdatedAtUtc","Key","Value") VALUES (lower(hex(randomblob(16))),datetime('now'),datetime('now'),'schema.version',$value);
            """;
        command.Parameters.AddWithValue("$value", value);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string> ReadVersionAsync(string path)
    {
        await using var connection = new SqliteConnection($"Data Source={path}");
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT \"Value\" FROM \"AppSettings\" WHERE \"Key\" = 'schema.version';";
        return (string)(await command.ExecuteScalarAsync())!;
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