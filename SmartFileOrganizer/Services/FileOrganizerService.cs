using System.IO;
using SmartFileOrganizer.Models;

namespace SmartFileOrganizer.Services;

public class FileOrganizerService
{
    public List<OrganizationResult> Organize(
        IReadOnlyList<FileItem> files,
        IReadOnlyList<OrganizationRule> rules,
        string rootFolder)
    {
        if (files.Count == 0)
        {
            return [];
        }

        if (string.IsNullOrWhiteSpace(rootFolder) || !Directory.Exists(rootFolder))
        {
            return files.Select(file => new OrganizationResult
            {
                SourcePath = file.FullPath,
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
        var rule = FindMatchingRule(file.Extension, rules);
        if (rule is null)
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
        catch (UnauthorizedAccessException ex)
        {
            return CreateFailureResult(file.FullPath, destinationPath, ex.Message);
        }
        catch (IOException ex)
        {
            return CreateFailureResult(file.FullPath, destinationPath, ex.Message);
        }
        catch (Exception ex)
        {
            return CreateFailureResult(file.FullPath, destinationPath, ex.Message);
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
        string extension,
        IReadOnlyList<OrganizationRule> rules)
    {
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
