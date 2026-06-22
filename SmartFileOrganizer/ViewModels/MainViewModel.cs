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
    private readonly RuleService _ruleService;

    [ObservableProperty]
    private string _selectedFolder = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Select a folder to begin.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteRuleCommand))]
    private OrganizationRule? _selectedRule;

    public MainViewModel()
    {
        _ruleService = new RuleService(new JsonStorageService());
        SelectedFolderCommand = new RelayCommand(OnSelectedFolder);
        _ = LoadRulesAsync();
    }

    public ObservableCollection<OrganizationRule> Rules { get; } = new();
    public ObservableCollection<PreviewItem> Files { get; } = new();
    public ObservableCollection<HistoryEntry> History { get; } = new();

    public IRelayCommand SelectedFolderCommand { get; }

    [RelayCommand]
    private void ScanFolder()
    {
        // TODO: implement folder scanning
    }

    [RelayCommand]
    private void OrganizeFiles()
    {
        // TODO: implement file organization
    }

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
