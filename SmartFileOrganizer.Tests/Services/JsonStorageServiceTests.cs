using SmartFileOrganizer.Models;
using SmartFileOrganizer.Services;
using SmartFileOrganizer.Tests.Helpers;

namespace SmartFileOrganizer.Tests.Services;

public class JsonStorageServiceTests
{
    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsRulesCollection()
    {
        // Arrange
        using var temp = new TempDirectory();
        var filePath = System.IO.Path.Combine(temp.Path, "rules.json");
        var storage = new JsonStorageService();
        var rules = new List<OrganizationRule>
        {
            new() { Extension = ".pdf", DestinationFolder = "Documents" },
            new() { Extension = ".jpg", DestinationFolder = "Images" }
        };

        // Act
        await storage.SaveAsync(filePath, rules);
        var loaded = await storage.LoadAsync<OrganizationRule>(filePath);

        // Assert
        Assert.Equal(2, loaded.Count);
        Assert.Contains(loaded, rule => rule.Extension == ".pdf" && rule.DestinationFolder == "Documents");
        Assert.Contains(loaded, rule => rule.Extension == ".jpg" && rule.DestinationFolder == "Images");
    }

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsHistoryCollection()
    {
        // Arrange
        using var temp = new TempDirectory();
        var filePath = System.IO.Path.Combine(temp.Path, "history.json");
        var storage = new JsonStorageService();
        var history = new List<HistoryEntry>
        {
            new()
            {
                Timestamp = new DateTime(2026, 6, 22, 10, 0, 0),
                SourcePath = @"C:\source\a.pdf",
                DestinationPath = @"C:\dest\a.pdf",
                Status = "Success",
                Message = "File moved successfully."
            },
            new()
            {
                Timestamp = new DateTime(2026, 6, 22, 10, 5, 0),
                SourcePath = @"C:\source\b.txt",
                DestinationPath = string.Empty,
                Status = "Failed",
                Message = "No matching rule."
            }
        };

        // Act
        await storage.SaveAsync(filePath, history);
        var loaded = await storage.LoadAsync<HistoryEntry>(filePath);

        // Assert
        Assert.Equal(2, loaded.Count);
        Assert.Equal("Success", loaded[0].Status);
        Assert.Equal("Failed", loaded[1].Status);
    }

    [Fact]
    public async Task LoadAsync_ReturnsEmptyList_WhenFileDoesNotExist()
    {
        // Arrange
        using var temp = new TempDirectory();
        var filePath = System.IO.Path.Combine(temp.Path, "missing.json");
        var storage = new JsonStorageService();

        // Act
        var loaded = await storage.LoadAsync<OrganizationRule>(filePath);

        // Assert
        Assert.Empty(loaded);
    }

    [Fact]
    public async Task LoadAsync_ReturnsEmptyList_WhenJsonIsInvalid()
    {
        // Arrange
        using var temp = new TempDirectory();
        var filePath = System.IO.Path.Combine(temp.Path, "invalid.json");
        await File.WriteAllTextAsync(filePath, "{ not valid json");
        var storage = new JsonStorageService();

        // Act
        var loaded = await storage.LoadAsync<OrganizationRule>(filePath);

        // Assert
        Assert.Empty(loaded);
    }
}
