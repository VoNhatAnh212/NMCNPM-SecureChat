#nullable enable

namespace SecureChat.Domain.Exceptions;

/// <summary>
/// Represents an error raised when a device cannot be found.
/// </summary>
public class DeviceNotFoundException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceNotFoundException"/> class.
    /// </summary>
    /// <param name="deviceId">The missing device identifier.</param>
    public DeviceNotFoundException(Guid deviceId)
        : base($"Device '{deviceId}' was not found.", 404)
    {
    }
}
