using System.Security.Cryptography;
using Finora.Domain;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Finora.Performance;

internal sealed class PerformanceSeeder(PerformanceDbFactory factory, string rootPath)
{
    private const int BatchSize = 2_000;
    private readonly PerformanceDbFactory _factory = factory;
    private readonly string _rootPath = rootPath;

    public async Task<PerformanceDatasetSummary> SeedAsync(PerformanceOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        Directory.CreateDirectory(_rootPath);
        await new DatabaseInitializer(_factory).InitializeAsync(cancellationToken).ConfigureAwait(false);

        var accounts = await SeedAccountsAsync(cancellationToken).ConfigureAwait(false);
        var categories = await LoadCategoriesAsync(cancellationToken).ConfigureAwait(false);
        var attachmentTransactionIds = await SeedTransactionsAsync(options.TransactionCount, options.AttachmentCount, accounts, categories, cancellationToken).ConfigureAwait(false);
        await SeedBudgetsAsync(options.BudgetCount, categories, cancellationToken).ConfigureAwait(false);
        await SeedGoalsAsync(options.GoalCount, cancellationToken).ConfigureAwait(false);
        await SeedRecurrencesAsync(options.RecurrenceCount, accounts, categories, cancellationToken).ConfigureAwait(false);
        var attachmentBytes = await SeedAttachmentsAsync(attachmentTransactionIds, cancellationToken).ConfigureAwait(false);

        var databasePath = Path.Combine(_rootPath, "finora-performance.db3");
        return new PerformanceDatasetSummary(
            accounts.Count,
            categories.Count,
            options.TransactionCount,
            options.AttachmentCount,
            options.RecurrenceCount,
            options.BudgetCount,
            options.GoalCount,
            File.Exists(databasePath) ? new FileInfo(databasePath).Length : 0,
            attachmentBytes);
    }

    private async Task<IReadOnlyList<Account>> SeedAccountsAsync(CancellationToken cancellationToken)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var existing = await db.Accounts.AsNoTracking().OrderBy(account => account.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (existing.Count > 0) return existing;

        var accounts = new[]
        {
            new Account { Name = "Benchmark Bank", Type = AccountType.Bank, Currency = "INR", OpeningBalanceMinor = 2_500_000 },
            new Account { Name = "Benchmark Cash", Type = AccountType.Cash, Currency = "INR", OpeningBalanceMinor = 250_000 },
            new Account { Name = "Benchmark Savings", Type = AccountType.Savings, Currency = "INR", OpeningBalanceMinor = 5_000_000 },
            new Account { Name = "Benchmark Wallet", Type = AccountType.DigitalWallet, Currency = "INR", OpeningBalanceMinor = 100_000 }
        };
        db.Accounts.AddRange(accounts);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return accounts;
    }

    private async Task<IReadOnlyList<Category>> LoadCategoriesAsync(CancellationToken cancellationToken)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Categories.AsNoTracking().Where(category => !category.IsArchived).OrderBy(category => category.SortOrder).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<Guid>> SeedTransactionsAsync(
        int transactionCount,
        int attachmentCount,
        IReadOnlyList<Account> accounts,
        IReadOnlyList<Category> categories,
        CancellationToken cancellationToken)
    {
        var attachmentTransactionIds = new List<Guid>(attachmentCount);
        var now = DateTimeOffset.UtcNow;

        for (var offset = 0; offset < transactionCount; offset += BatchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var count = Math.Min(BatchSize, transactionCount - offset);
            var batch = new List<FinanceTransaction>(count);
            for (var localIndex = 0; localIndex < count; localIndex++)
            {
                var index = offset + localIndex;
                var income = index % 10 == 0;
                var amount = 100L + (index % 50_000);
                var transaction = new FinanceTransaction
                {
                    Id = Guid.NewGuid(),
                    Type = income ? TransactionType.Income : TransactionType.Expense,
                    AmountMinor = income ? amount : -amount,
                    Currency = "INR",
                    AccountId = accounts[index % accounts.Count].Id,
                    CategoryId = income ? null : categories[index % categories.Count].Id,
                    OccurredAtUtc = now.AddMinutes(-(index * 11L)),
                    Merchant = income ? $"Employer {index % 5:D2}" : $"Merchant {index % 250:D3}",
                    Note = index % 7 == 0 ? $"Benchmark note {index % 50:D2}" : null,
                    PaymentMethod = index % 3 == 0 ? "UPI" : index % 3 == 1 ? "Card" : "Cash",
                    ManualLocation = index % 4 == 0 ? $"Benchmark Location {index % 20:D2}" : null
                };
                batch.Add(transaction);
                if (attachmentTransactionIds.Count < attachmentCount)
                    attachmentTransactionIds.Add(transaction.Id);
            }

            await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            db.Transactions.AddRange(batch);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return attachmentTransactionIds;
    }

    private async Task SeedBudgetsAsync(int budgetCount, IReadOnlyList<Category> categories, CancellationToken cancellationToken)
    {
        for (var offset = 0; offset < budgetCount; offset += BatchSize)
        {
            var count = Math.Min(BatchSize, budgetCount - offset);
            var batch = new List<Budget>(count);
            for (var localIndex = 0; localIndex < count; localIndex++)
            {
                var index = offset + localIndex;
                var overall = index % 5 == 0;
                batch.Add(new Budget
                {
                    Name = $"Benchmark Budget {index:D6}",
                    Kind = overall ? BudgetKind.Overall : BudgetKind.Category,
                    Cadence = BudgetCadence.Monthly,
                    CategoryId = overall ? null : categories[index % categories.Count].Id,
                    LimitMinor = 100_000L + (index % 10_000),
                    Currency = "INR",
                    WarningThresholdPercent = 80,
                    RolloverEnabled = index % 3 == 0
                });
            }

            await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            db.Budgets.AddRange(batch);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SeedGoalsAsync(int goalCount, CancellationToken cancellationToken)
    {
        for (var offset = 0; offset < goalCount; offset += BatchSize)
        {
            var count = Math.Min(BatchSize, goalCount - offset);
            var batch = new List<SavingsGoal>(count);
            for (var localIndex = 0; localIndex < count; localIndex++)
            {
                var index = offset + localIndex;
                batch.Add(new SavingsGoal
                {
                    Name = $"Benchmark Goal {index:D6}",
                    TargetMinor = 1_000_000L + index,
                    StartingMinor = 100_000L + (index % 50_000),
                    Currency = "INR",
                    TargetDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(365 + (index % 365))),
                    Icon = "target"
                });
            }

            await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            db.SavingsGoals.AddRange(batch);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SeedRecurrencesAsync(
        int recurrenceCount,
        IReadOnlyList<Account> accounts,
        IReadOnlyList<Category> categories,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        for (var offset = 0; offset < recurrenceCount; offset += BatchSize)
        {
            var count = Math.Min(BatchSize, recurrenceCount - offset);
            var batch = new List<RecurrenceRule>(count);
            for (var localIndex = 0; localIndex < count; localIndex++)
            {
                var index = offset + localIndex;
                batch.Add(new RecurrenceRule
                {
                    Name = $"Benchmark Recurrence {index:D6}",
                    Frequency = RecurrenceFrequency.Monthly,
                    Interval = 1,
                    DayOfMonth = (index % 28) + 1,
                    StartsOn = today.AddMonths(-12),
                    GracePeriodDays = 3,
                    ReminderMinutesBefore = 1_440,
                    Status = index % 10 == 0 ? RecurrenceStatus.Paused : RecurrenceStatus.Active,
                    TransactionType = TransactionType.Expense,
                    AmountMinor = 50_000L + (index % 10_000),
                    Currency = "INR",
                    AccountId = accounts[index % accounts.Count].Id,
                    CategoryId = categories[index % categories.Count].Id,
                    Merchant = $"Recurring Merchant {index % 100:D3}",
                    NextDueOn = today.AddDays(index % 30)
                });
            }

            await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            db.RecurrenceRules.AddRange(batch);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<long> SeedAttachmentsAsync(IReadOnlyList<Guid> transactionIds, CancellationToken cancellationToken)
    {
        if (transactionIds.Count == 0) return 0;

        var totalBytes = 0L;
        var attachmentRoot = Path.Combine(_rootPath, "attachments");
        Directory.CreateDirectory(attachmentRoot);

        for (var offset = 0; offset < transactionIds.Count; offset += BatchSize)
        {
            var count = Math.Min(BatchSize, transactionIds.Count - offset);
            var batch = new List<Attachment>(count);
            for (var localIndex = 0; localIndex < count; localIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var index = offset + localIndex;
                var transactionId = transactionIds[index];
                var attachmentId = Guid.NewGuid();
                var bytes = CreateAttachmentBytes(index);
                var relativePath = Path.Combine("attachments", transactionId.ToString("N"), $"{attachmentId:N}.png").Replace('\\', '/');
                var fullPath = Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? attachmentRoot);
                await File.WriteAllBytesAsync(fullPath, bytes, cancellationToken).ConfigureAwait(false);
                totalBytes = checked(totalBytes + bytes.Length);
                batch.Add(new Attachment
                {
                    Id = attachmentId,
                    TransactionId = transactionId,
                    RelativePath = relativePath,
                    OriginalFileName = $"benchmark-receipt-{index:D6}.png",
                    ContentType = "image/png",
                    SizeBytes = bytes.Length,
                    Sha256 = SHA256.HashData(bytes)
                });
            }

            await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            db.Attachments.AddRange(batch);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return totalBytes;
    }

    private static byte[] CreateAttachmentBytes(int index)
    {
        var bytes = new byte[1_024];
        for (var byteIndex = 0; byteIndex < bytes.Length; byteIndex++)
            bytes[byteIndex] = (byte)((index + byteIndex * 31) % 251);
        return bytes;
    }
}