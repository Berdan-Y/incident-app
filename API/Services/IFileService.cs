using Microsoft.AspNetCore.Http;

namespace API.Services;

public interface IFileService
{
    Task<string> SaveFileAsync(IFormFile file, string directory);
    Task DeleteFileAsync(string filePath);
    string GetFileUrl(string filePath);
}