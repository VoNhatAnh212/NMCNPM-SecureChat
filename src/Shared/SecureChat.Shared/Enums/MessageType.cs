#nullable enable

namespace SecureChat.Shared.Enums;

/// <summary>
/// Defines the supported message payload types.
/// </summary>
public enum MessageType
{
    /// <summary>Text message content.</summary>
    Text,
    /// <summary>Image attachment content.</summary>
    Image,
    /// <summary>Video attachment content.</summary>
    Video,
    /// <summary>Audio attachment content.</summary>
    Audio,
    /// <summary>Generic file attachment content.</summary>
    File,
    /// <summary>System generated content.</summary>
    System
}
