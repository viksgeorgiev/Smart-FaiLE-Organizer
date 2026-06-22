using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartFileOrganizer.Models;
using SmartFileOrganizer.Services;

namespace SmartFileOrganizer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly JsonStorageService _jsonStorageService;
    private readonly RuleService _ruleService;
    private readonly FileScannerService _fileScannerService;
    private readonly FileOrganizerService _fileOrganizerService;
    private readonly LoggingService _loggingService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(OrganizeFilesCommand))]
    private string _selectedFolder = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Select a folder to begin.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteRuleCommand))]
    private OrganizationRule? _selectedRule;

    public MainViewModel()
    {
        _jsonStorageService = new JsonStorageService();
        _ruleService = new RuleService(_jsonStorageService);
        _fileScannerService = new FileScannerService();
        _fileOrganizerService = new FileOrganizerService();
        _loggingService = new LoggingService(_jsonStorageService);
        SelectedFolderCommand = new RelayCommand(OnSelectedFolder);
        _ = InitializeAsync();
    }

    public ObservableCollection<OrganizationRule> Rules { get; } = new();
    public ObservableCollection<PreviewItem> Files { get; } = new();
    public ObservableCollection<HistoryEntry> History { get; } = new();

    public IRelayCommand SelectedFolderCommand { get; }

    [RelayCommand(CanExecute = nameof(CanScanFolder))]
    private async Task ScanFolder()
    {
        if (!CanScanFolder())
        {
            StatusMessage = "Select a valid folder first.";
            return;
        }

        StatusMessage = "Scanning folder...";

        var scannedFiles = await Task.Run(() => _fileScannerService.Scan(SelectedFolder));

        Files.Clear();

        foreach (var file in scannedFiles)
        {
            var rule = FindMatchingRule(file.Extension);
            var destinationFolder = rule?.DestinationFolder ?? string.Empty;

            Files.Add(new PreviewItem
            {
                FileName = file.Name,
                SourcePath = file.FullPath,
                DestinationFolder = destinationFolder,
                DestinationPath = rule is not null
                    ? Path.Combine(SelectedFolder, destinationFolder, file.Name)
                    : string.Empty,
                Status = rule is not null ? "Ready" : "No Rule"
            });
        }

        var matchedCount = Files.Count(file => file.Status == "Ready");
        StatusMessage = $"Found {Files.Count} file(s). {matchedCount} matched by rules.";
        OrganizeFilesCommand.NotifyCanExecuteChanged();
    }

    private bool CanScanFolder() =>
        !string.IsNullOrWhiteSpace(SelectedFolder) && Directory.Exists(SelectedFolder);

    [RelayCommand(CanExecute = nameof(CanOrganizeFiles))]
    private async Task OrganizeFiles()
    {
        var previewFiles = Files.Where(file => file.Status == "Ready").ToList();
        if (previewFiles.Count == 0)
        {
            StatusMessage = "No files ready to organize. Scan a folder first.";
            return;
        }

        StatusMessage = "Organizing files...";

        var fileItems = previewFiles.Select(preview => new FileItem
        {
            Name = preview.FileName,
            FullPath = preview.SourcePath,
            Extension = Path.GetExtension(preview.SourcePath).ToLowerInvariant()
        }).ToList();

        var results = await Task.Run(() =>
            _fileOrganizerService.Organize(fileItems, Rules.ToList(), SelectedFolder));

        await _loggingService.LogMovesAsync(fileItems, results);

        for (var i = 0; i < results.Count; i++)
        {
            var result = results[i];
            var preview = previewFiles[i];

            History.Insert(0, new HistoryEntry
            {
                Timestamp = DateTime.Now,
                SourcePath = result.SourcePath,
                DestinationPath = result.DestinationPath,
                Status = result.Success ? "Success" : "Failed",
                Message = result.Message
            });

            preview.Status = result.Success ? "Moved" : "Failed";
        }

        var successCount = results.Count(result => result.Success);
        StatusMessage = $"Organizing complete. {successCount} of {results.Count} file(s) moved successfully.";

        System.Windows.MessageBox.Show(
            $"Organizing complete.\n{successCount} of {results.Count} file(s) moved successfully.",
            "Smart File Organizer",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);

        OrganizeFilesCommand.NotifyCanExecuteChanged();
    }

    private bool CanOrganizeFiles() =>
        CanScanFolder() && Files.Any(file => file.Status == "Ready");

    [RelayCommand]
    private async Task AddRule()
    {
        var extensionInput = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter a file extension (e.g. .pdf or pdf):",
            "Add Rule",
            ".pdf");

        if (string.IsNullOrWhiteSpace(extensionInput))
        {
            return;
        }

        var destinationInput = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter the destination folder name (e.g. Documents):",
            "Add Rule",
            "Documents");

        if (string.IsNullOrWhiteSpace(destinationInput))
        {
            return;
        }

        var normalizedExtension = NormalizeExtension(extensionInput);
        var destinationFolder = destinationInput.Trim();

        if (destinationFolder.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            System.Windows.MessageBox.Show(
                "Destination folder name contains invalid characters.",
                "Add Rule",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (Rules.Any(rule => NormalizeExtension(rule.Extension) == normalizedExtension))
        {
            System.Windows.MessageBox.Show(
                $"A rule for extension '{normalizedExtension}' already exists.",
                "Duplicate Rule",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        Rules.Add(new OrganizationRule
        {
            Extension = normalizedExtension,
            DestinationFolder = destinationFolder
        });

        await SaveRulesAsync();
        StatusMessage = $"Rule added: {normalizedExtension} -> {destinationFolder}";
    }

    [RelayCommand(CanExecute = nameof(CanDeleteRule))]
    private async Task DeleteRule()
    {
        if (SelectedRule is null)
        {
            return;
        }

        var extension = SelectedRule.Extension;
        Rules.Remove(SelectedRule);
        SelectedRule = null;

        await SaveRulesAsync();
        StatusMessage = $"Rule deleted: {extension}";
    }

    private bool CanDeleteRule() => SelectedRule is not null;

    private async Task InitializeAsync()
    {
        await LoadRulesAsync();
        await LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            await _loggingService.LoadAsync();

            History.Clear();
            foreach (var entry in _loggingService.Entries.OrderByDescending(entry => entry.Timestamp))
            {
                History.Add(ToHistoryEntry(entry));
            }
        }
        catch (Exception)
        {
            StatusMessage = "Could not load history.";
        }
    }

    private static HistoryEntry ToHistoryEntry(LogEntry entry)
    {
        return new HistoryEntry
        {
            Timestamp = entry.Timestamp,
            SourcePath = entry.SourcePath,
            DestinationPath = entry.DestinationPath,
            Status = entry.Success ? "Success" : "Failed",
            Message = entry.Success ? "File moved successfully." : "Move failed."
        };
    }

    private async Task LoadRulesAsync()
    {
        try
        {
            var rules = await _ruleService.LoadRulesAsync();

            Rules.Clear();
            foreach (var rule in rules)
            {
                Rules.Add(rule);
            }

            if (Rules.Count > 0)
            {
                StatusMessage = $"Loaded {Rules.Count} saved rule(s).";
            }
        }
        catch (Exception)
        {
            StatusMessage = "Could not load saved rules.";
        }
    }

    private async Task SaveRulesAsync()
    {
        await _ruleService.SaveRulesAsync(Rules);
    }

    private OrganizationRule? FindMatchingRule(string extension)
    {
        var normalizedExtension = NormalizeExtension(extension);
        return Rules.FirstOrDefault(rule => NormalizeExtension(rule.Extension) == normalizedExtension);
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

    private void OnSelectedFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select a folder to organize",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true
        };

        if (!string.IsNullOrWhiteSpace(SelectedFolder) && Directory.Exists(SelectedFolder))
        {
            dialog.SelectedPath = SelectedFolder;
        }

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            SelectedFolder = dialog.SelectedPath;
            StatusMessage = "Folder selected.";
        }
    }
}
