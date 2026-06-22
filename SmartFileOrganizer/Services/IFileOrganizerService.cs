using SmartFileOrganizer.Models;

namespace SmartFileOrganizer.Services;

public interface IFileOrganizerService
{
    List<OrganizationResult> Organize(
        IReadOnlyList<FileItem>? files,
        IReadOnlyList<OrganizationRule>? rules,
        string? rootFolder);
}
