using EBayClone.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EBayClone.Infrastructure.Services;

public class LocalFileStorageService(
    IConfiguration configuration,
    ILogger<LocalFileStorageService> logger) : IFileStorageService
{
    private readonly string _uploadBasePath = Path.GetFullPath(
        configuration["FileStorage:UploadDirectory"] ?? "wwwroot/uploads");

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string folder = "documents", CancellationToken ct = default)
    {
        var safeFolder = string.IsNullOrWhiteSpace(folder)
            ? "documents"
            : folder.Replace("..", string.Empty).Trim('/', '\\');
        var dir = Path.Combine(_uploadBasePath, safeFolder);
        Directory.CreateDirectory(dir);

        var extension = Path.GetExtension(fileName);
        var uniqueName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(dir, uniqueName);

        using var output = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await fileStream.CopyToAsync(output, ct);

        logger.LogInformation("File uploaded: {FileName} -> {Path}", fileName, filePath);

        return $"/uploads/{safeFolder.Replace("\\", "/")}/{uniqueName}";
    }

    public Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        var relativePath = fileUrl.TrimStart('/').Replace("uploads/", "", StringComparison.OrdinalIgnoreCase);
        var fullPath = Path.Combine(_uploadBasePath, relativePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            logger.LogInformation("File deleted: {Path}", fullPath);
        }

        return Task.CompletedTask;
    }
}
