#nullable enable

using SecureChat.Domain.Entities;

namespace SecureChat.Domain.Interfaces.Services;

/// <summary>
/// Defines JWT token operations.
/// </summary>
public interface IJwtService
{
    /// <summary>Generates an access token for a user and device.</summary>
    /// <param name="user">The authenticated user.</param>
    /// <param name="device">The authenticated device.</param>
    /// <returns>The generated access token.</returns>
    Task<string> GenerateAccessTokenAsync(User user, Device device);

    /// <summary>Generates a refresh token for a user and device.</summary>
    /// <param name="user">The authenticated user.</param>
    /// <param name="device">The authenticated device.</param>
    /// <param name="userId">The authenticated user identifier.</param>
    /// <param name="deviceId">The authenticated device identifier.</param>
    /// <returns>The generated refresh token entity.</returns>
    Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId, Guid deviceId);

    /// <summary>Validates a token.</summary>
    /// <param name="token">The token to validate.</param>
    /// <returns>The claims principal when valid; otherwise, null.</returns>
    Task<System.Security.Claims.ClaimsPrincipal?> ValidateTokenAsync(string token);

    /// <summary>Gets claims from a token.</summary>
    /// <param name="token">The token.</param>
    /// <returns>The claims contained in the token.</returns>
    Task<Dictionary<string, string>> GetClaimsAsync(string token);
}
