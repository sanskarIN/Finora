namespace Finora.App;

public static class AppRoutes
{
    public const string MobileDashboardRoot = "//dashboard";
    public const string DesktopDashboardRoot = "//dashboard-desktop";

    public static bool UseDesktopNavigation
    {
        get
        {
            if (DeviceInfo.Idiom is DeviceIdiom.Desktop or DeviceIdiom.Tablet)
                return true;

            var width = Shell.Current?.Width ?? Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Width ?? 0;
            return width >= 900;
        }
    }

    public static string DashboardRoot => UseDesktopNavigation ? DesktopDashboardRoot : MobileDashboardRoot;
}
