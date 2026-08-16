using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class ShaderView : UserControl
{
    public ShaderView()
    {
        InitializeComponent();
        DataContext = new ShaderViewModel();
    }
}
