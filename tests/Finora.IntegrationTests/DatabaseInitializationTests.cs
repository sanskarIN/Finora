using System.Globalization;
using Finora.Infrastructure;
using Finora.Shared;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class DatabaseInitializationTests
{
    [Fact]
    public async Task FreshDatabase_CreatesCurrentSchemaAndCanReopen()
    {
        var path = Path.Combine(Path.GetTempPath(), $"finora-initialize-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<FinoraDbContext>().UseSqlite($"Data Source={path}").Options;
            var factory = new FinanceStoreTests.TestFactory(options);
            var initializer = new DatabaseInitializer(factory);

            await initializer.InitializeAsync();
            await initializer.InitializeAsync();

            await using var db = await factory.CreateDbContextAsync();
            var version = await db.AppSettings.SingleAsync(x => x.Key == "schema.version");
            Assert.Equal(AppConstants.DatabaseSchemaVersion.ToString(CultureInfo.InvariantCulture), version.Value);
            Assert.Equal(11, await db.Categories.CountAsync(category => category.IsSystem));

            var tables = await db.Database.SqlQueryRaw<string>(
                "SELECT name AS Value FROM sqlite_master WHERE type = 'table' AND name IN ('Attachments','TransactionRevisions','AccountReconciliations','NotificationSchedules') ORDER BY name")
                .ToListAsync();
            Assert.Equal(4, tables.Count);
            Assert.Contains("Attachments", tables);
            Assert.Contains("TransactionRevisions", tables);
            Assert.Contains("AccountReconciliations", tables);
            Assert.Contains("NotificationSchedules", tables);
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