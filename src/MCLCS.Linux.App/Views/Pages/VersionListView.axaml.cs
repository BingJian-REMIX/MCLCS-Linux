using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class VersionListView : UserControl
{
    public VersionListView()
    {
        InitializeComponent();
        DataContext = new VersionListViewModel();
    }
}
