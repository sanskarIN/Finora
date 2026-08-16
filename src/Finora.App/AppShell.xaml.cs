using Microsoft.Maui.Devices;

namespace Finora.App;

public partial class AppShell : Shell
{
    private bool _hasMeasuredNavigation;
    private bool _usingDesktopNavigation;

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

        ApplyAdaptiveNavigation(navigateEquivalentSection: false);
        SizeChanged += OnShellSizeChanged;
    }

    private void OnShellSizeChanged(object? sender, EventArgs e)
    {
        var desktop = ShouldUseDesktopNavigation();
        if (!_hasMeasuredNavigation)
        {
            _hasMeasuredNavigation = true;
            _usingDesktopNavigation = desktop;
            ApplyAdaptiveNavigation(navigateEquivalentSection: false);
            return;
        }

        if (desktop == _usingDesktopNavigation) return;
        _usingDesktopNavigation = desktop;
        ApplyAdaptiveNavigation(navigateEquivalentSection: true);
    }

    private void ApplyAdaptiveNavigation(bool navigateEquivalentSection)
    {
        var desktop = ShouldUseDesktopNavigation();
        _usingDesktopNavigation = desktop;

        MobileTabBar.IsVisible = !desktop;
        DesktopDashboard.IsVisible = desktop;
        DesktopTransactions.IsVisible = desktop;
        DesktopBudgets.IsVisible = desktop;
        DesktopGoals.IsVisible = desktop;
        DesktopSettings.IsVisible = desktop;
        FlyoutBehavior = desktop
            ? Microsoft.Maui.FlyoutBehavior.Flyout
            : Microsoft.Maui.FlyoutBehavior.Disabled;

        if (!navigateEquivalentSection) return;

        var current = CurrentState?.Location?.OriginalString ?? string.Empty;
        if (current.Contains("onboarding", StringComparison.OrdinalIgnoreCase) ||
            current.Contains("lock", StringComparison.OrdinalIgnoreCase))
            return;

        var route = ResolveEquivalentRoot(current, desktop);
        _ = GoToAsync(route);
    }

    private bool ShouldUseDesktopNavigation()
    {
        var idiom = DeviceInfo.Idiom;
        if (idiom == DeviceIdiom.Desktop || idiom == DeviceIdiom.Tablet) return true;
        return Width >= 900;
    }

    private static string ResolveEquivalentRoot(string location, bool desktop)
    {
        if (location.Contains("transactions", StringComparison.OrdinalIgnoreCase))
            return desktop ? "//transactions-desktop" : "//transactions";
        if (location.Contains("budgets", StringComparison.OrdinalIgnoreCase))
            return desktop ? "//budgets-desktop" : "//budgets";
        if (location.Contains("goals", StringComparison.OrdinalIgnoreCase))
            return desktop ? "//goals-desktop" : "//goals";
        if (location.Contains("settings", StringComparison.OrdinalIgnoreCase))
            return desktop ? "//settings-desktop" : "//settings";
        return desktop ? AppRoutes.DesktopDashboardRoot : AppRoutes.MobileDashboardRoot;
    }

    private async void OnShellBuyMeACoffeeTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            var opened = await Launcher.Default.OpenAsync(new Uri(Finora.Shared.AppConstants.BuyMeACoffeeUrl));
            if (!opened)
                await DisplayAlertAsync("Support Finora", "The Buy Me a Coffee support page could not be opened with the available browser or system handler.", "OK");
        }
        catch (Exception)
        {
            await DisplayAlertAsync("Support Finora", "The Buy Me a Coffee support page could not be opened right now.", "OK");
        }
    }
}
