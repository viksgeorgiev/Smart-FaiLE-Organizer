using System.IO;
using System.Text.Json;
using SmartFileOrganizer.Models;

namespace SmartFileOrganizer.Services;

public class FileOrganizerService
{
    public List<OrganizationResult> Organize(
        IReadOnlyList<FileItem>? files,
        IReadOnlyList<OrganizationRule>? rules,
        string? rootFolder)
    {
        if (files is null || files.Count == 0)
        {
            return [];
        }

        if (rules is null)
        {
            return files.Select(file => new OrganizationResult
            {
                SourcePath = file.FullPath ?? string.Empty,
                Success = false,
                Message = "No organization rules are available."
            }).ToList();
        }

        if (string.IsNullOrWhiteSpace(rootFolder) || !Directory.Exists(rootFolder))
        {
            return files.Select(file => new OrganizationResult
            {
                SourcePath = file.FullPath ?? string.Empty,
                Success = false,
                Message = "Root folder is invalid."
            }).ToList();
        }

        return files.Select(file => OrganizeFile(file, rules, rootFolder)).ToList();
    }

    private static OrganizationResult OrganizeFile(
        FileItem file,
        IReadOnlyList<OrganizationRule> rules,
        string rootFolder)
    {
        if (file is null || string.IsNullOrWhiteSpace(file.FullPath))
        {
            return new OrganizationResult
            {
                Success = false,
                Message = "Source file path is missing."
            };
        }

        if (string.IsNullOrWhiteSpace(file.Name))
        {
            return new OrganizationResult
            {
                SourcePath = file.FullPath,
                Success = false,
                Message = "Source file name is missing."
            };
        }

        var rule = FindMatchingRule(file.Extension, rules);
        if (rule is null || string.IsNullOrWhiteSpace(rule.DestinationFolder))
        {
            return new OrganizationResult
            {
                SourcePath = file.FullPath,
                Success = false,
                Message = "No matching rule."
            };
        }

        var destinationDirectory = Path.Combine(rootFolder, rule.DestinationFolder);
        var destinationPath = Path.Combine(destinationDirectory, file.Name);

        try
        {
            if (!File.Exists(file.FullPath))
            {
                return new OrganizationResult
                {
                    SourcePath = file.FullPath,
                    DestinationPath = destinationPath,
                    Success = false,
                    Message = "Source file not found."
                };
            }

            if (string.Equals(file.FullPath, destinationPath, StringComparison.OrdinalIgnoreCase))
            {
                return new OrganizationResult
                {
                    SourcePath = file.FullPath,
                    DestinationPath = destinationPath,
                    Success = true,
                    Message = "File is already in the destination."
                };
            }

            if (File.Exists(destinationPath))
            {
                return new OrganizationResult
                {
                    SourcePath = file.FullPath,
                    DestinationPath = destinationPath,
                    Success = false,
                    Message = "Destination file already exists."
                };
            }

            Directory.CreateDirectory(destinationDirectory);
            File.Move(file.FullPath, destinationPath);

            return new OrganizationResult
            {
                SourcePath = file.FullPath,
                DestinationPath = destinationPath,
                Success = true,
                Message = "File moved successfully."
            };
        }
        catch (UnauthorizedAccessException)
        {
            return CreateFailureResult(file.FullPath, destinationPath, "Access denied while moving the file.");
        }
        catch (IOException)
        {
            return CreateFailureResult(file.FullPath, destinationPath, "The file could not be moved. It may be in use.");
        }
        catch (Exception)
        {
            return CreateFailureResult(file.FullPath, destinationPath, "An unexpected error occurred while moving the file.");
        }
    }

    private static OrganizationResult CreateFailureResult(
        string sourcePath,
        string destinationPath,
        string message)
    {
        return new OrganizationResult
        {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            Success = false,
            Message = message
        };
    }

    private static OrganizationRule? FindMatchingRule(
        string? extension,
        IReadOnlyList<OrganizationRule> rules)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return null;
        }

        var normalizedExtension = NormalizeExtension(extension);
        return rules.FirstOrDefault(rule => NormalizeExtension(rule.Extension) == normalizedExtension);
    }

    private static string NormalizeExtension(string extension)
    {
        extension = extension.Trim().ToLowerInvariant();

        if (!extension.StartsWith('.'))
        {
            extension = "." + extension;
        }

        return extension;
    }
}
