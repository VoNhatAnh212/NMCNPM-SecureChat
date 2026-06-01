#nullable enable

namespace SecureChat.Domain.Exceptions;

/// <summary>
/// Represents an error raised when an OTP has expired.
/// </summary>
public class OtpExpiredException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OtpExpiredException"/> class.
    /// </summary>
    public OtpExpiredException()
        : base("The OTP code has expired.", 400)
    {
    }
}
