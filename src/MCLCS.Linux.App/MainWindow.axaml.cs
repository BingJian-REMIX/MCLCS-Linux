using Avalonia.Controls;
using Avalonia.Interactivity;
using MCLCS.Core.UI;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private async void DetectJava_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            await vm.DetectJavaAsync();
    }

    private void Tab_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: MainTabDefinition tab } && DataContext is MainViewModel vm)
            vm.SelectedTab = tab;
    }
}
