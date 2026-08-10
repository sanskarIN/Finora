using Finora.Application;
using Finora.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Finora.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
#if DEBUG
        builder.Logging.AddDebug();
#endif
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, Finora.Shared.AppConstants.DatabaseFileName);
        builder.Services.AddPooledDbContextFactory<FinoraDbContext>(options => options.UseSqlite($"Data Source={dbPath};Cache=Shared"));
        builder.Services.AddSingleton<DatabaseInitializer>();
        builder.Services.AddSingleton<IFinanceStore, FinanceStore>();
        builder.Services.AddSingleton<IFinanceDataResetService, FinanceDataResetService>();
        builder.Services.AddSingleton<ISampleDataService, SampleDataService>();
        builder.Services.AddSingleton<IStorageRecoveryService>(sp => new RestoreRecoveryService(sp.GetRequiredService<IDbContextFactory<FinoraDbContext>>(), FileSystem.AppDataDirectory));
        builder.Services.AddSingleton<ITransactionMaintenanceService, TransactionMaintenanceService>();
        builder.Services.AddSingleton<IAccountManagementService, AccountManagementService>();
        builder.Services.AddSingleton<ICategoryTagService, CategoryTagService>();
        builder.Services.AddSingleton<IReconciliationService, ReconciliationService>();
        builder.Services.AddSingleton<IRecurringWorkflowService, RecurringWorkflowService>();
        builder.Services.AddSingleton<ICsvImportService, CsvImportService>();
        builder.Services.AddSingleton<IAdvancedReportService, AdvancedReportService>();
        builder.Services.AddSingleton<IBackupService>(sp => new CrashSafeBackupService(sp.GetRequiredService<IDbContextFactory<FinoraDbContext>>(), FileSystem.AppDataDirectory));
        builder.Services.AddSingleton<IExportService, ExportService>();
        builder.Services.AddSingleton<IAttachmentService>(sp => new AttachmentService(sp.GetRequiredService<IDbContextFactory<FinoraDbContext>>(), FileSystem.AppDataDirectory));
        builder.Services.AddSingleton<IDataIntegrityService>(sp => new DataIntegrityService(sp.GetRequiredService<IDbContextFactory<FinoraDbContext>>(), FileSystem.AppDataDirectory));
        builder.Services.AddSingleton<IPrivacyLogger>(_ => new PrivacyLogger(FileSystem.CacheDirectory));
        builder.Services.AddSingleton<AppExceptionCoordinator>();
        builder.Services.AddSingleton<IAppSettingsService, MauiAppSettingsService>();
        builder.Services.AddSingleton<IAppLockService, MauiAppLockService>();
        builder.Services.AddSingleton<IPlatformNotificationGateway, PlatformNotificationGateway>();
        builder.Services.AddSingleton<ILocalNotificationService, LocalNotificationService>();
        builder.Services.AddSingleton<IBiometricService, PlatformBiometricService>();
        builder.Services.AddSingleton<ISensitiveScreenService, SensitiveScreenService>();
        builder.Services.AddSingleton<ReminderCoordinator>();

        var app = builder.Build();
        var privacyLogger = app.Services.GetRequiredService<IPrivacyLogger>();
        AsyncCommand.UnexpectedFailureHandler = exception => privacyLogger.Error(exception, "AsyncCommand.ExecutionFailed");
        return app;
    }
}
