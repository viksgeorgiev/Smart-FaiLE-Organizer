using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartFileOrganizer.Models;

namespace SmartFileOrganizer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _selectedFolder = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Select a folder to begin.";

    public MainViewModel()
    {
        SelectedFolderCommand = new RelayCommand(OnSelectedFolder);
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
    private void AddRule()
    {
        // TODO: implement add rule
    }

    [RelayCommand]
    private void DeleteRule()
    {
        // TODO: implement delete rule
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
