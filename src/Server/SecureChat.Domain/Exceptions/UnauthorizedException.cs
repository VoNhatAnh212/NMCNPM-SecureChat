#nullable enable

namespace SecureChat.Domain.Exceptions;

/// <summary>
/// Represents an authorization failure.
/// </summary>
public class UnauthorizedException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedException"/> class.
    /// </summary>
    public UnauthorizedException()
        : base("The request is not authorized.", 401)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public UnauthorizedException(string message)
        : base(message, 401)
    {
    }
}
