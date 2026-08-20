using Avalonia;
using Avalonia.Browser;
using Finora.Universal;

namespace Finora.Universal.Browser;

internal static partial class Program
{
    private static Task Main(string[] args)
    {
        App.Runtime = new BrowserUniversalRuntime();
        return BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>();
}
