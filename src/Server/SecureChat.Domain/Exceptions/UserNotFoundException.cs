#nullable enable

namespace SecureChat.Domain.Exceptions;

/// <summary>
/// Represents an error raised when a user cannot be found.
/// </summary>
public class UserNotFoundException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserNotFoundException"/> class.
    /// </summary>
    /// <param name="userId">The missing user identifier.</param>
    public UserNotFoundException(Guid userId)
        : base($"User '{userId}' was not found.", 404)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserNotFoundException"/> class.
    /// </summary>
    /// <param name="email">The missing user email address.</param>
    public UserNotFoundException(string email)
        : base($"User with email '{email}' was not found.", 404)
    {
    }
}
