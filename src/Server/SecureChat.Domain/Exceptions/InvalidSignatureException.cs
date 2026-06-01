#nullable enable

namespace SecureChat.Domain.Exceptions;

/// <summary>
/// Represents an error raised when a cryptographic signature is invalid.
/// </summary>
public class InvalidSignatureException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidSignatureException"/> class.
    /// </summary>
    public InvalidSignatureException()
        : base("The provided signature is invalid.", 400)
    {
    }
}
