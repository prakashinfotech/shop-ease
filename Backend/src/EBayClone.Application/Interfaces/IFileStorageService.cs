namespace EBayClone.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string folder = "documents", CancellationToken ct = default);
    Task DeleteAsync(string fileUrl, CancellationToken ct = default);
}
