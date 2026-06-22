using System.Windows;
using SmartFileOrganizer.ViewModels;

namespace SmartFileOrganizer;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
