namespace Finora.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(AccountsPage), typeof(AccountsPage));
        Routing.RegisterRoute(nameof(ReportsPage), typeof(ReportsPage));
        Routing.RegisterRoute(nameof(RecurringPage), typeof(RecurringPage));
        Routing.RegisterRoute(nameof(ImportPage), typeof(ImportPage));
        Routing.RegisterRoute(nameof(TransactionToolsPage), typeof(TransactionToolsPage));
        Routing.RegisterRoute(nameof(TransactionDetailPage), typeof(TransactionDetailPage));
        Routing.RegisterRoute(nameof(CategoriesTagsPage), typeof(CategoriesTagsPage));
        Routing.RegisterRoute(nameof(ReconciliationPage), typeof(ReconciliationPage));
        Routing.RegisterRoute(nameof(AccountDetailPage), typeof(AccountDetailPage));
        Routing.RegisterRoute(nameof(LegalPage), typeof(LegalPage));
    }
}
