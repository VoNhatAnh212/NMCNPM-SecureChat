#nullable enable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureChat.Domain.Exceptions;
using SecureChat.Domain.Interfaces.Repositories;
using SecureChat.Domain.Interfaces.Services;
using SecureChat.Shared.DTOs.File;
using DomainAttachment = SecureChat.Domain.Entities.Attachment;

namespace SecureChat.Application.Services.File;

/// <summary>
/// Provides encrypted file workflows.
/// </summary>
public class FileService : IFileService
{
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileService"/> class.
    /// </summary>
    public FileService(
        IAttachmentRepository attachmentRepository,
        IFileStorageService fileStorageService,
        IConfiguration configuration,
        ILogger<FileService> logger)
    {
        _attachmentRepository = attachmentRepository;
        _fileStorageService = fileStorageService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UploadFileResponseDto> InitiateUploadAsync(Guid userId, UploadFileRequestDto request)
    {
        _logger.LogInformation("Initiating encrypted upload for user {UserId}, file {FileName}.", userId, request.FileName);

        var maxSizeMb = GetConfiguredMaxFileSizeMb();
        var maxSizeBytes = maxSizeMb * 1024L * 1024L;
        if (request.FileSize > maxSizeBytes)
        {
            throw new DomainException($"File size exceeds the configured limit of {maxSizeMb} MB.", 413);
        }

        var attachmentId = Guid.NewGuid();
        var uploadInfo = await _fileStorageService.GenerateUploadUrlAsync(attachmentId, request.FileName);
        var attachment = new DomainAttachment
        {
            Id = attachmentId,
            MessageId = null,
            FileName = request.FileName,
            FileSize = request.FileSize,
            MimeType = request.MimeType,
            EncryptedStoragePath = uploadInfo.Path,
            EncryptedFileKey = Decode(request.EncryptedFileKey),
            IV = Decode(request.IV),
            AuthTag = Decode(request.AuthTag),
            UploadedAt = DateTimeOffset.UtcNow
        };

        await _attachmentRepository.AddAsync(attachment);

        return new UploadFileResponseDto
        {
            AttachmentId = attachmentId,
            UploadUrl = uploadInfo.Path,
            ExpiresAt = uploadInfo.ExpiresAt
        };
    }

    /// <inheritdoc />
    public async Task<bool> ReceiveEncryptedFileAsync(Guid attachmentId, Stream stream)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(attachmentId)
            ?? throw new DomainException($"Attachment '{attachmentId}' was not found.", 404);

        var maxSizeMb = GetConfiguredMaxFileSizeMb();
        var maxSizeBytes = maxSizeMb * 1024L * 1024L;
        var rootPath = _configuration["FileStorage:RootPath"] ?? _configuration["FileStorage:BasePath"] ?? "uploads";
        var path = ResolvePhysicalPath(rootPath, attachment.EncryptedStoragePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? rootPath);

        await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        var buffer = new byte[81920];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
        {
            totalRead += bytesRead;
            if (totalRead > maxSizeBytes)
            {
                throw new DomainException($"File size exceeds the configured limit of {maxSizeMb} MB.", 413);
            }

            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<AttachmentDto> GetDownloadInfoAsync(Guid attachmentId, Guid requesterId)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(attachmentId)
            ?? throw new DomainException($"Attachment '{attachmentId}' was not found.", 404);

        return MapAttachment(attachment);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAttachmentAsync(Guid attachmentId, Guid requesterId)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(attachmentId)
            ?? throw new DomainException($"Attachment '{attachmentId}' was not found.", 404);

        var deleted = await _fileStorageService.DeleteFileAsync(attachment.EncryptedStoragePath);
        await _attachmentRepository.DeleteAsync(attachmentId);
        return deleted;
    }

    private static AttachmentDto MapAttachment(DomainAttachment attachment)
    {
        return new AttachmentDto
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            FileSize = attachment.FileSize,
            MimeType = attachment.MimeType,
            EncryptedUrl = attachment.EncryptedStoragePath,
            EncryptedFileKey = Convert.ToBase64String(attachment.EncryptedFileKey),
            IV = Convert.ToBase64String(attachment.IV),
            AuthTag = Convert.ToBase64String(attachment.AuthTag)
        };
    }

    private int GetConfiguredMaxFileSizeMb()
    {
        return int.TryParse(_configuration["FileStorage:MaxFileSizeMb"], out var value) ? value : 100;
    }

    private static byte[] Decode(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? Array.Empty<byte>() : Convert.FromBase64String(value);
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
