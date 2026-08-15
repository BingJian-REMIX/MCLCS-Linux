using Avalonia.Controls;
using Avalonia.Interactivity;
using MCLCS.Core.UI;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        var vm = new MainViewModel();
        MainViewModel.Instance = vm;
        DataContext = vm;
        InitializeComponent();
    }

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

    private void Sidebar_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: SidebarItem item } && DataContext is MainViewModel vm)
            vm.SelectedSidebarId = item.Id;
    }

    /// <summary>主题色输入框实时改色：手写回 Core.TabThemeConfig 后刷新主标签颜色。</summary>
    private void ThemeColor_Changed(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox tb || tb.Tag is not string tag || DataContext is not MainViewModel vm)
            return;
        switch (tag)
        {
            case "game": vm.Theme.Game = tb.Text; break;
            case "download": vm.Theme.Download = tb.Text; break;
            case "toolbox": vm.Theme.Toolbox = tb.Text; break;
            case "settings": vm.Theme.Settings = tb.Text; break;
        }
        vm.RefreshTabs();
    }
}
