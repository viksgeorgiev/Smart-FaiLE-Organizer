using SmartFileOrganizer.Models;
using SmartFileOrganizer.Services;
using SmartFileOrganizer.Tests.Helpers;

namespace SmartFileOrganizer.Tests.Services;

public class FileOrganizerServiceTests
{
    [Fact]
    public void Organize_MovesFileToRuleDestination_WhenRuleMatchesExtension()
    {
        // Arrange
        using var temp = new TempDirectory();
        var sourceFile = System.IO.Path.Combine(temp.Path, "report.pdf");
        File.WriteAllText(sourceFile, "pdf");
        var files = new List<FileItem>
        {
            new()
            {
                Name = "report.pdf",
                FullPath = sourceFile,
                Extension = ".pdf"
            }
        };
        var rules = new List<OrganizationRule>
        {
            new() { Extension = ".pdf", DestinationFolder = "Documents" }
        };
        var service = new FileOrganizerService();
        var expectedDestination = System.IO.Path.Combine(temp.Path, "Documents", "report.pdf");

        // Act
        var results = service.Organize(files, rules, temp.Path);

        // Assert
        var result = Assert.Single(results);
        Assert.True(result.Success);
        Assert.Equal(expectedDestination, result.DestinationPath);
        Assert.True(File.Exists(expectedDestination));
        Assert.False(File.Exists(sourceFile));
    }

    [Fact]
    public void Organize_CreatesDestinationFolder_WhenMissing()
    {
        // Arrange
        using var temp = new TempDirectory();
        var sourceFile = System.IO.Path.Combine(temp.Path, "photo.jpg");
        File.WriteAllText(sourceFile, "jpg");
        var destinationFolder = System.IO.Path.Combine(temp.Path, "Images");
        var service = new FileOrganizerService();

        // Act
        var results = service.Organize(
            [new FileItem { Name = "photo.jpg", FullPath = sourceFile, Extension = ".jpg" }],
            [new OrganizationRule { Extension = ".jpg", DestinationFolder = "Images" }],
            temp.Path);

        // Assert
        Assert.True(Directory.Exists(destinationFolder));
        Assert.True(results.Single().Success);
    }

    [Fact]
    public void Organize_ReturnsFailure_WhenNoMatchingRule()
    {
        // Arrange
        using var temp = new TempDirectory();
        var sourceFile = System.IO.Path.Combine(temp.Path, "notes.txt");
        File.WriteAllText(sourceFile, "text");
        var service = new FileOrganizerService();

        // Act
        var results = service.Organize(
            [new FileItem { Name = "notes.txt", FullPath = sourceFile, Extension = ".txt" }],
            [new OrganizationRule { Extension = ".pdf", DestinationFolder = "Documents" }],
            temp.Path);

        // Assert
        var result = Assert.Single(results);
        Assert.False(result.Success);
        Assert.Equal("No matching rule.", result.Message);
        Assert.True(File.Exists(sourceFile));
    }

    [Fact]
    public void Organize_ReturnsFailure_WhenDestinationFileAlreadyExists()
    {
        // Arrange
        using var temp = new TempDirectory();
        var sourceFile = System.IO.Path.Combine(temp.Path, "report.pdf");
        var destinationFolder = System.IO.Path.Combine(temp.Path, "Documents");
        Directory.CreateDirectory(destinationFolder);
        var destinationFile = System.IO.Path.Combine(destinationFolder, "report.pdf");
        File.WriteAllText(sourceFile, "source");
        File.WriteAllText(destinationFile, "existing");
        var service = new FileOrganizerService();

        // Act
        var results = service.Organize(
            [new FileItem { Name = "report.pdf", FullPath = sourceFile, Extension = ".pdf" }],
            [new OrganizationRule { Extension = ".pdf", DestinationFolder = "Documents" }],
            temp.Path);

        // Assert
        var result = Assert.Single(results);
        Assert.False(result.Success);
        Assert.Equal("Destination file already exists.", result.Message);
        Assert.True(File.Exists(sourceFile));
        Assert.Equal("existing", File.ReadAllText(destinationFile));
    }
}
