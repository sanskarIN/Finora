using Finora.Domain;
using Microsoft.EntityFrameworkCore;

namespace Finora.Infrastructure;

public sealed class FinoraDbContext(DbContextOptions<FinoraDbContext> options) : DbContext(options)
{
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
        modelBuilder.Entity<Attachment>().Property(x => x.OriginalFileName).HasMaxLength(240);
        modelBuilder.Entity<NotificationSchedule>().Property(x => x.Kind).HasMaxLength(64);
        modelBuilder.Entity<NotificationSchedule>().Property(x => x.Title).HasMaxLength(160);
        modelBuilder.Entity<NotificationSchedule>().Property(x => x.Body).HasMaxLength(500);
    }
}
