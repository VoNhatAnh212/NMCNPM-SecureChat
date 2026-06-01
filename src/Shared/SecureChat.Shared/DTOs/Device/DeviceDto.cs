#nullable enable

using System;
using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.Device;

/// <summary>
/// Represents a registered user device.
/// </summary>
public record DeviceDto
{
    /// <summary>Gets the device identifier.</summary>
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    /// <summary>Gets the device name.</summary>
    [JsonPropertyName("deviceName")]
    public string DeviceName { get; init; } = string.Empty;

    /// <summary>Gets the device platform.</summary>
    [JsonPropertyName("platform")]
    public string Platform { get; init; } = string.Empty;

    /// <summary>Gets the last login time for the device.</summary>
    [JsonPropertyName("lastLoginAt")]
    public DateTimeOffset? LastLoginAt { get; init; }

    /// <summary>Gets a value indicating whether this is the current device.</summary>
    [JsonPropertyName("isCurrentDevice")]
    public bool IsCurrentDevice { get; init; }

    /// <summary>Gets the device fingerprint.</summary>
    [JsonPropertyName("fingerprint")]
    public string Fingerprint { get; init; } = string.Empty;
}
