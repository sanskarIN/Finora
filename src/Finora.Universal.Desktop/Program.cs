using Avalonia;
using Finora.Universal;

namespace Finora.Universal.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.Runtime = new DesktopUniversalRuntime();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
