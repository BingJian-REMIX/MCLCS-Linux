using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class FileWatchView : UserControl
{
    public FileWatchView()
    {
        InitializeComponent();
        DataContext = new FileWatchViewModel();
    }
}
