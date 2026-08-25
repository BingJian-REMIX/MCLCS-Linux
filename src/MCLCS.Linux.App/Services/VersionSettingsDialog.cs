using MCLCS.Linux.App.ViewModels;
using MCLCS.Linux.App.Views.Pages;

namespace MCLCS.Linux.App;

/// <summary>打开「版本设置」对话框（主页与版本列表页共用）。</summary>
public static class VersionSettingsDialog
{
    /// <summary>
    /// 打开版本设置对话框。点击「保存」时回写 versions/&lt;id&gt;/profile.json；
    /// <paramref name="onVersionsChanged"/> 在加载器安装创建新实例时触发，供调用方刷新版本列表。
    /// </summary>
    public static async Task OpenAsync(string gameRoot, string id, string type, Action? onVersionsChanged = null)
    {
        var vm = new VersionSettingsViewModel(id, type, gameRoot);
        if (onVersionsChanged is not null) vm.VersionsChanged += onVersionsChanged;
        var view = new VersionSettingsView { DataContext = vm };
        var result = await DialogService.Instance.ShowAsync(new DialogOptions
        {
            Title = $"版本设置 · {id}",
            Content = view,
            Buttons = new[]
            {
                new DialogButton("保存", "save", DialogButtonKind.Primary, isDefault: true),
                new DialogButton("取消", "cancel", DialogButtonKind.Ghost, isCancel: true)
            },
            Width = 660
        });
        if (Equals(result, "save"))
            vm.Save();
    }
}
