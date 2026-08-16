using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class BackupView : UserControl
{
    public BackupView()
    {
        InitializeComponent();
        DataContext = new BackupViewModel();
    }
}
