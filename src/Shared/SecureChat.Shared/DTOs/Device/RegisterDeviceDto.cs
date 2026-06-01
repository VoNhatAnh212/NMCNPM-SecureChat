#nullable enable

using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.Device;

/// <summary>
/// Represents a request to register a trusted device.
/// </summary>
public record RegisterDeviceDto
{
    /// <summary>Gets the device name.</summary>
    [JsonPropertyName("deviceName")]
    public string DeviceName { get; init; } = string.Empty;

    /// <summary>Gets the device platform.</summary>
    [JsonPropertyName("platform")]
    public string Platform { get; init; } = string.Empty;

    /// <summary>Gets the PEM encoded public key for the device.</summary>
    [JsonPropertyName("publicKeyPem")]
    public string PublicKeyPem { get; init; } = string.Empty;

    /// <summary>Gets the device fingerprint.</summary>
    [JsonPropertyName("deviceFingerprint")]
    public string DeviceFingerprint { get; init; } = string.Empty;
}
