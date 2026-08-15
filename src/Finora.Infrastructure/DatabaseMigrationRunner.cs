using System.Data.Common;
using Finora.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Finora.Infrastructure;

public sealed class DatabaseMigrationRunner
{
    public async Task MigrateAsync(FinoraDbContext db, CancellationToken cancellationToken = default)
    {
        var versionSetting = await db.AppSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Key == "schema.version", cancellationToken).ConfigureAwait(false);
        if (versionSetting is null) throw new InvalidOperationException("The existing Finora database does not contain schema version metadata.");
        if (!int.TryParse(versionSetting.Value, out var currentVersion)) throw new InvalidDataException("The Finora database schema version is invalid.");
        if (currentVersion > AppConstants.DatabaseSchemaVersion) throw new InvalidOperationException("This Finora build cannot open a database created by a newer schema.");

        while (currentVersion < AppConstants.DatabaseSchemaVersion)
        {
            var nextVersion = currentVersion + 1;
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            switch (nextVersion)
            {
                case 2:
                    await MigrateFrom1To2Async(db, cancellationToken).ConfigureAwait(false);
                    await ValidateSchema2Async(db, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    throw new InvalidOperationException($"No database migration is registered for schema {currentVersion} to {nextVersion}.");
            }

            await ValidateDatabaseHealthAsync(db, cancellationToken).ConfigureAwait(false);

            var nextVersionText = nextVersion.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var updated = await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"AppSettings\" SET \"Value\" = {nextVersionText}, \"UpdatedAtUtc\" = CURRENT_TIMESTAMP WHERE \"Key\" = 'schema.version'",
                cancellationToken).ConfigureAwait(false);
            if (updated != 1)
                throw new InvalidDataException("Finora could not update schema version metadata atomically.");

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            currentVersion = nextVersion;
        }
    }

    private static async Task MigrateFrom1To2Async(FinoraDbContext db, CancellationToken cancellationToken)
    {
        const string sql = """
            ALTER TABLE "Attachments" ADD COLUMN "OriginalFileName" TEXT NULL;
            UPDATE "Attachments" SET "OriginalFileName" = 'receipt' WHERE "OriginalFileName" IS NULL OR TRIM("OriginalFileName") = '';

            CREATE TABLE IF NOT EXISTS "TransactionRevisions" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_TransactionRevisions" PRIMARY KEY,
                "CreatedAtUtc" TEXT NOT NULL,
                "UpdatedAtUtc" TEXT NOT NULL,
                "TransactionId" TEXT NOT NULL,
                "ChangeKind" TEXT NOT NULL,
                "SnapshotJson" TEXT NOT NULL,
                "ChangedAtUtc" TEXT NOT NULL,
                CONSTRAINT "FK_TransactionRevisions_Transactions_TransactionId" FOREIGN KEY ("TransactionId") REFERENCES "Transactions" ("Id") ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS "IX_TransactionRevisions_TransactionId_ChangedAtUtc" ON "TransactionRevisions" ("TransactionId", "ChangedAtUtc");

            CREATE TABLE IF NOT EXISTS "AccountReconciliations" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_AccountReconciliations" PRIMARY KEY,
                "CreatedAtUtc" TEXT NOT NULL,
                "UpdatedAtUtc" TEXT NOT NULL,
                "AccountId" TEXT NOT NULL,
                "StatementDateUtc" TEXT NOT NULL,
                "BookBalanceMinor" INTEGER NOT NULL,
                "StatementBalanceMinor" INTEGER NOT NULL,
                "DifferenceMinor" INTEGER NOT NULL,
                "AdjustmentCreated" INTEGER NOT NULL,
                "AdjustmentTransactionId" TEXT NULL,
                "Note" TEXT NULL,
                "CompletedAtUtc" TEXT NOT NULL,
                CONSTRAINT "FK_AccountReconciliations_Accounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES "Accounts" ("Id") ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS "IX_AccountReconciliations_AccountId_StatementDateUtc" ON "AccountReconciliations" ("AccountId", "StatementDateUtc");

            CREATE TABLE IF NOT EXISTS "NotificationSchedules" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_NotificationSchedules" PRIMARY KEY,
                "CreatedAtUtc" TEXT NOT NULL,
                "UpdatedAtUtc" TEXT NOT NULL,
                "Kind" TEXT NOT NULL,
                "Title" TEXT NOT NULL,
                "Body" TEXT NOT NULL,
                "TriggerAtUtc" TEXT NOT NULL,
                "DedupeKey" TEXT NULL,
                "IsEnabled" INTEGER NOT NULL,
                "DeliveredAtUtc" TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_NotificationSchedules_TriggerAtUtc" ON "NotificationSchedules" ("TriggerAtUtc");
            CREATE INDEX IF NOT EXISTS "IX_NotificationSchedules_DedupeKey" ON "NotificationSchedules" ("DedupeKey");
            """;
        await db.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
    }

    private static async Task ValidateSchema2Async(FinoraDbContext db, CancellationToken cancellationToken)
    {
        (string Table, string[] Columns)[] requirements =
        [
            ("Attachments", ["OriginalFileName"]),
            ("TransactionRevisions", ["Id", "CreatedAtUtc", "UpdatedAtUtc", "TransactionId", "ChangeKind", "SnapshotJson", "ChangedAtUtc"]),
            ("AccountReconciliations", ["Id", "CreatedAtUtc", "UpdatedAtUtc", "AccountId", "StatementDateUtc", "BookBalanceMinor", "StatementBalanceMinor", "DifferenceMinor", "AdjustmentCreated", "AdjustmentTransactionId", "Note", "CompletedAtUtc"]),
            ("NotificationSchedules", ["Id", "CreatedAtUtc", "UpdatedAtUtc", "Kind", "Title", "Body", "TriggerAtUtc", "DedupeKey", "IsEnabled", "DeliveredAtUtc"])
        ];

        foreach (var requirement in requirements)
        {
            var columns = await GetTableColumnsAsync(db, requirement.Table, cancellationToken).ConfigureAwait(false);
            foreach (var requiredColumn in requirement.Columns)
            {
                if (!columns.Contains(requiredColumn))
                {
                    throw new InvalidDataException($"Migration to schema 2 did not produce required column '{requirement.Table}.{requiredColumn}'.");
                }
            }
        }
    }

    private static async Task<HashSet<string>> GetTableColumnsAsync(FinoraDbContext db, string tableName, CancellationToken cancellationToken)
    {
        await using var command = CreateTransactionCommand(db, $"PRAGMA table_info(\"{tableName}\");");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            columns.Add(reader.GetString(1));
        }

        return columns;
    }

    private static async Task ValidateDatabaseHealthAsync(FinoraDbContext db, CancellationToken cancellationToken)
    {
        await using (var foreignKeyCommand = CreateTransactionCommand(db, "PRAGMA foreign_key_check;"))
        await using (var reader = await foreignKeyCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var table = reader.IsDBNull(0) ? "unknown" : reader.GetString(0);
                throw new InvalidDataException($"Database migration produced or preserved a foreign-key violation in table '{table}'.");
            }
        }

        await using var integrityCommand = CreateTransactionCommand(db, "PRAGMA integrity_check;");
        var integrityResult = await integrityCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (!string.Equals(Convert.ToString(integrityResult, System.Globalization.CultureInfo.InvariantCulture), "ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Database migration failed SQLite integrity validation.");
        }
    }

    private static DbCommand CreateTransactionCommand(FinoraDbContext db, string commandText)
    {
        var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = commandText;
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        return command;
    }
}