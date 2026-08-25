using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

/// <summary>版本设置对话框主体（作为 DialogOptions.Content 注入 ModalHost）。</summary>
public partial class VersionSettingsView : UserControl
{
    public VersionSettingsView()
    {
        InitializeComponent();
    }

    public VersionSettingsViewModel ViewModel => (VersionSettingsViewModel)DataContext!;

    private async void OnBrowseCustomDir(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is null) return;
        var start = string.IsNullOrWhiteSpace(ViewModel.CustomGameDir)
            ? await top.StorageProvider.TryGetFolderFromPathAsync(
                Path.Combine(Path.GetTempPath()))
            : await top.StorageProvider.TryGetFolderFromPathAsync(ViewModel.CustomGameDir);
        var folders = await top.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择实例游戏目录",
            SuggestedStartLocation = start
        });
        if (folders.Count > 0)
            ViewModel.CustomGameDir = folders[0].Path.LocalPath;
    }
}
