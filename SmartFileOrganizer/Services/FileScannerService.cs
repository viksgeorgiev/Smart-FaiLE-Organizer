using System.IO;
using SmartFileOrganizer.Models;

namespace SmartFileOrganizer.Services;

public class FileScannerService
{
    public List<FileItem> Scan(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return [];
        }

        var files = new List<FileItem>();
        ScanDirectory(folderPath, files);
        return files;
    }

    private static void ScanDirectory(string directoryPath, List<FileItem> files)
    {
        foreach (var filePath in EnumerateFilesSafely(directoryPath))
        {
            TryAddFile(filePath, files);
        }

        foreach (var subDirectory in EnumerateDirectoriesSafely(directoryPath))
        {
            ScanDirectory(subDirectory, files);
        }
    }

    private static IEnumerable<string> EnumerateFilesSafely(string directoryPath)
    {
        try
        {
            return Directory.EnumerateFiles(directoryPath);
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
        catch (DirectoryNotFoundException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }

    private static IEnumerable<string> EnumerateDirectoriesSafely(string directoryPath)
    {
        try
        {
            return Directory.EnumerateDirectories(directoryPath);
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
        catch (DirectoryNotFoundException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }

    private static void TryAddFile(string filePath, List<FileItem> files)
    {
        try
        {
            var info = new FileInfo(filePath);

            files.Add(new FileItem
            {
                Name = info.Name,
                Extension = info.Extension.ToLowerInvariant(),
                FullPath = info.FullName,
                Size = info.Length,
                LastModified = info.LastWriteTime
            });
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (IOException)
        {
        }
    }
}
