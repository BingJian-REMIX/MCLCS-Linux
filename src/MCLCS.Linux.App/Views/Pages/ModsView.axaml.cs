using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class ModsView : UserControl
{
    public ModsView()
    {
        InitializeComponent();
        DataContext = new ModsViewModel();
    }
}
