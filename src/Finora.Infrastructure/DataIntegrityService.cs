using System.Data.Common;
using System.Security.Cryptography;
using Finora.Application;
using Microsoft.EntityFrameworkCore;

namespace Finora.Infrastructure;

public sealed class DataIntegrityService(
    IDbContextFactory<FinoraDbContext> factory,
    string appDataRoot) : IDataIntegrityService
{
    private readonly string _appDataRoot = Path.GetFullPath(appDataRoot);

    public async Task<IntegrityReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var issues = new List<IntegrityIssue>();
        await using var db = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var databaseIntegrityPassed = await CheckSqliteIntegrityAsync(db, issues, cancellationToken).ConfigureAwait(false);
        var foreignKeysPassed = await CheckForeignKeysAsync(db, issues, cancellationToken).ConfigureAwait(false);

        var accounts = await db.Accounts.AsNoTracking()
            .Select(x => new AccountRow(x.Id, x.Currency))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var accountCurrencies = accounts.ToDictionary(x => x.Id, x => x.Currency);

        var transactions = await db.Transactions.AsNoTracking()
            .Select(x => new TransactionRow(
                x.Id,
                x.AccountId,
                x.AmountMinor,
                x.Currency,
                x.TransferGroupId,
                x.CounterpartyAccountId,
                x.IsDeleted))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        CheckTransactionAccounts(transactions, accountCurrencies, issues);
        CheckTransferPairs(transactions, issues);
        await CheckSplitsAsync(db, transactions.ToDictionary(x => x.Id, x => x.AmountMinor), issues, cancellationToken).ConfigureAwait(false);
        await CheckCategoryTreeAsync(db, issues, cancellationToken).ConfigureAwait(false);

        var occurrences = await db.RecurrenceOccurrences.AsNoTracking()
            .Select(x => new OccurrenceRow(x.Id, x.RecurrenceRuleId, x.DueOn, x.GeneratedTransactionId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        CheckRecurrenceOccurrences(occurrences, transactions.Select(x => x.Id).ToHashSet(), issues);

        var attachments = await db.Attachments.AsNoTracking()
            .Select(x => new AttachmentRow(x.Id, x.RelativePath, x.SizeBytes, x.Sha256))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        await CheckAttachmentsAsync(attachments, issues, cancellationToken).ConfigureAwait(false);

        return new IntegrityReport(
            DateTimeOffset.UtcNow,
            databaseIntegrityPassed,
            foreignKeysPassed,
            accounts.Count,
            transactions.Count,
            attachments.Count,
            occurrences.Count,
            issues);
    }

    private static async Task<bool> CheckSqliteIntegrityAsync(
        FinoraDbContext db,
        ICollection<IntegrityIssue> issues,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check;";
        var failures = 0;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var value = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            if (!string.Equals(value, "ok", StringComparison.OrdinalIgnoreCase))
                failures++;
        }

        if (failures == 0)
            return true;

        issues.Add(new IntegrityIssue(
            "SQLITE_INTEGRITY",
            IntegritySeverity.Error,
            "SQLite reported an internal database integrity problem. Create a backup only after reviewing recovery options.",
            failures));
        return false;
    }

    private static async Task<bool> CheckForeignKeysAsync(
        FinoraDbContext db,
        ICollection<IntegrityIssue> issues,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_key_check;";
        var violations = 0;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            violations++;

        if (violations == 0)
            return true;

        issues.Add(new IntegrityIssue(
            "FOREIGN_KEY_VIOLATION",
            IntegritySeverity.Error,
            "One or more local records reference missing parent records.",
            violations));
        return false;
    }

    private static void CheckTransactionAccounts(
        IReadOnlyCollection<TransactionRow> transactions,
        IReadOnlyDictionary<Guid, string> accountCurrencies,
        ICollection<IntegrityIssue> issues)
    {
        var missingAccounts = 0;
        var currencyMismatches = 0;

        foreach (var transaction in transactions)
        {
            if (!accountCurrencies.TryGetValue(transaction.AccountId, out var accountCurrency))
            {
                missingAccounts++;
                continue;
            }

            if (!string.Equals(accountCurrency, transaction.Currency, StringComparison.OrdinalIgnoreCase))
                currencyMismatches++;
        }

        if (missingAccounts > 0)
            issues.Add(new IntegrityIssue("TRANSACTION_ACCOUNT_MISSING", IntegritySeverity.Error, "Transactions reference an account that is not present.", missingAccounts));
        if (currencyMismatches > 0)
            issues.Add(new IntegrityIssue("TRANSACTION_CURRENCY_MISMATCH", IntegritySeverity.Error, "Transaction currency does not match its account currency.", currencyMismatches));
    }

    private static void CheckTransferPairs(
        IReadOnlyCollection<TransactionRow> transactions,
        ICollection<IntegrityIssue> issues)
    {
        var broken = 0;
        foreach (var group in transactions.Where(x => x.TransferGroupId is not null).GroupBy(x => x.TransferGroupId!.Value))
        {
            var pair = group.ToList();
            if (pair.Count != 2)
            {
                broken++;
                continue;
            }

            var left = pair[0];
            var right = pair[1];
            var valid =
                left.AmountMinor + right.AmountMinor == 0 &&
                string.Equals(left.Currency, right.Currency, StringComparison.OrdinalIgnoreCase) &&
                left.CounterpartyAccountId == right.AccountId &&
                right.CounterpartyAccountId == left.AccountId &&
                left.IsDeleted == right.IsDeleted;
            if (!valid)
                broken++;
        }

        if (broken > 0)
            issues.Add(new IntegrityIssue("TRANSFER_PAIR_BROKEN", IntegritySeverity.Error, "Linked transfer pairs are incomplete or do not balance to zero.", broken));
    }

    private static async Task CheckSplitsAsync(
        FinoraDbContext db,
        IReadOnlyDictionary<Guid, long> transactionAmounts,
        ICollection<IntegrityIssue> issues,
        CancellationToken cancellationToken)
    {
        var splits = await db.TransactionSplits.AsNoTracking()
            .Select(x => new SplitRow(x.TransactionId, x.AmountMinor))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var invalid = 0;

        foreach (var group in splits.GroupBy(x => x.TransactionId))
        {
            if (!transactionAmounts.TryGetValue(group.Key, out var amount))
            {
                invalid++;
                continue;
            }

            long total;
            try
            {
                total = group.Aggregate(0L, (sum, row) => checked(sum + row.AmountMinor));
            }
            catch (OverflowException)
            {
                invalid++;
                continue;
            }

            if (total != amount)
                invalid++;
        }

        if (invalid > 0)
            issues.Add(new IntegrityIssue("TRANSACTION_SPLIT_TOTAL", IntegritySeverity.Error, "One or more split transactions do not add up to the parent transaction amount.", invalid));
    }

    private static async Task CheckCategoryTreeAsync(
        FinoraDbContext db,
        ICollection<IntegrityIssue> issues,
        CancellationToken cancellationToken)
    {
        var categories = await db.Categories.AsNoTracking()
            .Select(x => new CategoryRow(x.Id, x.ParentId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var parentById = categories.ToDictionary(x => x.Id, x => x.ParentId);
        var cyclic = new HashSet<Guid>();

        foreach (var category in categories)
        {
            var path = new HashSet<Guid>();
            Guid? current = category.Id;
            while (current is Guid id)
            {
                if (!path.Add(id))
                {
                    foreach (var member in path)
                        cyclic.Add(member);
                    break;
                }

                if (!parentById.TryGetValue(id, out current))
                    break;
            }
        }

        if (cyclic.Count > 0)
            issues.Add(new IntegrityIssue("CATEGORY_CYCLE", IntegritySeverity.Error, "The category hierarchy contains a parent/child cycle.", cyclic.Count));
    }

    private static void CheckRecurrenceOccurrences(
        IReadOnlyCollection<OccurrenceRow> occurrences,
        IReadOnlySet<Guid> transactionIds,
        ICollection<IntegrityIssue> issues)
    {
        var duplicateCount = occurrences
            .GroupBy(x => (x.RecurrenceRuleId, x.DueOn))
            .Count(group => group.Count() > 1);
        var missingGeneratedTransactions = occurrences.Count(x => x.GeneratedTransactionId is Guid id && !transactionIds.Contains(id));

        if (duplicateCount > 0)
            issues.Add(new IntegrityIssue("RECURRENCE_DUPLICATE", IntegritySeverity.Error, "Duplicate recurrence occurrences exist for the same rule and due date.", duplicateCount));
        if (missingGeneratedTransactions > 0)
            issues.Add(new IntegrityIssue("RECURRENCE_TRANSACTION_MISSING", IntegritySeverity.Error, "Recurring occurrences reference a generated transaction that is missing.", missingGeneratedTransactions));
    }

    private async Task CheckAttachmentsAsync(
        IReadOnlyCollection<AttachmentRow> attachments,
        ICollection<IntegrityIssue> issues,
        CancellationToken cancellationToken)
    {
        var unsafePaths = 0;
        var missingFiles = 0;
        var sizeMismatches = 0;
        var hashMismatches = 0;

        foreach (var attachment in attachments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string fullPath;
            try
            {
                fullPath = ResolveSafePath(attachment.RelativePath);
            }
            catch (InvalidDataException)
            {
                unsafePaths++;
                continue;
            }

            if (!File.Exists(fullPath))
            {
                missingFiles++;
                continue;
            }

            var info = new FileInfo(fullPath);
            if (info.Length != attachment.SizeBytes)
            {
                sizeMismatches++;
                continue;
            }

            if (attachment.Sha256 is null || attachment.Sha256.Length == 0)
                continue;

            await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var actualHash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
            if (!CryptographicOperations.FixedTimeEquals(actualHash, attachment.Sha256))
                hashMismatches++;
        }

        if (unsafePaths > 0)
            issues.Add(new IntegrityIssue("ATTACHMENT_PATH_UNSAFE", IntegritySeverity.Error, "Attachment metadata contains a path outside Finora private storage.", unsafePaths));
        if (missingFiles > 0)
            issues.Add(new IntegrityIssue("ATTACHMENT_FILE_MISSING", IntegritySeverity.Error, "Attachment metadata exists but the local receipt file is missing.", missingFiles));
        if (sizeMismatches > 0)
            issues.Add(new IntegrityIssue("ATTACHMENT_SIZE_MISMATCH", IntegritySeverity.Error, "Attachment file size no longer matches stored metadata.", sizeMismatches));
        if (hashMismatches > 0)
            issues.Add(new IntegrityIssue("ATTACHMENT_HASH_MISMATCH", IntegritySeverity.Error, "Attachment file checksum no longer matches stored metadata.", hashMismatches));
    }

    private string ResolveSafePath(string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_appDataRoot, normalized));
        var allowedRoot = _appDataRoot.EndsWith(Path.DirectorySeparatorChar)
            ? _appDataRoot
            : _appDataRoot + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Attachment path escaped app-private storage.");
        return fullPath;
    }

    private static async Task EnsureOpenAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
    }

    private sealed record AccountRow(Guid Id, string Currency);
    private sealed record TransactionRow(Guid Id, Guid AccountId, long AmountMinor, string Currency, Guid? TransferGroupId, Guid? CounterpartyAccountId, bool IsDeleted);
    private sealed record SplitRow(Guid TransactionId, long AmountMinor);
    private sealed record CategoryRow(Guid Id, Guid? ParentId);
    private sealed record OccurrenceRow(Guid Id, Guid RecurrenceRuleId, DateOnly DueOn, Guid? GeneratedTransactionId);
    private sealed record AttachmentRow(Guid Id, string RelativePath, long SizeBytes, byte[]? Sha256);
}
