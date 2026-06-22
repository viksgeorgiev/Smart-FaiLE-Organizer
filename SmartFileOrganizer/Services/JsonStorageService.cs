using System.IO;
using System.Text.Json;

namespace SmartFileOrganizer.Services;

public class JsonStorageService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public async Task<List<T>> LoadAsync<T>(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        try
        {
            await using var stream = File.OpenRead(filePath);
            var items = await JsonSerializer.DeserializeAsync<List<T>>(stream, Options);
            return items ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public async Task SaveAsync<T>(string filePath, IEnumerable<T> items)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, items, Options);
    }
}
