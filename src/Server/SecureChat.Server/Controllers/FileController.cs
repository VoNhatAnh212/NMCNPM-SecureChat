#nullable enable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureChat.Application.Services.File;
using SecureChat.Server.Extensions;
using SecureChat.Server.Responses;
using SecureChat.Shared.DTOs.File;

namespace SecureChat.Server.Controllers;

/// <summary>
/// Provides encrypted file API endpoints.
/// </summary>
[Authorize]
[ApiController]
[Route("api/files")]
public class FileController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly IConfiguration _configuration;

    /// <summary>Initializes a new instance of the <see cref="FileController"/> class.</summary>
    public FileController(IFileService fileService, IConfiguration configuration)
    {
        _fileService = fileService;
        _configuration = configuration;
    }

    /// <summary>Initiates an encrypted upload.</summary>
    [HttpPost("upload/init")]
    [ProducesResponseType(typeof(ApiResponse<UploadFileResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UploadFileResponseDto>>> InitiateUpload([FromBody] UploadFileRequestDto request)
    {
        var response = await _fileService.InitiateUploadAsync(User.GetUserId(), request);
        return Ok(ApiResponse<UploadFileResponseDto>.Ok(response));
    }

    /// <summary>Receives encrypted file bytes.</summary>
    [HttpPost("upload/{attachmentId:guid}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> ReceiveFile(Guid attachmentId, IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        var result = await _fileService.ReceiveEncryptedFileAsync(attachmentId, stream);
        return Ok(ApiResponse<bool>.Ok(result));
    }

    /// <summary>Downloads encrypted file bytes.</summary>
    [HttpGet("{attachmentId:guid}/download")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadFile(Guid attachmentId)
    {
        var attachment = await _fileService.GetDownloadInfoAsync(attachmentId, User.GetUserId());
        var rootPath = _configuration["FileStorage:RootPath"] ?? _configuration["FileStorage:BasePath"] ?? "uploads";
        var path = ResolvePhysicalPath(rootPath, attachment.EncryptedUrl);

        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return File(stream, attachment.MimeType, attachment.FileName);
    }

    /// <summary>Deletes an attachment.</summary>
    [HttpDelete("{attachmentId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteFile(Guid attachmentId)
    {
        var deleted = await _fileService.DeleteAttachmentAsync(attachmentId, User.GetUserId());
        return Ok(ApiResponse<bool>.Ok(deleted));
    }

    private static string ResolvePhysicalPath(string rootPath, string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        if (normalized.StartsWith("uploads" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized["uploads".Length..].TrimStart(Path.DirectorySeparatorChar);
        }

        var fullRoot = Path.GetFullPath(rootPath);
        var fullPath = Path.GetFullPath(Path.Combine(fullRoot, normalized));
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("File path is outside the configured storage root.");
        }

        return fullPath;
    }
}
