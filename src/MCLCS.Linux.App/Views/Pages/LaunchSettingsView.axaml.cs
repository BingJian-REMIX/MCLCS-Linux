using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MCLCS.Core.Utils;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class LaunchSettingsView : UserControl
{
    public LaunchSettingsView()
    {
        InitializeComponent();
        DataContext = new LaunchSettingsViewModel();
    }

    private LaunchSettingsViewModel Vm => (LaunchSettingsViewModel)DataContext!;

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is null) return;
        var folders = await top.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择游戏目录",
            SuggestedStartLocation = await top.StorageProvider.TryGetFolderFromPathAsync(Vm.GameRoot)
        });
        if (folders.Count > 0)
            Vm.GameRoot = folders[0].Path.LocalPath;
    }

    private void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Vm.GameRoot)) return;
        try
        {
            Process.Start(new ProcessStartInfo { FileName = Vm.GameRoot, UseShellExecute = true });
        }
        catch
        {
            // 文件夹打开失败（如沙箱/无桌面环境）时静默忽略
        }
    }

    private void OnResetClick(object? sender, RoutedEventArgs e)
        => Vm.GameRoot = GameConstants.SystemGameRoot;
}
