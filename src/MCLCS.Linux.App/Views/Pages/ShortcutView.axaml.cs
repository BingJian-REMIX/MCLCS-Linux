using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class ShortcutView : UserControl
{
    public ShortcutView()
    {
        InitializeComponent();
        DataContext = new ShortcutViewModel();
    }
}
