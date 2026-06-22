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
    private readonly IFileScannerService _fileScannerService;
    private readonly IFileOrganizerService _fileOrganizerService;
    private readonly IRuleService _ruleService;
    private readonly ILoggingService _loggingService;
    private readonly IRuleDialogService _ruleDialogService;
    private readonly bool _suppressDialogs;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(OrganizeFilesCommand))]
    private string _selectedFolder = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Select a folder to begin.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteRuleCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditRuleCommand))]
    private OrganizationRule? _selectedRule;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(OrganizeFilesCommand))]
    private bool _isScanning;

    public MainViewModel()
        : this(CreateDefaultScanner(), CreateDefaultOrganizer(), CreateDefaultRuleService(), CreateDefaultLoggingService(), new RuleDialogService(), loadOnStartup: true, suppressDialogs: false)
    {
    }

    public MainViewModel(
        IFileScannerService fileScannerService,
        IFileOrganizerService fileOrganizerService,
        IRuleService ruleService,
        ILoggingService loggingService,
        IRuleDialogService ruleDialogService,
        bool loadOnStartup = false,
        bool suppressDialogs = true)
    {
        _fileScannerService = fileScannerService;
        _fileOrganizerService = fileOrganizerService;
        _ruleService = ruleService;
        _loggingService = loggingService;
        _ruleDialogService = ruleDialogService;
        _suppressDialogs = suppressDialogs;
        SelectedFolderCommand = new RelayCommand(OnSelectedFolder);

        if (loadOnStartup)
        {
            _ = InitializeAsync();
        }
    }

    private static FileScannerService CreateDefaultScanner() => new();

    private static FileOrganizerService CreateDefaultOrganizer() => new();

    private static RuleService CreateDefaultRuleService()
    {
        var storage = new JsonStorageService();
        return new RuleService(storage);
    }

    private static LoggingService CreateDefaultLoggingService()
    {
        var storage = new JsonStorageService();
        return new LoggingService(storage);
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
            ShowWarning("Please select a valid folder before scanning.");
            return;
        }

        try
        {
            IsScanning = true;
            StatusMessage = "Scanning folder...";

            var scannedFiles = await Task.Run(() => _fileScannerService.Scan(SelectedFolder));

            Files.Clear();

            foreach (var file in scannedFiles)
            {
                if (string.IsNullOrWhiteSpace(file.FullPath))
                {
                    continue;
                }

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
        }
        catch (Exception)
        {
            StatusMessage = "Could not scan this folder. Please check permissions.";
            ShowWarning("Could not scan this folder. Please check permissions and try again.");
        }
        finally
        {
            IsScanning = false;
            OrganizeFilesCommand.NotifyCanExecuteChanged();
        }
    }

    private bool HasValidSelectedFolder() =>
        !string.IsNullOrWhiteSpace(SelectedFolder) && Directory.Exists(SelectedFolder);

    private bool CanScanFolder() =>
        !IsScanning && HasValidSelectedFolder();

    [RelayCommand(CanExecute = nameof(CanOrganizeFiles))]
    private async Task OrganizeFiles()
    {
        if (!HasValidSelectedFolder())
        {
            StatusMessage = "Select a valid folder first.";
            ShowWarning("Please select a valid folder before organizing files.");
            return;
        }

        var previewFiles = Files.Where(file => file.Status == "Ready").ToList();
        if (previewFiles.Count == 0)
        {
            StatusMessage = "No files ready to organize. Scan a folder first.";
            ShowWarning("No files are ready to organize. Scan a folder and add matching rules first.");
            return;
        }

        try
        {
            StatusMessage = "Organizing files...";

            var fileItems = previewFiles
                .Where(preview => !string.IsNullOrWhiteSpace(preview.SourcePath))
                .Select(preview => new FileItem
                {
                    Name = preview.FileName,
                    FullPath = preview.SourcePath,
                    Extension = Path.GetExtension(preview.SourcePath).ToLowerInvariant()
                })
                .ToList();

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

            if (!_suppressDialogs)
            {
                System.Windows.MessageBox.Show(
                    $"Organizing complete.\n{successCount} of {results.Count} file(s) moved successfully.",
                    "Smart File Organizer",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }

            OrganizeFilesCommand.NotifyCanExecuteChanged();
            ClearHistoryCommand.NotifyCanExecuteChanged();
        }
        catch (Exception)
        {
            StatusMessage = "Could not organize files. Some files may be locked or inaccessible.";
            ShowWarning("Could not organize files. Some files may be locked or inaccessible.");
        }
    }

    private bool CanOrganizeFiles() =>
        !IsScanning &&
        HasValidSelectedFolder() &&
        Files.Any(file => file.Status == "Ready");

    [RelayCommand]
    private async Task AddRule()
    {
        try
        {
            if (!_ruleDialogService.TryGetRuleInput(out var extensionInput, out var destinationInput))
            {
                return;
            }

            if (!TryNormalizeExtension(extensionInput, out var normalizedExtension, out var extensionError))
            {
                ShowWarning(extensionError, "Add Rule");
                return;
            }

            if (!TryValidateDestinationFolder(destinationInput, out var destinationFolder, out var destinationError))
            {
                ShowWarning(destinationError, "Add Rule");
                return;
            }

            if (Rules.Any(rule => NormalizeExtension(rule.Extension) == normalizedExtension))
            {
                ShowWarning($"A rule for extension '{normalizedExtension}' already exists.", "Duplicate Rule");
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
        catch (Exception)
        {
            StatusMessage = "Could not save rules. Please try again.";
            ShowWarning("Could not save rules. Please try again.");
        }
    }

    [RelayCommand(CanExecute = nameof(CanEditRule))]
    private async Task EditRule()
    {
        if (SelectedRule is null)
        {
            return;
        }

        try
        {
            var ruleToEdit = SelectedRule;

            if (!_ruleDialogService.TryGetRuleInput(
                    out var extensionInput,
                    out var destinationInput,
                    defaultExtension: ruleToEdit.Extension,
                    defaultDestination: ruleToEdit.DestinationFolder,
                    title: "Edit Rule"))
            {
                return;
            }

            if (!TryNormalizeExtension(extensionInput, out var normalizedExtension, out var extensionError))
            {
                ShowWarning(extensionError, "Edit Rule");
                return;
            }

            if (!TryValidateDestinationFolder(destinationInput, out var destinationFolder, out var destinationError))
            {
                ShowWarning(destinationError, "Edit Rule");
                return;
            }

            if (Rules.Any(rule => rule != ruleToEdit && NormalizeExtension(rule.Extension) == normalizedExtension))
            {
                ShowWarning($"A rule for extension '{normalizedExtension}' already exists.", "Duplicate Rule");
                return;
            }

            var index = Rules.IndexOf(ruleToEdit);
            if (index < 0)
            {
                return;
            }

            var updatedRule = new OrganizationRule
            {
                Extension = normalizedExtension,
                DestinationFolder = destinationFolder
            };

            Rules[index] = updatedRule;
            SelectedRule = updatedRule;

            await SaveRulesAsync();
            StatusMessage = $"Rule updated: {normalizedExtension} -> {destinationFolder}";
        }
        catch (Exception)
        {
            StatusMessage = "Could not save rules. Please try again.";
            ShowWarning("Could not save rules. Please try again.");
        }
    }

    private bool CanEditRule() => SelectedRule is not null;

    [RelayCommand(CanExecute = nameof(CanDeleteRule))]
    private async Task DeleteRule()
    {
        if (SelectedRule is null)
        {
            return;
        }

        try
        {
            var extension = SelectedRule.Extension;
            Rules.Remove(SelectedRule);
            SelectedRule = null;

            await SaveRulesAsync();
            StatusMessage = $"Rule deleted: {extension}";
        }
        catch (Exception)
        {
            StatusMessage = "Could not save rules. Please try again.";
            ShowWarning("Could not delete the rule. Please try again.");
        }
    }

    private bool CanDeleteRule() => SelectedRule is not null;

    [RelayCommand(CanExecute = nameof(CanClearHistory))]
    private async Task ClearHistory()
    {
        var confirmation = System.Windows.MessageBox.Show(
            "Clear all history? This cannot be undone.",
            "Clear History",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (confirmation != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _loggingService.ClearAsync();
            History.Clear();
            StatusMessage = "History cleared.";
            ClearHistoryCommand.NotifyCanExecuteChanged();
        }
        catch (Exception)
        {
            StatusMessage = "Could not clear history. Please try again.";
            ShowWarning("Could not clear history. Please try again.");
        }
    }

    private bool CanClearHistory() => History.Count > 0;

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

            ClearHistoryCommand.NotifyCanExecuteChanged();
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
                if (string.IsNullOrWhiteSpace(rule.Extension) || string.IsNullOrWhiteSpace(rule.DestinationFolder))
                {
                    continue;
                }

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

    private OrganizationRule? FindMatchingRule(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return null;
        }

        var normalizedExtension = NormalizeExtension(extension);
        return Rules.FirstOrDefault(rule => NormalizeExtension(rule.Extension) == normalizedExtension);
    }

    private static bool TryNormalizeExtension(
        string? extensionInput,
        out string normalizedExtension,
        out string errorMessage)
    {
        normalizedExtension = string.Empty;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(extensionInput))
        {
            errorMessage = "Extension cannot be empty.";
            return false;
        }

        var trimmed = extensionInput.Trim().ToLowerInvariant();

        if (trimmed.Contains(' ') || trimmed.Contains('*') || trimmed.Contains('?') ||
            trimmed.Contains('/') || trimmed.Contains('\\'))
        {
            errorMessage = "Extension cannot contain spaces, wildcards, or path separators.";
            return false;
        }

        normalizedExtension = trimmed.StartsWith('.') ? trimmed : "." + trimmed;

        if (normalizedExtension.Length <= 1)
        {
            errorMessage = "Please enter a valid extension such as .pdf or pdf.";
            return false;
        }

        var extensionWithoutDot = normalizedExtension[1..];
        if (string.IsNullOrWhiteSpace(extensionWithoutDot) ||
            extensionWithoutDot.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            errorMessage = "Extension contains invalid characters.";
            return false;
        }

        return true;
    }

    private static bool TryValidateDestinationFolder(
        string? destinationInput,
        out string destinationFolder,
        out string errorMessage)
    {
        destinationFolder = string.Empty;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(destinationInput))
        {
            errorMessage = "Destination folder name cannot be empty.";
            return false;
        }

        destinationFolder = destinationInput.Trim();

        if (destinationFolder.Contains('/') || destinationFolder.Contains('\\'))
        {
            errorMessage = "Destination folder must be a simple folder name, not a path.";
            return false;
        }

        if (destinationFolder.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            errorMessage = "Destination folder name contains invalid characters.";
            return false;
        }

        return true;
    }

    private static string NormalizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        extension = extension.Trim().ToLowerInvariant();

        if (!extension.StartsWith('.'))
        {
            extension = "." + extension;
        }

        return extension;
    }

    private void ShowWarning(string message, string title = "Smart File Organizer")
    {
        if (_suppressDialogs)
        {
            return;
        }

        System.Windows.MessageBox.Show(
            message,
            title,
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Warning);
    }

    private void OnSelectedFolder()
    {
        try
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
                if (string.IsNullOrWhiteSpace(dialog.SelectedPath) || !Directory.Exists(dialog.SelectedPath))
                {
                    StatusMessage = "The selected folder is not valid.";
                    ShowWarning("The selected folder is not valid. Please choose another folder.");
                    return;
                }

                SelectedFolder = dialog.SelectedPath;
                StatusMessage = "Folder selected.";
            }
        }
        catch (Exception)
        {
            StatusMessage = "Could not open folder selection.";
            ShowWarning("Could not open folder selection. Please try again.");
        }
    }
}
