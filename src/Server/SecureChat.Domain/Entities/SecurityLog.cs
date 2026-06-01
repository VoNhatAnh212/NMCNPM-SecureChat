#nullable enable

namespace SecureChat.Domain.Entities;

/// <summary>
/// Represents a security audit event.
/// </summary>
public class SecurityLog
{
    /// <summary>Initializes a new instance of the <see cref="SecurityLog"/> class.</summary>
    public SecurityLog()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityLog"/> class with required fields.
    /// </summary>
    /// <param name="userId">The related user identifier.</param>
    /// <param name="deviceId">The related device identifier.</param>
    /// <param name="eventType">The security event type.</param>
    public SecurityLog(Guid? userId, Guid? deviceId, string eventType)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        DeviceId = deviceId;
        EventType = eventType;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets or sets the security log identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the related user identifier.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Gets or sets the related device identifier.</summary>
    public Guid? DeviceId { get; set; }

    /// <summary>Gets or sets the event type.</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Gets or sets the IP address associated with the event.</summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>Gets or sets the user agent associated with the event.</summary>
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>Gets or sets additional event details.</summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>Gets or sets the time when the event was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
