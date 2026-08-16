using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class PackMakerView : UserControl
{
    public PackMakerView()
    {
        InitializeComponent();
        DataContext = new PackMakerViewModel();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
