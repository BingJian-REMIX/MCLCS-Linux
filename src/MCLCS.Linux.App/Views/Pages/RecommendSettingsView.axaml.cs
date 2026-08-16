using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class RecommendSettingsView : UserControl
{
    public RecommendSettingsView()
    {
        InitializeComponent();
        DataContext = new RecommendSettingsViewModel();
    }
}
