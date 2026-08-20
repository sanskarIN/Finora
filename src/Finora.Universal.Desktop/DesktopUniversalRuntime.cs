using Finora.Application;
using Finora.Infrastructure;
using Finora.Shared;
using Finora.Universal;
using Microsoft.EntityFrameworkCore;

namespace Finora.Universal.Desktop;

internal sealed class DesktopUniversalRuntime : IUniversalRuntime
{
    public async Task<UniversalRuntimeState> InitializeAsync(CancellationToken cancellationToken = default)
    {
        var dataDirectory = GetDataDirectory();
        Directory.CreateDirectory(dataDirectory);
        var databasePath = Path.Combine(dataDirectory, AppConstants.DatabaseFileName);

        var options = new DbContextOptionsBuilder<FinoraDbContext>()
            .UseSqlite($"Data Source={databasePath};Cache=Shared")
            .Options;
        var factory = new DesktopDbContextFactory(options);
        var initializer = new DatabaseInitializer(factory);
        IFinanceStore store = new FinanceStore(factory, initializer);

        await store.InitializeAsync(cancellationToken).ConfigureAwait(false);
        var accounts = await store.GetAccountsAsync(cancellationToken).ConfigureAwait(false);

        return new UniversalRuntimeState(
            GetPlatformName(),
            true,
            "Private SQLite data is stored in the operating system's local application-data directory.",
            accounts.Count,
            "Native desktop storage is initialized. Finora finance data remains local to this device unless the user explicitly exports or backs it up.");
    }

    private static string GetPlatformName()
    {
        if (OperatingSystem.IsLinux()) return "Linux desktop";
        if (OperatingSystem.IsWindows()) return "Windows desktop (Avalonia host)";
        if (OperatingSystem.IsMacOS()) return "macOS desktop (Avalonia host)";
        return "Desktop";
    }

    private static string GetDataDirectory()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(local))
            local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".finora");

        return Path.Combine(local, AppConstants.ProductName);
    }

    private sealed class DesktopDbContextFactory(DbContextOptions<FinoraDbContext> options) : IDbContextFactory<FinoraDbContext>
    {
        public FinoraDbContext CreateDbContext() => new(options);
    }
}
