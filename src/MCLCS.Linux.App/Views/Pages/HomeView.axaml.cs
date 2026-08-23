using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using MCLCS.Core.UI;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
        DataContext = new HomeViewModel();
    }

    private void OpenAnnualReport_Click(object? sender, RoutedEventArgs e)
    {
        if (this.FindAncestorOfType<MainWindow>() is { } win)
            win.NavigateTo(MainTabKind.Toolbox, "annual");
    }
}
