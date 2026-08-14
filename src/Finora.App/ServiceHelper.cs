using Microsoft.Extensions.DependencyInjection;

namespace Finora.App;

internal static class ServiceHelper
{
    public static T Get<T>() where T : notnull
    {
        var application = IPlatformApplication.Current
            ?? throw new InvalidOperationException("Finora services are unavailable.");
        return application.Services.GetRequiredService<T>();
    }
}
