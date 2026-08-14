using Finora.Shared;

namespace Finora.Application;

public sealed record CategoryInfo(Guid Id, string Name, string Icon, int SortOrder, bool IsArchived, bool IsSystem, Guid? ParentId, string? ParentName);
public sealed record TagInfo(Guid Id, string Name, string? ColorLabel, bool IsArchived, int TransactionCount);
public sealed record TagSpendSummary(Guid TagId, string TagName, long ExpenseMinor, long IncomeMinor, int TransactionCount);

public interface ICategoryTagService
{
    Task<IReadOnlyList<CategoryInfo>> GetCategoriesAsync(bool includeArchived, CancellationToken cancellationToken = default);
    Task<Result<Guid>> SaveCategoryAsync(Guid? id, string name, string icon, Guid? parentId, CancellationToken cancellationToken = default);
    Task<Result> ArchiveCategoryAsync(Guid categoryId, Guid? reassignToCategoryId, CancellationToken cancellationToken = default);
    Task<Result> RestoreCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<Result> MergeCategoryAsync(Guid sourceCategoryId, Guid targetCategoryId, CancellationToken cancellationToken = default);
    Task<Result> MoveCategoryAsync(Guid categoryId, int direction, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TagInfo>> GetTagsAsync(bool includeArchived, CancellationToken cancellationToken = default);
    Task<Result<Guid>> SaveTagAsync(Guid? id, string name, string? colorLabel, CancellationToken cancellationToken = default);
    Task<Result> ArchiveTagAsync(Guid tagId, CancellationToken cancellationToken = default);
    Task<Result> RestoreTagAsync(Guid tagId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TagSpendSummary>> GetTagReportAsync(DateTimeOffset from, DateTimeOffset through, CancellationToken cancellationToken = default);
}
