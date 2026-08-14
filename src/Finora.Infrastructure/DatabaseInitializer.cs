using Finora.Domain;
using Finora.Shared;
using Microsoft.EntityFrameworkCore;

namespace Finora.Infrastructure;

public sealed class DatabaseInitializer(IDbContextFactory<FinoraDbContext> factory)
{
    private readonly IDbContextFactory<FinoraDbContext> _factory = factory;
    private readonly DatabaseMigrationRunner _migrationRunner = new();

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var created = await db.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken).ConfigureAwait(false);
        await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON;", cancellationToken).ConfigureAwait(false);
        await db.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;", cancellationToken).ConfigureAwait(false);

        if (created)
        {
            db.AppSettings.Add(new AppSetting { Key = "schema.version", Value = AppConstants.DatabaseSchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture) });
        }
        else
        {
            await _migrationRunner.MigrateAsync(db, cancellationToken).ConfigureAwait(false);
        }

        if (!await db.Categories.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            string[] names = ["Food", "Transport", "Housing", "Utilities", "Health", "Education", "Shopping", "Entertainment", "Travel", "Salary", "Other"];
            db.Categories.AddRange(names.Select((name, index) => new Category { Name = name, SortOrder = index, IsSystem = true, Icon = name.ToLowerInvariant() }));
        }

        await RepairDerivedGoalStateAsync(db, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task RepairDerivedGoalStateAsync(FinoraDbContext db, CancellationToken cancellationToken)
    {
        var goals = await db.SavingsGoals
            .Include(goal => goal.Contributions)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var goal in goals)
        {
            try
            {
                DomainRules.ValidateSavingsGoal(goal);
                var current = goal.StartingMinor;
                var historyValid = true;
                foreach (var contribution in goal.Contributions
                             .OrderBy(item => item.OccurredAtUtc)
                             .ThenBy(item => item.CreatedAtUtc))
                {
                    DomainRules.ValidateGoalContribution(contribution);
                    current = checked(current + contribution.AmountMinor);
                    if (current < 0)
                    {
                        historyValid = false;
                        break;
                    }
                }

                if (!historyValid) continue;
                var completed = current >= goal.TargetMinor;
                if (goal.IsCompleted == completed) continue;
                goal.IsCompleted = completed;
                goal.UpdatedAtUtc = DateTimeOffset.UtcNow;
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or OverflowException)
            {
                // Corrupt financial history belongs to the integrity checker; startup must not mask it.
            }
        }
    }
}
