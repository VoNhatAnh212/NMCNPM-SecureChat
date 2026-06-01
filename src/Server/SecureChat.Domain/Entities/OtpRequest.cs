#nullable enable

namespace SecureChat.Domain.Entities;

/// <summary>
/// Represents an OTP verification request.
/// </summary>
public class OtpRequest
{
    /// <summary>Initializes a new instance of the <see cref="OtpRequest"/> class.</summary>
    public OtpRequest()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OtpRequest"/> class with required fields.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="otpCode">The OTP code.</param>
    /// <param name="expiresAt">The expiration time.</param>
    public OtpRequest(string email, string otpCode, DateTimeOffset expiresAt)
    {
        Id = Guid.NewGuid();
        Email = email;
        OtpCode = otpCode;
        ExpiresAt = expiresAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets or sets the OTP request identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the OTP code.</summary>
    public string OtpCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the expiration time.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Gets or sets a value indicating whether the OTP has been used.</summary>
    public bool IsUsed { get; set; }

    /// <summary>Gets or sets the number of verification attempts.</summary>
    public int Attempts { get; set; }

    /// <summary>Gets or sets the time when the OTP request was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
