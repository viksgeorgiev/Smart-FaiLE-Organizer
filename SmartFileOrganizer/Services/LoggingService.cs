using System.IO;
using SmartFileOrganizer.Data;
using SmartFileOrganizer.Models;

namespace SmartFileOrganizer.Services;

public class LoggingService
{
    private readonly JsonStorageService _storage;
    private readonly List<LogEntry> _entries = [];

    public LoggingService(JsonStorageService storage)
    {
        _storage = storage;
    }

    public IReadOnlyList<LogEntry> Entries => _entries;

    public async Task LoadAsync()
    {
        AppDataPaths.EnsureAppFolderExists();
        var entries = await _storage.LoadAsync<LogEntry>(AppDataPaths.LogFilePath);

        _entries.Clear();
        _entries.AddRange(entries);
    }

    public LogEntry CreateLogEntry(OrganizationResult result, string fileName)
    {
        return new LogEntry
        {
            FileName = fileName,
            SourcePath = result.SourcePath,
            DestinationPath = result.DestinationPath,
            Timestamp = DateTime.Now,
            Success = result.Success
        };
    }

    public async Task<LogEntry> LogMoveAsync(OrganizationResult result, string fileName)
    {
        var entry = CreateLogEntry(result, fileName);
        _entries.Add(entry);
        await SaveAsync();
        return entry;
    }

    public async Task LogMovesAsync(IReadOnlyList<FileItem> files, IReadOnlyList<OrganizationResult> results)
    {
        for (var i = 0; i < results.Count; i++)
        {
            var fileName = i < files.Count
                ? files[i].Name
                : Path.GetFileName(results[i].SourcePath);

            _entries.Add(CreateLogEntry(results[i], fileName));
        }

        await SaveAsync();
    }

    public async Task SaveAsync()
    {
        AppDataPaths.EnsureAppFolderExists();
        await _storage.SaveAsync(AppDataPaths.LogFilePath, _entries);
    }
}
