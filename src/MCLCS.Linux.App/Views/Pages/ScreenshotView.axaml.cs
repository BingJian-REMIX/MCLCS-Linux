using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class ScreenshotView : UserControl
{
    public ScreenshotView()
    {
        InitializeComponent();
        DataContext = new ScreenshotViewModel();
    }
}
