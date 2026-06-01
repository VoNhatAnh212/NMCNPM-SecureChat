#nullable enable

namespace SecureChat.Domain.Exceptions;

/// <summary>
/// Represents an error raised when a revoked device attempts an operation.
/// </summary>
public class DeviceRevokedException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceRevokedException"/> class.
    /// </summary>
    /// <param name="deviceId">The revoked device identifier.</param>
    public DeviceRevokedException(Guid deviceId)
        : base($"Device '{deviceId}' has been revoked.", 403)
    {
    }
}
