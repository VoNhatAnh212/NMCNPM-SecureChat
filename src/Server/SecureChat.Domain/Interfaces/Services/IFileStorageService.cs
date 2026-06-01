#nullable enable

namespace SecureChat.Domain.Interfaces.Services;

/// <summary>
/// Defines encrypted file storage operations.
/// </summary>
public interface IFileStorageService
{
    /// <summary>Generates an upload URL for an attachment.</summary>
    /// <param name="attachmentId">The attachment identifier.</param>
    /// <param name="fileName">The original file name.</param>
    /// <returns>The relative upload path and expiration time.</returns>
    Task<(string Path, DateTimeOffset ExpiresAt)> GenerateUploadUrlAsync(Guid attachmentId, string fileName);

    /// <summary>Generates a download URL for a stored encrypted file.</summary>
    /// <param name="attachmentId">The attachment identifier.</param>
    /// <returns>The generated download URL.</returns>
    Task<string> GenerateDownloadUrlAsync(Guid attachmentId);

    /// <summary>Deletes a stored encrypted file.</summary>
    /// <param name="path">The relative storage path.</param>
    /// <returns>True when a file was deleted; otherwise, false.</returns>
    Task<bool> DeleteFileAsync(string path);

    /// <summary>Determines whether a stored encrypted file exists.</summary>
    /// <param name="path">The relative storage path.</param>
    /// <returns>True when the file exists; otherwise, false.</returns>
    Task<bool> FileExistsAsync(string path);
}
