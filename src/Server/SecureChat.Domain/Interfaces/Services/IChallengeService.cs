#nullable enable

namespace SecureChat.Domain.Interfaces.Services;

/// <summary>
/// Defines login challenge operations.
/// </summary>
public interface IChallengeService
{
    /// <summary>Generates a new challenge for a device.</summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <returns>The generated challenge.</returns>
    Task<string> GenerateChallengeAsync(Guid deviceId);

    /// <summary>Validates a signed challenge for a device.</summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="signedChallenge">The signed challenge value.</param>
    /// <param name="publicKeyPem">The PEM encoded public key.</param>
    /// <returns>True when the challenge is valid; otherwise, false.</returns>
    Task<bool> ValidateChallengeAsync(Guid deviceId, string signedChallenge, string publicKeyPem);
}
