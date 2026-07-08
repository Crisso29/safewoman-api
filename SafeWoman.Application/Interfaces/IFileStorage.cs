namespace SafeWoman.Application.Interfaces;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream fileStream, string originalFileName, string subFolder, CancellationToken ct = default);
    void Delete(string filePath);
}
