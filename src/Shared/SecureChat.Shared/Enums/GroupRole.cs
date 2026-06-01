#nullable enable

namespace SecureChat.Shared.Enums;

/// <summary>
/// Defines a user's role in a group.
/// </summary>
public enum GroupRole
{
    /// <summary>The group owner.</summary>
    Owner,
    /// <summary>A group administrator.</summary>
    Admin,
    /// <summary>A standard group member.</summary>
    Member
}
