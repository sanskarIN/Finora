using Finora.Application;
using Finora.Domain;
using Finora.Shared;
using Microsoft.EntityFrameworkCore;

namespace Finora.Infrastructure;

public sealed class CategoryTagService(IDbContextFactory<FinoraDbContext> factory) : ICategoryTagService
{
    private readonly IDbContextFactory<FinoraDbContext> _factory = factory;

    public async Task<IReadOnlyList<CategoryInfo>> GetCategoriesAsync(bool includeArchived, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var query = db.Categories.AsNoTracking().Include(x => x.Parent).AsQueryable();
        if (!includeArchived) query = query.Where(x => !x.IsArchived);
        return await query.OrderBy(x => x.ParentId).ThenBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new CategoryInfo(x.Id, x.Name, x.Icon, x.SortOrder, x.IsArchived, x.IsSystem, x.ParentId, x.Parent != null ? x.Parent.Name : null))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<Guid>> SaveCategoryAsync(Guid? id, string name, string icon, Guid? parentId, CancellationToken cancellationToken = default)
    {
        name = name?.Trim() ?? string.Empty;
        icon = string.IsNullOrWhiteSpace(icon) ? "tag" : icon.Trim();
        if (name.Length is < 1 or > 120) return Result<Guid>.Failure("Category name must contain 1–120 characters.");
        if (parentId == id && id is not null) return Result<Guid>.Failure("A category cannot be its own parent.");
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        if (parentId is Guid parent && !await db.Categories.AnyAsync(x => x.Id == parent && !x.IsArchived, cancellationToken).ConfigureAwait(false))
            return Result<Guid>.Failure("The selected parent category is unavailable.");

        if (id is Guid existingId && parentId is Guid proposedParent)
        {
            var all = await db.Categories.AsNoTracking().Select(x => new { x.Id, x.ParentId }).ToListAsync(cancellationToken).ConfigureAwait(false);
            if (IsDescendant(all.ToDictionary(x => x.Id, x => x.ParentId), proposedParent, existingId))
                return Result<Guid>.Failure("A category cannot be moved beneath one of its own descendants.");
        }

        var duplicate = await db.Categories.AnyAsync(x => x.Id != id && x.ParentId == parentId && x.Name.ToUpper() == name.ToUpper(), cancellationToken).ConfigureAwait(false);
        if (duplicate) return Result<Guid>.Failure("A category with that name already exists at this level.");

        Category category;
        if (id is Guid categoryId)
        {
            category = await db.Categories.SingleOrDefaultAsync(x => x.Id == categoryId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Category not found.");
            category.Name = name;
            category.Icon = icon;
            category.ParentId = parentId;
            category.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }
        else
        {
            var maxOrder = await db.Categories.Where(x => x.ParentId == parentId)
                .Select(x => (int?)x.SortOrder)
                .MaxAsync(cancellationToken)
                .ConfigureAwait(false);
            var nextOrder = checked((maxOrder ?? -1) + 1);
            category = new Category { Name = name, Icon = icon, ParentId = parentId, SortOrder = nextOrder };
            db.Categories.Add(category);
        }

        db.AuditEntries.Add(new AuditEntry { EntityType = "Category", EntityId = category.Id, Action = id is null ? "Created" : "Updated" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<Guid>.Success(category.Id);
    }

    public async Task<Result> ArchiveCategoryAsync(Guid categoryId, Guid? reassignToCategoryId, CancellationToken cancellationToken = default)
    {
        if (reassignToCategoryId == categoryId) return Result.Failure("Choose a different reassignment category.");
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var categories = await db.Categories.ToListAsync(cancellationToken).ConfigureAwait(false);
        var category = categories.SingleOrDefault(x => x.Id == categoryId);
        if (category is null) return Result.Failure("Category not found.");
        if (category.IsArchived) return Result.Success();
        if (categories.Any(x => x.ParentId == categoryId && !x.IsArchived))
            return Result.Failure("Archive or move this category's active subcategories first.");

        var inUse = await db.Transactions.AnyAsync(x => x.CategoryId == categoryId, cancellationToken).ConfigureAwait(false)
            || await db.TransactionSplits.AnyAsync(x => x.CategoryId == categoryId, cancellationToken).ConfigureAwait(false)
            || await db.Budgets.AnyAsync(x => x.CategoryId == categoryId, cancellationToken).ConfigureAwait(false)
            || await db.RecurrenceRules.AnyAsync(x => x.CategoryId == categoryId, cancellationToken).ConfigureAwait(false);

        Category? replacement = null;
        if (reassignToCategoryId is Guid replacementId)
            replacement = categories.SingleOrDefault(x => x.Id == replacementId && !x.IsArchived);
        if (inUse && replacement is null)
            return Result.Failure("This category is in use. Choose another category to reassign its records before archiving.");

        if (replacement is not null)
        {
            var parentMap = categories.ToDictionary(x => x.Id, x => x.ParentId);
            if (IsDescendant(parentMap, replacement.Id, category.Id))
                return Result.Failure("Cannot reassign a category into one of its descendants.");
            var hasSubcategoryBudget = await db.Budgets.AnyAsync(x => x.CategoryId == categoryId && x.Kind == BudgetKind.Subcategory, cancellationToken).ConfigureAwait(false);
            if (hasSubcategoryBudget && replacement.ParentId is null)
                return Result.Failure("A subcategory budget uses this category. Reassign it to another subcategory before archiving.");

            await db.Transactions.Where(x => x.CategoryId == categoryId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.CategoryId, replacement.Id), cancellationToken)
                .ConfigureAwait(false);
            await db.TransactionSplits.Where(x => x.CategoryId == categoryId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.CategoryId, replacement.Id), cancellationToken)
                .ConfigureAwait(false);
            await db.Budgets.Where(x => x.CategoryId == categoryId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.CategoryId, replacement.Id), cancellationToken)
                .ConfigureAwait(false);
            await db.RecurrenceRules.Where(x => x.CategoryId == categoryId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.CategoryId, replacement.Id), cancellationToken)
                .ConfigureAwait(false);
        }

        category.IsArchived = true;
        category.UpdatedAtUtc = DateTimeOffset.UtcNow;
        db.AuditEntries.Add(new AuditEntry { EntityType = "Category", EntityId = category.Id, Action = replacement is null ? "Archived" : $"ArchivedAndReassigned:{replacement.Id}" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> RestoreCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var category = await db.Categories.SingleOrDefaultAsync(x => x.Id == categoryId, cancellationToken).ConfigureAwait(false);
        if (category is null) return Result.Failure("Category not found.");
        if (!category.IsArchived) return Result.Success();
        if (category.ParentId is Guid parent && await db.Categories.AnyAsync(x => x.Id == parent && x.IsArchived, cancellationToken).ConfigureAwait(false))
            return Result.Failure("Restore the parent category first.");
        category.IsArchived = false;
        category.UpdatedAtUtc = DateTimeOffset.UtcNow;
        db.AuditEntries.Add(new AuditEntry { EntityType = "Category", EntityId = category.Id, Action = "Restored" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> MergeCategoryAsync(Guid sourceCategoryId, Guid targetCategoryId, CancellationToken cancellationToken = default)
    {
        if (sourceCategoryId == targetCategoryId) return Result.Failure("Choose two different categories.");
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var categories = await db.Categories.ToListAsync(cancellationToken).ConfigureAwait(false);
        var source = categories.SingleOrDefault(x => x.Id == sourceCategoryId);
        var target = categories.SingleOrDefault(x => x.Id == targetCategoryId && !x.IsArchived);
        if (source is null || target is null) return Result.Failure("Source or target category is unavailable.");
        if (source.IsArchived) return Result.Failure("Restore the source category before merging it.");

        var parentMap = categories.ToDictionary(x => x.Id, x => x.ParentId);
        if (IsDescendant(parentMap, target.Id, source.Id))
            return Result.Failure("Cannot merge a category into one of its descendants.");
        var hasSubcategoryBudget = await db.Budgets.AnyAsync(x => x.CategoryId == source.Id && x.Kind == BudgetKind.Subcategory, cancellationToken).ConfigureAwait(false);
        if (hasSubcategoryBudget && target.ParentId is null)
            return Result.Failure("A subcategory budget uses the source category. Merge into another subcategory or reassign that budget first.");

        await db.Transactions.Where(x => x.CategoryId == source.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.CategoryId, target.Id), cancellationToken)
            .ConfigureAwait(false);
        await db.TransactionSplits.Where(x => x.CategoryId == source.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.CategoryId, target.Id), cancellationToken)
            .ConfigureAwait(false);
        await db.Budgets.Where(x => x.CategoryId == source.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.CategoryId, target.Id), cancellationToken)
            .ConfigureAwait(false);
        await db.RecurrenceRules.Where(x => x.CategoryId == source.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.CategoryId, target.Id), cancellationToken)
            .ConfigureAwait(false);

        foreach (var child in categories.Where(x => x.ParentId == source.Id)) child.ParentId = target.Id;
        source.IsArchived = true;
        source.UpdatedAtUtc = DateTimeOffset.UtcNow;
        db.AuditEntries.Add(new AuditEntry { EntityType = "Category", EntityId = source.Id, Action = $"MergedInto:{target.Id}" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> MoveCategoryAsync(Guid categoryId, int direction, CancellationToken cancellationToken = default)
    {
        if (direction is not (-1 or 1)) return Result.Failure("Direction must be -1 or 1.");
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var category = await db.Categories.SingleOrDefaultAsync(x => x.Id == categoryId, cancellationToken).ConfigureAwait(false);
        if (category is null) return Result.Failure("Category not found.");
        var siblings = await db.Categories
            .Where(x => x.ParentId == category.ParentId && x.IsArchived == category.IsArchived)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var index = siblings.FindIndex(x => x.Id == category.Id);
        var targetIndex = index + direction;
        if (index < 0 || targetIndex < 0 || targetIndex >= siblings.Count) return Result.Success();
        var other = siblings[targetIndex];
        (category.SortOrder, other.SortOrder) = (other.SortOrder, category.SortOrder);
        if (category.SortOrder == other.SortOrder)
            for (var i = 0; i < siblings.Count; i++) siblings[i].SortOrder = i;
        category.UpdatedAtUtc = other.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<IReadOnlyList<TagInfo>> GetTagsAsync(bool includeArchived, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var query = db.Tags.AsNoTracking().AsQueryable();
        if (!includeArchived) query = query.Where(x => !x.IsArchived);
        return await query.OrderBy(x => x.Name)
            .Select(x => new TagInfo(x.Id, x.Name, x.ColorLabel, x.IsArchived, x.TransactionTags.Count))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<Guid>> SaveTagAsync(Guid? id, string name, string? colorLabel, CancellationToken cancellationToken = default)
    {
        name = name?.Trim() ?? string.Empty;
        if (name.Length is < 1 or > 80) return Result<Guid>.Failure("Tag name must contain 1–80 characters.");
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        if (await db.Tags.AnyAsync(x => x.Id != id && x.Name.ToUpper() == name.ToUpper(), cancellationToken).ConfigureAwait(false))
            return Result<Guid>.Failure("A tag with that name already exists.");

        Tag tag;
        if (id is Guid tagId)
        {
            tag = await db.Tags.SingleOrDefaultAsync(x => x.Id == tagId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Tag not found.");
            tag.Name = name;
            tag.ColorLabel = NormalizeColor(colorLabel);
            tag.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }
        else
        {
            tag = new Tag { Name = name, ColorLabel = NormalizeColor(colorLabel) };
            db.Tags.Add(tag);
        }

        db.AuditEntries.Add(new AuditEntry { EntityType = "Tag", EntityId = tag.Id, Action = id is null ? "Created" : "Updated" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<Guid>.Success(tag.Id);
    }

    public Task<Result> ArchiveTagAsync(Guid tagId, CancellationToken cancellationToken = default)
        => SetTagArchivedAsync(tagId, true, cancellationToken);

    public Task<Result> RestoreTagAsync(Guid tagId, CancellationToken cancellationToken = default)
        => SetTagArchivedAsync(tagId, false, cancellationToken);

    public async Task<IReadOnlyList<TagSpendSummary>> GetTagReportAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        if (to <= from) throw new ArgumentException("Tag report end must be after its start.");
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rows = await db.TransactionTags.AsNoTracking()
            .Where(link => link.Transaction != null && !link.Transaction.IsDeleted && link.Transaction.OccurredAtUtc >= from && link.Transaction.OccurredAtUtc < to)
            .Select(link => new TagReportRow(link.TagId, link.Tag!.Name, link.Transaction!.AmountMinor, link.Transaction.Type))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var result = new List<TagSpendSummary>();

        foreach (var group in rows.GroupBy(x => new { x.TagId, x.TagName }))
        {
            long expense = 0;
            long income = 0;
            foreach (var row in group.Where(x => x.Type != TransactionType.Transfer))
            {
                if (row.AmountMinor == long.MinValue)
                    throw new InvalidDataException("Stored tagged transaction amount is outside the supported range.");
                if (row.AmountMinor < 0) expense = checked(expense - row.AmountMinor);
                else if (row.AmountMinor > 0) income = checked(income + row.AmountMinor);
            }
            result.Add(new TagSpendSummary(group.Key.TagId, group.Key.TagName, expense, income, group.Count()));
        }

        return result.OrderByDescending(x => x.ExpenseMinor).ThenBy(x => x.TagName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task<Result> SetTagArchivedAsync(Guid tagId, bool archived, CancellationToken cancellationToken)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var tag = await db.Tags.SingleOrDefaultAsync(x => x.Id == tagId, cancellationToken).ConfigureAwait(false);
        if (tag is null) return Result.Failure("Tag not found.");
        if (tag.IsArchived == archived) return Result.Success();
        tag.IsArchived = archived;
        tag.UpdatedAtUtc = DateTimeOffset.UtcNow;
        db.AuditEntries.Add(new AuditEntry { EntityType = "Tag", EntityId = tag.Id, Action = archived ? "Archived" : "Restored" });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static bool IsDescendant(IReadOnlyDictionary<Guid, Guid?> parentMap, Guid candidate, Guid ancestor)
    {
        var current = candidate;
        var visited = new HashSet<Guid>();
        while (visited.Add(current) && parentMap.TryGetValue(current, out var parent) && parent is Guid parentId)
        {
            if (parentId == ancestor) return true;
            current = parentId;
        }
        return false;
    }

    private static string? NormalizeColor(string? colorLabel)
    {
        if (string.IsNullOrWhiteSpace(colorLabel)) return null;
        var value = colorLabel.Trim();
        return value.Length <= 32 ? value : value[..32];
    }

    private sealed record TagReportRow(Guid TagId, string TagName, long AmountMinor, TransactionType Type);
}
