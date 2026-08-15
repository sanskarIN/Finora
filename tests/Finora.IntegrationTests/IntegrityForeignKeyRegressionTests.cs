using Finora.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Finora.IntegrationTests;

public sealed class IntegrityForeignKeyRegressionTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"finora-integrity-fk-{Guid.NewGuid():N}");
    private FinanceStoreTests.TestFactory _factory = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        var options = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_root, "finora.db")}")
            .Options;
        _factory = new FinanceStoreTests.TestFactory(options);
        await new DatabaseInitializer(_factory).InitializeAsync();
    }

    public Task DisposeAsync()
    {
        try { Directory.Delete(_root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task IntegrityCheck_DetectsInjectedForeignKeyViolation()
    {
        var databasePath = Path.Combine(_root, "finora.db");
        await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
        {
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = """
                PRAGMA foreign_keys = OFF;
                INSERT INTO "Attachments" (
                    "Id","CreatedAtUtc","UpdatedAtUtc","TransactionId","RelativePath","OriginalFileName","ContentType","SizeBytes","Sha256")
                VALUES (
                    '11111111-1111-1111-1111-111111111111',datetime('now'),datetime('now'),'22222222-2222-2222-2222-222222222222',
                    'attachments/22222222222222222222222222222222/11111111111111111111111111111111.pdf','orphan.pdf','application/pdf',1,NULL);
                """;
            await command.ExecuteNonQueryAsync();
        }

        var report = await new DataIntegrityService(_factory, _root).CheckAsync();

        Assert.False(report.IsHealthy);
        Assert.False(report.ForeignKeysPassed);
        Assert.Contains(report.Issues, issue =>
            issue.Code == "FOREIGN_KEY_VIOLATION" &&
            issue.Severity == Finora.Application.IntegritySeverity.Error &&
            issue.AffectedRecords >= 1);
    }
}
