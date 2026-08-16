using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class ModpackIoView : UserControl
{
    public ModpackIoView()
    {
        InitializeComponent();
        DataContext = new ModpackIoViewModel();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
