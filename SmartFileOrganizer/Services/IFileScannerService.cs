using SmartFileOrganizer.Models;

namespace SmartFileOrganizer.Services;

public interface IFileScannerService
{
    List<FileItem> Scan(string? folderPath);
}
