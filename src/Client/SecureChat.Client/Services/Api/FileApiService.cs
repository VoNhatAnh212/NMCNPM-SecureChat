#nullable enable

using SecureChat.Shared.DTOs.File;

namespace SecureChat.Client.Services.Api;

/// <summary>
/// Provides encrypted file API operations.
/// </summary>
public class FileApiService
{
    private readonly IApiClient _apiClient;

    /// <summary>Initializes a new instance of the <see cref="FileApiService"/> class.</summary>
    public FileApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>Initiates an upload.</summary>
    public Task<UploadFileResponseDto> InitiateUploadAsync(UploadFileRequestDto request) => _apiClient.PostAsync<UploadFileRequestDto, UploadFileResponseDto>("/api/files/upload/init", request);

    /// <summary>Uploads an encrypted file.</summary>
    public async Task<bool> UploadEncryptedFileAsync(Guid attachmentId, string encryptedFilePath)
    {
        await using var stream = System.IO.File.OpenRead(encryptedFilePath);
        return await _apiClient.PostFileAsync($"/api/files/upload/{attachmentId}", stream, Path.GetFileName(encryptedFilePath));
    }

    /// <summary>Downloads an encrypted file.</summary>
    public async Task<bool> DownloadEncryptedFileAsync(Guid attachmentId, string savePath)
    {
        await using var stream = await _apiClient.GetFileAsync($"/api/files/{attachmentId}/download");
        await using var fileStream = System.IO.File.Create(savePath);
        await stream.CopyToAsync(fileStream);
        return true;
    }

    /// <summary>Downloads encrypted file bytes into a temporary file.</summary>
    public async Task<string> DownloadEncryptedFileToTempAsync(Guid attachmentId)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.securechat");
        await DownloadEncryptedFileAsync(attachmentId, path);
        return path;
    }

    /// <summary>Deletes an attachment.</summary>
    public Task<bool> DeleteAttachmentAsync(Guid attachmentId) => _apiClient.DeleteAsync($"/api/files/{attachmentId}");
}
