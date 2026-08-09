using Finora.Application;

namespace Finora.App;

public sealed class AppExceptionCoordinator(IPrivacyLogger logger)
{
    private int _started;

    public void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
            return;

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    public void Report(Exception exception, string eventName)
    {
        logger.Error(exception, NormalizeEventName(eventName));
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        if (args.ExceptionObject is Exception exception)
            Report(exception, args.IsTerminating ? "app_unhandled_terminating" : "app_unhandled");
        else
            logger.Information(args.IsTerminating ? "app_unhandled_nonexception_terminating" : "app_unhandled_nonexception");
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        Report(args.Exception, "task_unobserved_exception");
        args.SetObserved();
    }

    private static string NormalizeEventName(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName))
            return "app_exception";

        return new string(eventName
            .Where(character => char.IsLetterOrDigit(character) || character is '_' or '-' or '.')
            .Take(80)
            .ToArray());
    }
}
