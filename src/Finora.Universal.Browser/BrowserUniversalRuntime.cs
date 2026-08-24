using Finora.Universal;

namespace Finora.Universal.Browser;

internal sealed class BrowserUniversalRuntime : IUniversalRuntime
{
    public Task<UniversalRuntimeState> InitializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new UniversalRuntimeState(
            "Web / WebAssembly",
            false,
            "Browser sandbox. Native SQLite is intentionally not opened from the WebAssembly host.",
            "The Web UI host is available. Full finance persistence remains disabled until Finora's browser-specific encrypted IndexedDB/OPFS adapter passes parity, backup, migration, and privacy validation."));
    }
}
