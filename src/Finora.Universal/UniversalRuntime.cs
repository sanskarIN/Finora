namespace Finora.Universal;

public sealed record UniversalRuntimeState(
    string PlatformName,
    bool PersistentFinanceAvailable,
    string StorageDescription,
    int AccountCount,
    string StatusMessage);

public interface IUniversalRuntime
{
    Task<UniversalRuntimeState> InitializeAsync(CancellationToken cancellationToken = default);
}

internal sealed class UnconfiguredUniversalRuntime : IUniversalRuntime
{
    public Task<UniversalRuntimeState> InitializeAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new UniversalRuntimeState(
            "Unknown",
            false,
            "No runtime host configured.",
            0,
            "Finora universal UI started without a platform runtime."));
}
