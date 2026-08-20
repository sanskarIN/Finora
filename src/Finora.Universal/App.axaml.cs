using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Finora.Universal.ViewModels;
using Finora.Universal.Views;

namespace Finora.Universal;

public partial class App : Avalonia.Application
{
    public static IUniversalRuntime Runtime { get; set; } = new UnconfiguredUniversalRuntime();

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var viewModel = new MainViewModel(Runtime);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { DataContext = viewModel };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new MainView { DataContext = viewModel };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
