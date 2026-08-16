using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class ModDevView : UserControl
{
    public ModDevView()
    {
        InitializeComponent();
        DataContext = new ModDevViewModel();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
