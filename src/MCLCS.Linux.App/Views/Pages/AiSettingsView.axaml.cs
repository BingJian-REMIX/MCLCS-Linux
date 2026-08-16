using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class AiSettingsView : UserControl
{
    public AiSettingsView()
    {
        InitializeComponent();
        DataContext = new AiSettingsViewModel();
    }
}
