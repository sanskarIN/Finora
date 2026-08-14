using Finora.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Finora.Infrastructure;

public sealed class FinoraDbContext(DbContextOptions<FinoraDbContext> options) : DbContext(options)
{
    private static readonly ValueConverter<DateTimeOffset, DateTime> DateTimeOffsetConverter = new(
        value => value.UtcDateTime,
        value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));

    private static readonly ValueConverter<DateTimeOffset?, DateTime?> NullableDateTimeOffsetConverter = new(
        value => value.HasValue ? value.Value.UtcDateTime : null,
        value => value.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)) : null);

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<FinanceTransaction> Transactions => Set<FinanceTransaction>();
    public DbSet<TransactionSplit> TransactionSplits => Set<TransactionSplit>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TransactionTag> TransactionTags => Set<TransactionTag>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetPeriod> BudgetPeriods => Set<BudgetPeriod>();
    public DbSet<SavingsGoal> SavingsGoals => Set<SavingsGoal>();
    public DbSet<GoalContribution> GoalContributions => Set<GoalContribution>();
    public DbSet<RecurrenceRule> RecurrenceRules => Set<RecurrenceRule>();
    public DbSet<RecurrenceOccurrence> RecurrenceOccurrences => Set<RecurrenceOccurrence>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<TransactionRevision> TransactionRevisions => Set<TransactionRevision>();
    public DbSet<AccountReconciliation> AccountReconciliations => Set<AccountReconciliation>();
    public DbSet<NotificationSchedule> NotificationSchedules => Set<NotificationSchedule>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<BackupMetadata> BackupMetadata => Set<BackupMetadata>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateTrackedFinanceEntries();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateTrackedFinanceEntries();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>().HasIndex(x => new { x.State, x.Name });
        modelBuilder.Entity<FinanceTransaction>().HasIndex(x => new { x.AccountId, x.OccurredAtUtc });
        modelBuilder.Entity<FinanceTransaction>().HasIndex(x => new { x.IsDeleted, x.OccurredAtUtc });
        modelBuilder.Entity<FinanceTransaction>().HasIndex(x => x.TransferGroupId);
        modelBuilder.Entity<Category>().HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TransactionTag>().HasKey(x => new { x.TransactionId, x.TagId });
        modelBuilder.Entity<TransactionTag>().HasOne(x => x.Transaction).WithMany(x => x.TransactionTags).HasForeignKey(x => x.TransactionId);
        modelBuilder.Entity<TransactionTag>().HasOne(x => x.Tag).WithMany(x => x.TransactionTags).HasForeignKey(x => x.TagId);
        modelBuilder.Entity<BudgetPeriod>().HasIndex(x => new { x.BudgetId, x.StartsOn, x.EndsOn }).IsUnique();
        modelBuilder.Entity<RecurrenceOccurrence>().HasIndex(x => new { x.RecurrenceRuleId, x.DueOn }).IsUnique();
        modelBuilder.Entity<AppSetting>().HasIndex(x => x.Key).IsUnique();
        modelBuilder.Entity<BackupMetadata>().HasIndex(x => x.BackupId).IsUnique();
        modelBuilder.Entity<TransactionRevision>().HasIndex(x => new { x.TransactionId, x.ChangedAtUtc });
        modelBuilder.Entity<TransactionRevision>().HasOne(x => x.Transaction).WithMany().HasForeignKey(x => x.TransactionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AccountReconciliation>().HasIndex(x => new { x.AccountId, x.StatementDateUtc });
        modelBuilder.Entity<AccountReconciliation>().HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<NotificationSchedule>().HasIndex(x => x.TriggerAtUtc);
        modelBuilder.Entity<NotificationSchedule>().HasIndex(x => x.DedupeKey);

        modelBuilder.Entity<Account>().Property(x => x.Name).HasMaxLength(120);
        modelBuilder.Entity<Account>().Property(x => x.Currency).HasMaxLength(8);
        modelBuilder.Entity<FinanceTransaction>().Property(x => x.Currency).HasMaxLength(8);
        modelBuilder.Entity<FinanceTransaction>().Property(x => x.Merchant).HasMaxLength(240);
        modelBuilder.Entity<Category>().Property(x => x.Name).HasMaxLength(120);
        modelBuilder.Entity<Category>().Property(x => x.Icon).HasMaxLength(80);
        modelBuilder.Entity<Tag>().Property(x => x.Name).HasMaxLength(80);
        modelBuilder.Entity<Tag>().Property(x => x.ColorLabel).HasMaxLength(32);
        modelBuilder.Entity<Budget>().Property(x => x.Name).HasMaxLength(120);
        modelBuilder.Entity<Budget>().Property(x => x.Currency).HasMaxLength(8);
        modelBuilder.Entity<SavingsGoal>().Property(x => x.Name).HasMaxLength(120);
        modelBuilder.Entity<SavingsGoal>().Property(x => x.Currency).HasMaxLength(8);
        modelBuilder.Entity<SavingsGoal>().Property(x => x.Icon).HasMaxLength(80);
        modelBuilder.Entity<RecurrenceRule>().Property(x => x.Name).HasMaxLength(120);
        modelBuilder.Entity<RecurrenceRule>().Property(x => x.Currency).HasMaxLength(8);
        modelBuilder.Entity<Attachment>().Property(x => x.RelativePath).HasMaxLength(1024);
        modelBuilder.Entity<Attachment>().Property(x => x.OriginalFileName).HasMaxLength(240);
        modelBuilder.Entity<Attachment>().Property(x => x.ContentType).HasMaxLength(80);
        modelBuilder.Entity<TransactionRevision>().Property(x => x.ChangeKind).HasMaxLength(80);
        modelBuilder.Entity<NotificationSchedule>().Property(x => x.Kind).HasMaxLength(64);
        modelBuilder.Entity<NotificationSchedule>().Property(x => x.Title).HasMaxLength(160);
        modelBuilder.Entity<NotificationSchedule>().Property(x => x.Body).HasMaxLength(500);
        modelBuilder.Entity<NotificationSchedule>().Property(x => x.DedupeKey).HasMaxLength(200);
        modelBuilder.Entity<AppSetting>().Property(x => x.Key).HasMaxLength(200);
        modelBuilder.Entity<AuditEntry>().Property(x => x.EntityType).HasMaxLength(80);
        modelBuilder.Entity<AuditEntry>().Property(x => x.Action).HasMaxLength(200);
        modelBuilder.Entity<BackupMetadata>().Property(x => x.BackupId).HasMaxLength(100);

        ApplyUtcDateTimeConverters(modelBuilder);
    }

    private static void ApplyUtcDateTimeConverters(ModelBuilder modelBuilder)
    {
        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(entityType => entityType.GetProperties()))
        {
            if (property.ClrType == typeof(DateTimeOffset))
                property.SetValueConverter(DateTimeOffsetConverter);
            else if (property.ClrType == typeof(DateTimeOffset?))
                property.SetValueConverter(NullableDateTimeOffsetConverter);
        }
    }

    private void ValidateTrackedFinanceEntries()
    {
        static IEnumerable<T> Pending<T>(ChangeTracker tracker) where T : class
            => tracker.Entries<T>()
                .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
                .Select(entry => entry.Entity);

        foreach (var account in Pending<Account>(ChangeTracker))
        {
            account.Name = account.Name.Trim();
            account.Currency = account.Currency.Trim().ToUpperInvariant();
            DomainRules.ValidateAccount(account);
        }

        foreach (var transaction in Pending<FinanceTransaction>(ChangeTracker))
        {
            transaction.Currency = transaction.Currency.Trim().ToUpperInvariant();
            DomainRules.ValidateTransaction(transaction);
        }

        foreach (var split in Pending<TransactionSplit>(ChangeTracker)) DomainRules.ValidateTransactionSplit(split);
        foreach (var category in Pending<Category>(ChangeTracker)) { category.Name = category.Name.Trim(); category.Icon = category.Icon.Trim(); DomainRules.ValidateCategory(category); }
        foreach (var tag in Pending<Tag>(ChangeTracker)) { tag.Name = tag.Name.Trim(); tag.ColorLabel = string.IsNullOrWhiteSpace(tag.ColorLabel) ? null : tag.ColorLabel.Trim(); DomainRules.ValidateTag(tag); }
        foreach (var link in Pending<TransactionTag>(ChangeTracker)) DomainRules.ValidateTransactionTag(link);

        foreach (var budget in Pending<Budget>(ChangeTracker))
        {
            budget.Name = budget.Name.Trim();
            budget.Currency = budget.Currency.Trim().ToUpperInvariant();
            DomainRules.ValidateBudget(budget);
        }
        foreach (var period in Pending<BudgetPeriod>(ChangeTracker)) DomainRules.ValidateBudgetPeriod(period);

        foreach (var goal in Pending<SavingsGoal>(ChangeTracker))
        {
            goal.Name = goal.Name.Trim();
            goal.Currency = goal.Currency.Trim().ToUpperInvariant();
            goal.Icon = goal.Icon.Trim();
            DomainRules.ValidateSavingsGoal(goal);
        }
        foreach (var contribution in Pending<GoalContribution>(ChangeTracker)) DomainRules.ValidateGoalContribution(contribution);

        foreach (var rule in Pending<RecurrenceRule>(ChangeTracker))
        {
            rule.Name = rule.Name.Trim();
            rule.Currency = rule.Currency.Trim().ToUpperInvariant();
            DomainRules.ValidateRecurrenceRule(rule);
        }
        foreach (var occurrence in Pending<RecurrenceOccurrence>(ChangeTracker)) DomainRules.ValidateRecurrenceOccurrence(occurrence);
        foreach (var attachment in Pending<Attachment>(ChangeTracker)) DomainRules.ValidateAttachmentMetadata(attachment);
        foreach (var revision in Pending<TransactionRevision>(ChangeTracker)) DomainRules.ValidateTransactionRevision(revision);
        foreach (var reconciliation in Pending<AccountReconciliation>(ChangeTracker)) DomainRules.ValidateReconciliation(reconciliation);
        foreach (var notification in Pending<NotificationSchedule>(ChangeTracker)) DomainRules.ValidateNotificationSchedule(notification);
        foreach (var setting in Pending<AppSetting>(ChangeTracker)) DomainRules.ValidateAppSetting(setting);
        foreach (var audit in Pending<AuditEntry>(ChangeTracker)) DomainRules.ValidateAuditEntry(audit);
        foreach (var metadata in Pending<BackupMetadata>(ChangeTracker)) DomainRules.ValidateBackupMetadata(metadata);
    }
}
