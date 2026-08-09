using Finora.Application;

namespace Finora.App;

public partial class ReconciliationPage : ContentPage
{
    private ReconciliationViewModel ViewModel => (ReconciliationViewModel)BindingContext;
    public ReconciliationPage() { InitializeComponent(); BindingContext = new ReconciliationViewModel(ServiceHelper.Get<IFinanceStore>(), ServiceHelper.Get<IReconciliationService>()); }
    protected override void OnAppearing() { base.OnAppearing(); _ = ViewModel.LoadAsync(); }
}
