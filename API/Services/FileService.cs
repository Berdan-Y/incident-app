using Microsoft.AspNetCore.Http;

namespace API.Services;

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public FileService(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;
    }

    public async Task<string> SaveFileAsync(IFormFile file, string directory)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty", nameof(file));

        // Ensure wwwroot directory exists
        var wwwrootPath = _environment.WebRootPath;
        if (string.IsNullOrEmpty(wwwrootPath))
        {
            wwwrootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
            if (!Directory.Exists(wwwrootPath))
            {
                Directory.CreateDirectory(wwwrootPath);
            }
        }

        // Create directory if it doesn't exist
        var uploadPath = Path.Combine(wwwrootPath, directory);
        if (!Directory.Exists(uploadPath))
            Directory.CreateDirectory(uploadPath);

        // Generate unique filename
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadPath, fileName);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Path.Combine(directory, fileName);
    }

    public async Task DeleteFileAsync(string filePath)
    {
        var wwwrootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var fullPath = Path.Combine(wwwrootPath, filePath);
        if (File.Exists(fullPath))
        {
            await Task.Run(() => File.Delete(fullPath));
        }
    }

    public string GetFileUrl(string filePath)
    {
        var baseUrl = _configuration["BaseUrl"] ?? "http://localhost:5000";
        return $"{baseUrl}/{filePath.Replace("\\", "/")}";
    }
} 