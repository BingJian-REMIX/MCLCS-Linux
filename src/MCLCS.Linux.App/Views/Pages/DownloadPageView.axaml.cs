using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class DownloadPageView : UserControl
{
    public DownloadPageView()
    {
        InitializeComponent();
        DataContext = DownloadPageViewModel.Instance;
        if (MainViewModel.Instance is { } mv && !string.IsNullOrEmpty(mv.SelectedSidebarId))
            DownloadPageViewModel.Instance.SetSubTab(mv.SelectedSidebarId);
    }
}
