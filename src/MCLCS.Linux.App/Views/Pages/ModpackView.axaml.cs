using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class ModpackView : UserControl
{
    public ModpackView()
    {
        InitializeComponent();
        DataContext = new ModpackViewModel();
    }
}
