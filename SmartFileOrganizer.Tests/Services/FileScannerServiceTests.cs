using SmartFileOrganizer.Models;
using SmartFileOrganizer.Services;
using SmartFileOrganizer.Tests.Helpers;

namespace SmartFileOrganizer.Tests.Services;

public class FileScannerServiceTests
{
    [Fact]
    public void Scan_ReturnsFileItems_WhenFolderContainsFiles()
    {
        // Arrange
        using var temp = new TempDirectory();
        var txtPath = System.IO.Path.Combine(temp.Path, "a.txt");
        var pdfPath = System.IO.Path.Combine(temp.Path, "b.pdf");
        File.WriteAllText(txtPath, "hello");
        File.WriteAllText(pdfPath, "pdf-content");
        var service = new FileScannerService();

        // Act
        var results = service.Scan(temp.Path);

        // Assert
        Assert.Equal(2, results.Count);
        var txt = Assert.Single(results, item => item.Name == "a.txt");
        Assert.Equal(".txt", txt.Extension);
        Assert.Equal(txtPath, txt.FullPath);
        Assert.True(txt.Size > 0);
    }

    [Fact]
    public void Scan_ReturnsEmptyList_WhenFolderIsEmpty()
    {
        // Arrange
        using var temp = new TempDirectory();
        var service = new FileScannerService();

        // Act
        var results = service.Scan(temp.Path);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Scan_ReturnsEmptyList_WhenFolderPathIsInvalid()
    {
        // Arrange
        var service = new FileScannerService();

        // Act
        var missingPathResults = service.Scan(System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString()));
        var nullResults = service.Scan(null);
        var emptyResults = service.Scan(string.Empty);

        // Assert
        Assert.Empty(missingPathResults);
        Assert.Empty(nullResults);
        Assert.Empty(emptyResults);
    }

    [Fact]
    public void Scan_IncludesFilesFromSubfolders()
    {
        // Arrange
        using var temp = new TempDirectory();
        var subFolder = System.IO.Path.Combine(temp.Path, "Nested");
        Directory.CreateDirectory(subFolder);
        var nestedFile = System.IO.Path.Combine(subFolder, "nested.docx");
        File.WriteAllText(nestedFile, "nested");
        var service = new FileScannerService();

        // Act
        var results = service.Scan(temp.Path);

        // Assert
        var file = Assert.Single(results);
        Assert.Equal("nested.docx", file.Name);
        Assert.Equal(nestedFile, file.FullPath);
    }
}
