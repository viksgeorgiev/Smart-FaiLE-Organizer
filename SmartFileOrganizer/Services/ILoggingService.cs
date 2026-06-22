using SmartFileOrganizer.Models;

namespace SmartFileOrganizer.Services;

public interface ILoggingService
{
    IReadOnlyList<LogEntry> Entries { get; }

    Task LoadAsync();

    Task LogMovesAsync(IReadOnlyList<FileItem>? files, IReadOnlyList<OrganizationResult>? results);

    Task ClearAsync();
}
