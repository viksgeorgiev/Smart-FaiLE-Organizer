using System.Collections.ObjectModel;
using System.Windows.Input;
using SmartFileOrganizer.Commands;
using SmartFileOrganizer.Models;

namespace SmartFileOrganizer.ViewModels;

public class MainViewModel : ViewModelBase
{
    private string _selectedFolderPath = string.Empty;
    private string _statusMessage = "Select a folder to begin.";

    public MainViewModel()
    {
        Rules = new ObservableCollection<OrganizationRule>();
        PreviewItems = new ObservableCollection<PreviewItem>();
        History = new ObservableCollection<HistoryEntry>();

        SelectFolderCommand = new RelayCommand(SelectFolder);
        OrganizeCommand = new RelayCommand(OrganizeFiles);
    }

    public string SelectedFolderPath
    {
        get => _selectedFolderPath;
        set => SetProperty(ref _selectedFolderPath, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ObservableCollection<OrganizationRule> Rules { get; }
    public ObservableCollection<PreviewItem> PreviewItems { get; }
    public ObservableCollection<HistoryEntry> History { get; }

    public ICommand SelectFolderCommand { get; }
    public ICommand OrganizeCommand { get; }

    private void SelectFolder()
    {
        // TODO: implement folder selection
    }

    private void OrganizeFiles()
    {
        // TODO: implement file organization
    }
}
