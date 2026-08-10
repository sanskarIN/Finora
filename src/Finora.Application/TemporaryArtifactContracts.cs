namespace Finora.Application;

public interface ITemporaryArtifactCleaner
{
    Task<int> CleanupStaleAsync(TimeSpan minimumAge, CancellationToken cancellationToken = default);
}
