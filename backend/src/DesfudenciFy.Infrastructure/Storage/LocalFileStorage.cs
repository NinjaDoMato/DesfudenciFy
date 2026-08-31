using DesfudenciFy.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace DesfudenciFy.Infrastructure.Storage;

public class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(IConfiguration configuration)
    {
        _rootPath = configuration["Storage:RootPath"]
                    ?? Path.Combine(Directory.GetCurrentDirectory(), "data", "uploads");
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SavePropertyPhotoAsync(
        Guid propertyId,
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        return await SavePhotoAsync("properties", propertyId, content, fileName, cancellationToken);
    }

    public async Task<string> SaveVehiclePhotoAsync(
        Guid vehicleId,
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        return await SavePhotoAsync("vehicles", vehicleId, content, fileName, cancellationToken);
    }

    private async Task<string> SavePhotoAsync(
        string folder,
        Guid entityId,
        Stream content,
        string fileName,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".jpg";
        }

        var relativeDir = Path.Combine(folder, entityId.ToString("N"));
        var absoluteDir = Path.Combine(_rootPath, relativeDir);
        Directory.CreateDirectory(absoluteDir);

        var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var absolutePath = Path.Combine(absoluteDir, storedName);

        await using var fileStream = File.Create(absolutePath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return Path.Combine(relativeDir, storedName).Replace('\\', '/');
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var absolute = GetAbsolutePath(relativePath);
        if (File.Exists(absolute))
        {
            File.Delete(absolute);
        }

        return Task.CompletedTask;
    }

    public string GetAbsolutePath(string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_rootPath, normalized);
    }
}
