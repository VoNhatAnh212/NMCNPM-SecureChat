#nullable enable

using Microsoft.AspNetCore.SignalR;
using SecureChat.Server.Extensions;

namespace SecureChat.Server.Hubs;

/// <summary>
/// Resolves SignalR user identifiers from JWT claims.
/// </summary>
public class SecureChatUserIdProvider : IUserIdProvider
{
    /// <inheritdoc />
    public string? GetUserId(HubConnectionContext connection)
    {
        try
        {
            return connection.User?.GetUserId().ToString();
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
