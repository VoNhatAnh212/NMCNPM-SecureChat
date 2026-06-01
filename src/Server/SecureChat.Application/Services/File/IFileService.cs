#nullable enable

using SecureChat.Shared.DTOs.File;

namespace SecureChat.Application.Services.File;

/// <summary>
/// Defines encrypted file application operations.
/// </summary>
public interface IFileService
{
    /// <summary>Initiates an encrypted file upload.</summary>
    Task<UploadFileResponseDto> InitiateUploadAsync(Guid userId, UploadFileRequestDto request);

    /// <summary>Receives encrypted file bytes from a stream.</summary>
    Task<bool> ReceiveEncryptedFileAsync(Guid attachmentId, Stream stream);

    /// <summary>Gets attachment download metadata.</summary>
    Task<AttachmentDto> GetDownloadInfoAsync(Guid attachmentId, Guid requesterId);

    /// <summary>Deletes an attachment.</summary>
    Task<bool> DeleteAttachmentAsync(Guid attachmentId, Guid requesterId);
}
