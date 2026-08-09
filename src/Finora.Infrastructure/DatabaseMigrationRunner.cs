using Finora.Shared;
using Microsoft.EntityFrameworkCore;

namespace Finora.Infrastructure;

public sealed class DatabaseMigrationRunner
{
    public async Task MigrateAsync(FinoraDbContext db, CancellationToken cancellationToken = default)
    {
        var versionSetting = await db.AppSettings.SingleOrDefaultAsync(x => x.Key == "schema.version", cancellationToken).ConfigureAwait(false);
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
                    break;
                default:
                    throw new InvalidOperationException($"No database migration is registered for schema {currentVersion} to {nextVersion}.");
            }

            versionSetting.Value = nextVersion.ToString(System.Globalization.CultureInfo.InvariantCulture);
            versionSetting.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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
}
