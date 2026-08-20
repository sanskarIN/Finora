using System.ComponentModel;
using System.Runtime.CompilerServices;
using Finora.Shared;

namespace Finora.Universal.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IUniversalRuntime _runtime;
    private string _platformName = "Detecting…";
    private string _storageDescription = "Checking local storage…";
    private string _statusMessage = "Initializing Finora…";
    private bool _persistentFinanceAvailable;

    public MainViewModel(IUniversalRuntime runtime)
    {
        _runtime = runtime;
        ProductName = AppConstants.ProductName;
        Attribution = AppConstants.Watermark;
        RepositoryUrl = AppConstants.RepositoryUrl;
        _ = InitializeAsync();
    }

    public string ProductName { get; }
    public string Attribution { get; }
    public string RepositoryUrl { get; }

    public string PlatformName
    {
        get => _platformName;
        private set => SetField(ref _platformName, value);
    }

    public string StorageDescription
    {
        get => _storageDescription;
        private set => SetField(ref _storageDescription, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public bool PersistentFinanceAvailable
    {
        get => _persistentFinanceAvailable;
        private set => SetField(ref _persistentFinanceAvailable, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private async Task InitializeAsync()
    {
        try
        {
            var state = await _runtime.InitializeAsync().ConfigureAwait(true);
            PlatformName = state.PlatformName;
            PersistentFinanceAvailable = state.PersistentFinanceAvailable;
            StorageDescription = state.StorageDescription;
            StatusMessage = state.StatusMessage;
        }
        catch (Exception)
        {
            PlatformName = "Runtime error";
            PersistentFinanceAvailable = false;
            StorageDescription = "Local runtime initialization failed.";
            StatusMessage = "Finora could not initialize this platform host. No private finance details were exposed.";
        }
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
