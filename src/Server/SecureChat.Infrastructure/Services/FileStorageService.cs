#nullable enable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureChat.Domain.Interfaces.Services;

namespace SecureChat.Infrastructure.Services;

/// <summary>
/// Provides local disk storage path generation for encrypted files.
/// </summary>
public class FileStorageService : IFileStorageService
{
    private readonly string _rootPath;
    private readonly ILogger<FileStorageService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileStorageService"/> class.
    /// </summary>
    public FileStorageService(IConfiguration configuration, ILogger<FileStorageService> logger)
    {
        _rootPath = configuration["FileStorage:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "uploads");
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<(string Path, DateTimeOffset ExpiresAt)> GenerateUploadUrlAsync(Guid attachmentId, string fileName)
    {
        _logger.LogDebug("Generating upload path for attachment {AttachmentId}.", attachmentId);

        var date = DateTimeOffset.UtcNow.ToString("yyyyMMdd");
        var safeFileName = Path.GetFileName(fileName);
        var relativePath = Path.Combine("uploads", date, attachmentId.ToString(), safeFileName).Replace('\\', '/');
        var physicalDirectory = ResolvePhysicalDirectory(relativePath);

        Directory.CreateDirectory(physicalDirectory);
        return Task.FromResult((relativePath, DateTimeOffset.UtcNow.AddMinutes(15)));
    }

    /// <inheritdoc />
    public Task<string> GenerateDownloadUrlAsync(Guid attachmentId)
    {
        _logger.LogDebug("Generating download path for attachment {AttachmentId}.", attachmentId);
        return Task.FromResult($"/files/{attachmentId}/download");
    }

    /// <inheritdoc />
    public Task<bool> DeleteFileAsync(string path)
    {
        _logger.LogInformation("Deleting stored file at {Path}.", path);

        var physicalPath = ResolvePhysicalPath(path);
        if (!File.Exists(physicalPath))
        {
            return Task.FromResult(false);
        }

        File.Delete(physicalPath);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> FileExistsAsync(string path)
    {
        var physicalPath = ResolvePhysicalPath(path);
        return Task.FromResult(File.Exists(physicalPath));
    }

    private string ResolvePhysicalDirectory(string relativePath)
    {
        var physicalPath = ResolvePhysicalPath(relativePath);
        return Path.GetDirectoryName(physicalPath) ?? _rootPath;
    }

    private string ResolvePhysicalPath(string relativePath)
    {
        var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        if (normalizedPath.StartsWith("uploads" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            normalizedPath = normalizedPath["uploads".Length..].TrimStart(Path.DirectorySeparatorChar);
        }

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, normalizedPath));
        var rootFullPath = Path.GetFullPath(_rootPath);

        if (!fullPath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The storage path is outside the configured file storage root.");
        }

        return fullPath;
    }
}
