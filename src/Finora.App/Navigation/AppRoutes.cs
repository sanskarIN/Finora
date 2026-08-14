using Microsoft.Maui.Devices;

namespace Finora.App;

public static class AppRoutes
{
    public const string MobileDashboardRoot = "//dashboard";
    public const string DesktopDashboardRoot = "//dashboard-desktop";

    public static bool UseDesktopNavigation
    {
        get
        {
            var idiom = DeviceInfo.Idiom;
            if (idiom == DeviceIdiom.Desktop || idiom == DeviceIdiom.Tablet)
                return true;

            var width = Shell.Current?.Width;
            if (width is null or <= 0)
            {
                var windows = Microsoft.Maui.Controls.Application.Current?.Windows;
                width = windows is { Count: > 0 } ? windows[0].Width : 0;
            }
            return width >= 900;
        }
    }

    public static string DashboardRoot => UseDesktopNavigation ? DesktopDashboardRoot : MobileDashboardRoot;
}
