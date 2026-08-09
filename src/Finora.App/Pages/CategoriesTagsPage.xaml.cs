using Finora.Application;

namespace Finora.App;

public partial class CategoriesTagsPage : ContentPage
{
    private CategoriesTagsViewModel ViewModel => (CategoriesTagsViewModel)BindingContext;
    public CategoriesTagsPage() { InitializeComponent(); BindingContext = new CategoriesTagsViewModel(ServiceHelper.Get<ICategoryTagService>()); }
    protected override void OnAppearing() { base.OnAppearing(); _ = ViewModel.LoadAsync(); }
}
