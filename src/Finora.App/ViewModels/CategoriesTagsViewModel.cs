using System.Collections.ObjectModel;
using Finora.Application;

namespace Finora.App;

public sealed class CategoriesTagsViewModel : ViewModelBase
{
    private readonly ICategoryTagService _service;
    private CategoryInfo? _selectedCategory;
    private CategoryInfo? _parentCategory;
    private CategoryInfo? _mergeTarget;
    private TagInfo? _selectedTag;
    private string _categoryName = string.Empty;
    private string _categoryIcon = "tag";
    private string _tagName = string.Empty;
    private string _tagColor = string.Empty;
    private string _status = string.Empty;

    public CategoriesTagsViewModel(ICategoryTagService service)
    {
        _service = service;
        RefreshCommand = new AsyncCommand(LoadAsync);
        NewCategoryCommand = new AsyncCommand(CreateCategoryAsync);
        UpdateCategoryCommand = new AsyncCommand(UpdateCategoryAsync);
        ArchiveCategoryCommand = new AsyncCommand(ArchiveCategoryAsync);
        RestoreCategoryCommand = new AsyncCommand(RestoreCategoryAsync);
        MoveUpCommand = new AsyncCommand(() => MoveCategoryAsync(-1));
        MoveDownCommand = new AsyncCommand(() => MoveCategoryAsync(1));
        MergeCategoryCommand = new AsyncCommand(MergeCategoryAsync);
        NewTagCommand = new AsyncCommand(CreateTagAsync);
        UpdateTagCommand = new AsyncCommand(UpdateTagAsync);
        ArchiveTagCommand = new AsyncCommand(ArchiveTagAsync);
        RestoreTagCommand = new AsyncCommand(RestoreTagAsync);
    }

    public ObservableCollection<CategoryInfo> Categories { get; } = [];
    public ObservableCollection<CategoryInfo> ActiveCategories { get; } = [];
    public ObservableCollection<TagInfo> Tags { get; } = [];
    public CategoryInfo? SelectedCategory { get => _selectedCategory; set { if (SetProperty(ref _selectedCategory, value) && value is not null) { CategoryName = value.Name; CategoryIcon = value.Icon; ParentCategory = ActiveCategories.FirstOrDefault(x => x.Id == value.ParentId); } } }
    public CategoryInfo? ParentCategory { get => _parentCategory; set => SetProperty(ref _parentCategory, value); }
    public CategoryInfo? MergeTarget { get => _mergeTarget; set => SetProperty(ref _mergeTarget, value); }
    public TagInfo? SelectedTag { get => _selectedTag; set { if (SetProperty(ref _selectedTag, value) && value is not null) { TagName = value.Name; TagColor = value.ColorLabel ?? string.Empty; } } }
    public string CategoryName { get => _categoryName; set => SetProperty(ref _categoryName, value); }
    public string CategoryIcon { get => _categoryIcon; set => SetProperty(ref _categoryIcon, value); }
    public string TagName { get => _tagName; set => SetProperty(ref _tagName, value); }
    public string TagColor { get => _tagColor; set => SetProperty(ref _tagColor, value); }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public System.Windows.Input.ICommand RefreshCommand { get; }
    public System.Windows.Input.ICommand NewCategoryCommand { get; }
    public System.Windows.Input.ICommand UpdateCategoryCommand { get; }
    public System.Windows.Input.ICommand ArchiveCategoryCommand { get; }
    public System.Windows.Input.ICommand RestoreCategoryCommand { get; }
    public System.Windows.Input.ICommand MoveUpCommand { get; }
    public System.Windows.Input.ICommand MoveDownCommand { get; }
    public System.Windows.Input.ICommand MergeCategoryCommand { get; }
    public System.Windows.Input.ICommand NewTagCommand { get; }
    public System.Windows.Input.ICommand UpdateTagCommand { get; }
    public System.Windows.Input.ICommand ArchiveTagCommand { get; }
    public System.Windows.Input.ICommand RestoreTagCommand { get; }

    public Task LoadAsync() => RunAsync(LoadCoreAsync);

    private async Task LoadCoreAsync()
    {
        var selectedCategoryId = SelectedCategory?.Id; var selectedTagId = SelectedTag?.Id;
        Categories.Clear(); ActiveCategories.Clear(); Tags.Clear();
        foreach (var category in await _service.GetCategoriesAsync(true)) { Categories.Add(category); if (!category.IsArchived) ActiveCategories.Add(category); }
        foreach (var tag in await _service.GetTagsAsync(true)) Tags.Add(tag);
        SelectedCategory = Categories.FirstOrDefault(x => x.Id == selectedCategoryId);
        SelectedTag = Tags.FirstOrDefault(x => x.Id == selectedTagId);
    }

    private Task CreateCategoryAsync() => RunAsync(async () => { var result = await _service.SaveCategoryAsync(null, CategoryName, CategoryIcon, ParentCategory?.Id); if (!result.IsSuccess) throw new InvalidOperationException(result.Error); Status = "Category created."; CategoryName = string.Empty; CategoryIcon = "tag"; ParentCategory = null; await LoadCoreAsync(); });
    private Task UpdateCategoryAsync() => RunAsync(async () => { if (SelectedCategory is null) throw new InvalidOperationException("Choose a category to update."); var result = await _service.SaveCategoryAsync(SelectedCategory.Id, CategoryName, CategoryIcon, ParentCategory?.Id); if (!result.IsSuccess) throw new InvalidOperationException(result.Error); Status = "Category updated."; await LoadCoreAsync(); });
    private Task ArchiveCategoryAsync() => RunAsync(async () => { if (SelectedCategory is null) throw new InvalidOperationException("Choose a category to archive."); var replacement = MergeTarget is { IsArchived: false } ? MergeTarget.Id : (Guid?)null; var result = await _service.ArchiveCategoryAsync(SelectedCategory.Id, replacement); if (!result.IsSuccess) throw new InvalidOperationException(result.Error); Status = "Category archived."; await LoadCoreAsync(); });
    private Task RestoreCategoryAsync() => RunAsync(async () => { if (SelectedCategory is null) throw new InvalidOperationException("Choose a category to restore."); var result = await _service.RestoreCategoryAsync(SelectedCategory.Id); if (!result.IsSuccess) throw new InvalidOperationException(result.Error); Status = "Category restored."; await LoadCoreAsync(); });
    private Task MoveCategoryAsync(int direction) => RunAsync(async () => { if (SelectedCategory is null) throw new InvalidOperationException("Choose a category to move."); var result = await _service.MoveCategoryAsync(SelectedCategory.Id, direction); if (!result.IsSuccess) throw new InvalidOperationException(result.Error); Status = direction < 0 ? "Category moved up." : "Category moved down."; await LoadCoreAsync(); });
    private Task MergeCategoryAsync() => RunAsync(async () => { if (SelectedCategory is null || MergeTarget is null) throw new InvalidOperationException("Choose source and target categories."); var result = await _service.MergeCategoryAsync(SelectedCategory.Id, MergeTarget.Id); if (!result.IsSuccess) throw new InvalidOperationException(result.Error); Status = "Category records were merged and the source category was archived."; await LoadCoreAsync(); });
    private Task CreateTagAsync() => RunAsync(async () => { var result = await _service.SaveTagAsync(null, TagName, TagColor); if (!result.IsSuccess) throw new InvalidOperationException(result.Error); Status = "Tag created."; TagName = string.Empty; TagColor = string.Empty; await LoadCoreAsync(); });
    private Task UpdateTagAsync() => RunAsync(async () => { if (SelectedTag is null) throw new InvalidOperationException("Choose a tag to update."); var result = await _service.SaveTagAsync(SelectedTag.Id, TagName, TagColor); if (!result.IsSuccess) throw new InvalidOperationException(result.Error); Status = "Tag updated."; await LoadCoreAsync(); });
    private Task ArchiveTagAsync() => RunAsync(async () => { if (SelectedTag is null) throw new InvalidOperationException("Choose a tag to archive."); var result = await _service.ArchiveTagAsync(SelectedTag.Id); if (!result.IsSuccess) throw new InvalidOperationException(result.Error); Status = "Tag archived. Existing transaction links are preserved."; await LoadCoreAsync(); });
    private Task RestoreTagAsync() => RunAsync(async () => { if (SelectedTag is null) throw new InvalidOperationException("Choose a tag to restore."); var result = await _service.RestoreTagAsync(SelectedTag.Id); if (!result.IsSuccess) throw new InvalidOperationException(result.Error); Status = "Tag restored."; await LoadCoreAsync(); });
}
