using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class SkinView : UserControl
{
    public SkinView()
    {
        InitializeComponent();
        DataContext = new SkinViewModel();
    }
}
