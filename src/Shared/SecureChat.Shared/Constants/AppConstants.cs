#nullable enable

namespace SecureChat.Shared.Constants;

/// <summary>
/// Contains application-wide constants shared by SecureChat projects.
/// </summary>
public static class AppConstants
{
    /// <summary>RSA key size in bits.</summary>
    public const int RSA_KEY_SIZE = 4096;

    /// <summary>AES key size in bits.</summary>
    public const int AES_KEY_SIZE = 256;

    /// <summary>Default access token lifetime in minutes.</summary>
    public const int TOKEN_EXPIRY_MINUTES = 15;

    /// <summary>Default refresh token lifetime in days.</summary>
    public const int REFRESH_TOKEN_EXPIRY_DAYS = 30;

    /// <summary>Maximum encrypted file size in megabytes.</summary>
    public const int MAX_FILE_SIZE_MB = 100;

    /// <summary>Maximum encrypted file size in bytes.</summary>
    public const long MAX_FILE_SIZE_BYTES = MAX_FILE_SIZE_MB * 1024L * 1024L;

    /// <summary>Login challenge lifetime in seconds.</summary>
    public const int CHALLENGE_EXPIRY_SECONDS = 60;

    /// <summary>OTP code lifetime in minutes.</summary>
    public const int OTP_EXPIRY_MINUTES = 5;

    /// <summary>Expected OTP code length.</summary>
    public const int OTP_CODE_LENGTH = 6;

    /// <summary>Minimum allowed display name length.</summary>
    public const int MIN_DISPLAY_NAME_LENGTH = 2;

    /// <summary>Maximum allowed display name length.</summary>
    public const int MAX_DISPLAY_NAME_LENGTH = 64;

    /// <summary>Maximum allowed group name length.</summary>
    public const int MAX_GROUP_NAME_LENGTH = 100;

    /// <summary>Maximum allowed message ciphertext length.</summary>
    public const int MAX_ENCRYPTED_MESSAGE_LENGTH = 65536;

    /// <summary>Maximum allowed friend request message length.</summary>
    public const int MAX_FRIEND_REQUEST_MESSAGE_LENGTH = 500;

    /// <summary>AES-GCM initialization vector size in bytes.</summary>
    public const int AES_GCM_IV_SIZE_BYTES = 12;

    /// <summary>AES-GCM authentication tag size in bytes.</summary>
    public const int AES_GCM_TAG_SIZE_BYTES = 16;

    /// <summary>Default page size for list endpoints.</summary>
    public const int DEFAULT_PAGE_SIZE = 20;

    /// <summary>Maximum page size for list endpoints.</summary>
    public const int MAX_PAGE_SIZE = 100;
}
