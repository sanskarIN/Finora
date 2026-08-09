using Finora.Application;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace Finora.App;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder=MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        var dbPath=Path.Combine(FileSystem.AppDataDirectory,Finora.Shared.AppConstants.DatabaseFileName);
        builder.Services.AddPooledDbContextFactory<FinoraDbContext>(o=>o.UseSqlite($"Data Source={dbPath};Cache=Shared"));
        builder.Services.AddSingleton<DatabaseInitializer>();
        builder.Services.AddSingleton<IFinanceStore,FinanceStore>();
        builder.Services.AddSingleton<IBackupService,BackupService>();
        builder.Services.AddSingleton<IExportService,ExportService>();
        builder.Services.AddSingleton<IPrivacyLogger>(_=>new PrivacyLogger(FileSystem.CacheDirectory));
        builder.Services.AddSingleton<IAppSettingsService,MauiAppSettingsService>();
        builder.Services.AddSingleton<IAppLockService,MauiAppLockService>();
        return builder.Build();
    }
}
