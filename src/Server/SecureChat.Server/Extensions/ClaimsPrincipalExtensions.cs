#nullable enable

using System.Security.Claims;

namespace SecureChat.Server.Extensions;

/// <summary>
/// Provides helpers for reading SecureChat claims.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>Gets the current user identifier from claims.</summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub")
            ?? principal.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("User id claim is missing or invalid.");
    }

    /// <summary>Gets the current device identifier from claims.</summary>
    public static Guid GetDeviceId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("deviceId");
        return Guid.TryParse(value, out var deviceId)
            ? deviceId
            : throw new UnauthorizedAccessException("Device id claim is missing or invalid.");
    }
}
