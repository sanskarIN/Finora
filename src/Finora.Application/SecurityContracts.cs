using Finora.Shared;

namespace Finora.Application;

public enum BiometricAvailability { Available, NotEnrolled, NotAvailable, Unsupported }

public interface IBiometricService
{
    Task<BiometricAvailability> GetAvailabilityAsync(CancellationToken cancellationToken = default);
    Task<Result> AuthenticateAsync(string reason, CancellationToken cancellationToken = default);
}

public interface ISensitiveScreenService
{
    bool IsProtectionSupported { get; }
    Task<Result> SetProtectionAsync(bool enabled, CancellationToken cancellationToken = default);
}
