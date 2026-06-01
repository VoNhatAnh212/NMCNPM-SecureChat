#nullable enable

namespace SecureChat.Shared.Enums;

/// <summary>
/// Defines the lifecycle state of a registered device.
/// </summary>
public enum DeviceStatus
{
    /// <summary>The device is active.</summary>
    Active,
    /// <summary>The device has been revoked.</summary>
    Revoked,
    /// <summary>The device registration has expired.</summary>
    Expired
}
