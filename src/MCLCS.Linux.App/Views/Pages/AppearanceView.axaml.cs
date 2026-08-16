using Avalonia.Controls;
using Avalonia.Interactivity;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

/// <summary>设置 → 外观页：四色主题编辑器，读写 Core.TabThemeConfig 并实时刷新。</summary>
public partial class AppearanceView : UserControl
{
    public AppearanceView()
    {
        InitializeComponent();
        DataContext = MainViewModel.Instance ?? new MainViewModel();
    }

    private void ThemeColor_Changed(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox tb || tb.Tag is not string tag || MainViewModel.Instance is not { } vm)
            return;
        switch (tag)
        {
            case "game": vm.Theme.Game = tb.Text ?? ""; break;
            case "download": vm.Theme.Download = tb.Text ?? ""; break;
            case "toolbox": vm.Theme.Toolbox = tb.Text ?? ""; break;
            case "settings": vm.Theme.Settings = tb.Text ?? ""; break;
        }
        vm.RefreshTabs();
    }
}
