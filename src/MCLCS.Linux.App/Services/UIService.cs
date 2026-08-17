using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using MCLCS.Linux.App.Controls;

namespace MCLCS.Linux.App.Services;

/// <summary>
/// Linux 版 UI 服务（对齐 WPF UIService 职责）：
/// 模态确认（自建 ConfirmDialog）、目录 / 文件选择（Avalonia StorageProvider，走系统 XDG portal）。
/// headless / 无主窗口环境：Confirm 返回 true（放行），选择器返回 null（调用方保留手动输入降级）。
/// </summary>
public static class UIService
{
    /// <summary>当前主窗口（无窗口生命周期时返回 null）。</summary>
    public static Window? MainWindow =>
        Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime { MainWindow: { } w }
            ? w
            : null;

    /// <summary>模态确认；无主窗口时默认放行（不阻塞自动化/后台流程）。</summary>
    public static async Task<bool> ConfirmAsync(string message, string title = "确认",
        string okText = "确定", bool danger = false)
    {
        var owner = MainWindow;
        if (owner is null) return true;
        return await ConfirmDialog.ShowAsync(owner, title, message, okText, danger) == true;
    }

    /// <summary>选择目录；无主窗口或取消时返回 null。</summary>
    public static async Task<string?> PickFolderAsync(string title = "选择目录")
    {
        var owner = MainWindow;
        if (owner is null) return null;
        var folders = await owner.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = title, AllowMultiple = false });
        return folders.FirstOrDefault()?.TryGetLocalPath();
    }

    /// <summary>选择文件；filterPattern 为通配符如 "*.dat;*.nbt"。无主窗口或取消时返回 null。</summary>
    public static async Task<string?> PickFileAsync(string title = "选择文件", string? filterPattern = null)
    {
        var owner = MainWindow;
        if (owner is null) return null;
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = filterPattern is null
                ? null
                : new[] { new FilePickerFileType(title) { Patterns = new[] { filterPattern } } }
        });
        return files.FirstOrDefault()?.TryGetLocalPath();
    }
}

/// <summary>Linux 版 Toast（对齐 WPF ToastService）：右下角 2.5s 提示。无主窗口时静默跳过。</summary>
public enum ToastKind { Info, Success, Error }

public static class ToastService
{
    public static void Show(string title, string message, ToastKind kind = ToastKind.Info)
    {
        var owner = UIService.MainWindow;
        if (owner is null) return; // headless / 无主窗口：静默跳过
        var toast = new ToastWindow(title, message, kind);
        toast.ShowAt(owner);
    }
}
