using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

/// <summary>设置 → 外观页：四色主题编辑器 + 主题色/背景图/字体缩放，均读写 Core.LauncherProfile 并实时刷新。</summary>
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
        vm.PersistProfile(); // 四色标签色真正持久化到 profile（此前仅实时生效、重启即丢）
    }

    private async void BrowseBackground_Click(object? sender, RoutedEventArgs e)
    {
        if (MainViewModel.Instance is not { } vm) return;
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is not { } sp) return;
        var files = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择背景图片",
            FileTypeFilter = new[]
            {
                new FilePickerFileType("图片")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.webp" }
                }
            }
        });
        if (files.Count > 0)
            vm.BackgroundImagePath = files[0].Path.LocalPath;
    }
}
