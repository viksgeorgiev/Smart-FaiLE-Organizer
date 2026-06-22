using System.IO;
using System.Text.Json;
using SmartFileOrganizer.Data;
using SmartFileOrganizer.Models;

namespace SmartFileOrganizer.Services;

public class LoggingService : ILoggingService
{
    private readonly JsonStorageService _storage;
    private readonly List<LogEntry> _entries = [];

    public LoggingService(JsonStorageService storage)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    public IReadOnlyList<LogEntry> Entries => _entries;

    public async Task LoadAsync()
    {
        AppDataPaths.EnsureAppFolderExists();
        var entries = await _storage.LoadAsync<LogEntry>(AppDataPaths.LogFilePath);

        _entries.Clear();
        _entries.AddRange(entries);
    }

    private LogEntry CreateLogEntry(OrganizationResult? result, string? fileName)
    {
        if (result is null)
        {
            return new LogEntry
            {
                FileName = fileName ?? string.Empty,
                Timestamp = DateTime.Now,
                Success = false
            };
        }

        return new LogEntry
        {
            FileName = fileName ?? string.Empty,
            SourcePath = result.SourcePath,
            DestinationPath = result.DestinationPath,
            Timestamp = DateTime.Now,
            Success = result.Success
        };
    }

    public async Task LogMovesAsync(IReadOnlyList<FileItem>? files, IReadOnlyList<OrganizationResult>? results)
    {
        if (results is null || results.Count == 0)
        {
            return;
        }

        for (var i = 0; i < results.Count; i++)
        {
            var fileName = files is not null && i < files.Count
                ? files[i].Name
                : Path.GetFileName(results[i].SourcePath);

            _entries.Add(CreateLogEntry(results[i], fileName));
        }

        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        AppDataPaths.EnsureAppFolderExists();
        await _storage.SaveAsync(AppDataPaths.LogFilePath, _entries);
    }

    public async Task ClearAsync()
    {
        _entries.Clear();
        await SaveAsync();
    }
}
