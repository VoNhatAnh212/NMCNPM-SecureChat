#nullable enable

namespace SecureChat.Domain.Exceptions;

/// <summary>
/// Represents a domain-level exception with an HTTP-compatible status code.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="statusCode">The HTTP-compatible status code.</param>
    public DomainException(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="statusCode">The HTTP-compatible status code.</param>
    /// <param name="innerException">The inner exception.</param>
    public DomainException(string message, int statusCode, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    /// <summary>Gets the HTTP-compatible status code.</summary>
    public int StatusCode { get; }
}
