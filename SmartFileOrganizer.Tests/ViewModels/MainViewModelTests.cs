using Moq;
using SmartFileOrganizer.Models;
using SmartFileOrganizer.Services;
using SmartFileOrganizer.Tests.Helpers;
using SmartFileOrganizer.ViewModels;

namespace SmartFileOrganizer.Tests.ViewModels;

public class MainViewModelTests
{
    [Fact]
    public async Task AddRuleCommand_DoesNotAddDuplicateExtension()
    {
        // Arrange
        var ruleService = new Mock<IRuleService>();
        ruleService.Setup(service => service.SaveRulesAsync(It.IsAny<IEnumerable<OrganizationRule>>()))
            .Returns(Task.CompletedTask);

        var dialogService = new Mock<IRuleDialogService>();
        dialogService.Setup(service => service.TryGetRuleInput(out It.Ref<string>.IsAny, out It.Ref<string>.IsAny))
            .Returns((out string extension, out string destination) =>
            {
                extension = ".PDF";
                destination = "Documents";
                return true;
            });

        var viewModel = CreateViewModel(ruleService: ruleService.Object, dialogService: dialogService.Object);
        viewModel.Rules.Add(new OrganizationRule { Extension = ".pdf", DestinationFolder = "Docs" });

        // Act
        await viewModel.AddRuleCommand.ExecuteAsync(null);

        // Assert
        Assert.Single(viewModel.Rules);
        ruleService.Verify(service => service.SaveRulesAsync(It.IsAny<IEnumerable<OrganizationRule>>()), Times.Never);
    }

    [Fact]
    public async Task DeleteRuleCommand_RemovesSelectedRule()
    {
        // Arrange
        var ruleService = new Mock<IRuleService>();
        ruleService.Setup(service => service.SaveRulesAsync(It.IsAny<IEnumerable<OrganizationRule>>()))
            .Returns(Task.CompletedTask);

        var viewModel = CreateViewModel(ruleService: ruleService.Object);
        var rule = new OrganizationRule { Extension = ".txt", DestinationFolder = "Text" };
        viewModel.Rules.Add(rule);
        viewModel.SelectedRule = rule;

        // Act
        await viewModel.DeleteRuleCommand.ExecuteAsync(null);

        // Assert
        Assert.Empty(viewModel.Rules);
        Assert.Null(viewModel.SelectedRule);
        ruleService.Verify(service => service.SaveRulesAsync(viewModel.Rules), Times.Once);
    }

    [Fact]
    public async Task ScanFolderCommand_PopulatesFilesCollection()
    {
        // Arrange
        using var temp = new TempDirectory();
        var scanner = new Mock<IFileScannerService>();
        scanner.Setup(service => service.Scan(temp.Path))
            .Returns(
            [
                new FileItem
                {
                    Name = "report.pdf",
                    FullPath = System.IO.Path.Combine(temp.Path, "report.pdf"),
                    Extension = ".pdf",
                    Size = 10
                }
            ]);

        var viewModel = CreateViewModel(scanner: scanner.Object);
        viewModel.SelectedFolder = temp.Path;
        viewModel.Rules.Add(new OrganizationRule { Extension = ".pdf", DestinationFolder = "Documents" });

        // Act
        await viewModel.ScanFolderCommand.ExecuteAsync(null);

        // Assert
        var preview = Assert.Single(viewModel.Files);
        Assert.Equal("report.pdf", preview.FileName);
        Assert.Equal("Documents", preview.DestinationFolder);
        Assert.Equal("Ready", preview.Status);
    }

    [Fact]
    public async Task OrganizeFilesCommand_CallsOrganizerService()
    {
        // Arrange
        using var temp = new TempDirectory();
        var sourcePath = System.IO.Path.Combine(temp.Path, "report.pdf");
        var destinationPath = System.IO.Path.Combine(temp.Path, "Documents", "report.pdf");

        var organizer = new Mock<IFileOrganizerService>();
        organizer.Setup(service => service.Organize(It.IsAny<IReadOnlyList<FileItem>>(), It.IsAny<IReadOnlyList<OrganizationRule>>(), temp.Path))
            .Returns(
            [
                new OrganizationResult
                {
                    SourcePath = sourcePath,
                    DestinationPath = destinationPath,
                    Success = true,
                    Message = "File moved successfully."
                }
            ]);

        var logging = new Mock<ILoggingService>();
        logging.Setup(service => service.LogMovesAsync(It.IsAny<IReadOnlyList<FileItem>>(), It.IsAny<IReadOnlyList<OrganizationResult>>()))
            .Returns(Task.CompletedTask);

        var viewModel = CreateViewModel(organizer: organizer.Object, logging: logging.Object);
        viewModel.SelectedFolder = temp.Path;
        viewModel.Rules.Add(new OrganizationRule { Extension = ".pdf", DestinationFolder = "Documents" });
        viewModel.Files.Add(new PreviewItem
        {
            FileName = "report.pdf",
            SourcePath = sourcePath,
            DestinationFolder = "Documents",
            DestinationPath = destinationPath,
            Status = "Ready"
        });

        // Act
        await viewModel.OrganizeFilesCommand.ExecuteAsync(null);

        // Assert
        organizer.Verify(
            service => service.Organize(It.IsAny<IReadOnlyList<FileItem>>(), It.IsAny<IReadOnlyList<OrganizationRule>>(), temp.Path),
            Times.Once);
        logging.Verify(
            service => service.LogMovesAsync(It.IsAny<IReadOnlyList<FileItem>>(), It.IsAny<IReadOnlyList<OrganizationResult>>()),
            Times.Once);
        Assert.Single(viewModel.History);
        Assert.Equal("Success", viewModel.History[0].Status);
    }

    private static MainViewModel CreateViewModel(
        IFileScannerService? scanner = null,
        IFileOrganizerService? organizer = null,
        IRuleService? ruleService = null,
        ILoggingService? logging = null,
        IRuleDialogService? dialogService = null)
    {
        return new MainViewModel(
            scanner ?? new Mock<IFileScannerService>().Object,
            organizer ?? new Mock<IFileOrganizerService>().Object,
            ruleService ?? new Mock<IRuleService>().Object,
            logging ?? new Mock<ILoggingService>().Object,
            dialogService ?? new Mock<IRuleDialogService>().Object,
            loadOnStartup: false,
            suppressDialogs: true);
    }
}
