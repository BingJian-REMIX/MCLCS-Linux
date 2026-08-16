using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class PerfView : UserControl
{
    public PerfView()
    {
        InitializeComponent();
        DataContext = new PerfViewModel();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
