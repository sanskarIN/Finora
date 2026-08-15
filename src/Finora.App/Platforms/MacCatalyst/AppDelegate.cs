using System.Diagnostics.CodeAnalysis;
using Foundation;

namespace Finora.App;

[Register("AppDelegate")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "UIKit requires the conventional AppDelegate type registered as the application delegate.")]
public sealed class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
