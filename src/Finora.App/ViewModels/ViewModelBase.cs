using System.ComponentModel;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Input;

namespace Finora.App;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    private const string GenericFailureMessage = "The operation could not be completed safely. Try again, or run the local data-integrity check if the problem continues.";
    private bool _isBusy;
    private string? _errorMessage;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsBusy
    {
        get => _isBusy;
        protected set => SetProperty(ref _isBusy, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        protected set
        {
            if (SetProperty(ref _errorMessage, value)) OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected async Task RunAsync(Func<Task> action)
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await action().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "The operation was cancelled.";
        }
        catch (Exception exception) when (!IsFatal(exception))
        {
            ErrorMessage = ToUserSafeMessage(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    internal static string ToUserSafeMessage(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (exception is IOException or UnauthorizedAccessException or CryptographicException or JsonException or DbException)
            return GenericFailureMessage;

        if (exception is not (ArgumentException or InvalidOperationException or FormatException))
            return GenericFailureMessage;

        var message = exception.Message?.Trim() ?? string.Empty;
        if (message.Length == 0 || message.Length > 300 || ContainsTechnicalOrPathDetail(message))
            return GenericFailureMessage;
        return message;
    }

    private static bool ContainsTechnicalOrPathDetail(string message)
    {
        if (message.Contains('\n') || message.Contains('\r')) return true;
        if (message.Contains(Path.DirectorySeparatorChar) || message.Contains(Path.AltDirectorySeparatorChar)) return true;

        string[] markers =
        [
            "Data Source=",
            "SQLite",
            "Microsoft.EntityFrameworkCore",
            "System.",
            ".dll",
            "stack trace",
            " at ",
            "0x"
        ];
        return markers.Any(marker => message.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsFatal(Exception exception)
        => exception is OutOfMemoryException or StackOverflowException or AccessViolationException;
}

public sealed class AsyncCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _running;

    public AsyncCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public static Action<Exception>? UnexpectedFailureHandler { get; set; }
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_running && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        try
        {
            _running = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            await _execute().ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException and not AccessViolationException)
        {
            UnexpectedFailureHandler?.Invoke(exception);
        }
        finally
        {
            _running = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
