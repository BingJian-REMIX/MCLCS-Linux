using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class SavesView : UserControl
{
    public SavesView()
    {
        InitializeComponent();
        DataContext = new SavesViewModel();
        if (DataContext is SavesViewModel vm)
        {
            vm.Refresh();
            // 提取种子后自动复制到系统剪贴板（对齐 WPF 行为；headless 下静默跳过）
            vm.PropertyChanged += OnSavesPropertyChanged;
        }
    }

    private void OnSavesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is SavesViewModel vm
            && e.PropertyName == nameof(SavesViewModel.SeedText)
            && !string.IsNullOrEmpty(vm.SeedText))
        {
            _ = CopySeedToClipboardAsync(vm.SeedText);
        }
    }

    private async Task CopySeedToClipboardAsync(string seed)
    {
        try
        {
            var top = TopLevel.GetTopLevel(this);
            if (top?.Clipboard is not null)
                await top.Clipboard.SetTextAsync(seed);
        }
        catch
        {
            // 剪贴板不可用时静默（种子已显示在状态栏）
        }
    }
}
