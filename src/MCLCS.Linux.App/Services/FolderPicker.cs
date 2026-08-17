using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace MCLCS.Linux.App.Services;

/// <summary>文件夹选择助手：复用主窗口的 StorageProvider（对齐 WPF 的 UIService.PickFolder）。</summary>
public static class FolderPicker
{
    /// <summary>打开文件夹选择对话框，返回所选路径；取消时返回 null。</summary>
    public static async Task<string?> PickAsync(string title)
    {
        if (Application.Current?.ApplicationLifetime is
            IClassicDesktopStyleApplicationLifetime { MainWindow: { } win })
        {
            var folders = await win.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions { Title = title, AllowMultiple = false });
            return folders.Count > 0 ? folders[0].Path.LocalPath : null;
        }
        return null;
    }
}
