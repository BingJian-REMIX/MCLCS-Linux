using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using MCLCS.Core.UI;
using MCLCS.Linux.App;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class ToolboxView : UserControl
{
    public ToolboxView()
    {
        InitializeComponent();
        DataContext = new ToolboxViewModel();
    }

    private void Card_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string id }) return;
        // 跳转到对应工具页（Toolbox 主标签下的子项）
        if (this.FindAncestorOfType<MainWindow>() is { } win)
            win.NavigateTo(MainTabKind.Toolbox, id);
    }
}
