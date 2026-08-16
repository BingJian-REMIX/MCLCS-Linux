using System;
using System.IO;
using System.Windows.Input;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Toolbox;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 工具箱 → 快捷方式生成（对齐 WPF ShortcutGenerator）：为指定版本在桌面生成启动快捷方式。
/// </summary>
public class ShortcutViewModel : ObservableObject
{
    private string _versionId = "";
    public string VersionId
    {
        get => _versionId;
        set => SetField(ref _versionId, value);
    }

    private string _desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop");
    public string DesktopDir
    {
        get => _desktopDir;
        set => SetField(ref _desktopDir, value);
    }

    private string _result = "";
    public string Result
    {
        get => _result;
        set => SetField(ref _result, value);
    }

    private string _status = LocaleManager.T("status.ready");
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public ICommand CreateCommand => new RelayCommand(_ => Create());

    private void Create()
    {
        if (string.IsNullOrWhiteSpace(VersionId))
        {
            Status = "请填写版本号";
            return;
        }
        try
        {
            var r = ShortcutGenerator.CreateShortcut(DesktopDir, VersionId);
            Result = r.Success
                ? $"已创建：{r.FilePath}（方式：{r.Method}）"
                : $"失败：{r.Error}";
            Status = r.Success ? "创建成功" : "创建失败";
        }
        catch (Exception ex)
        {
            Result = $"异常：{ex.Message}";
            Status = "创建异常";
        }
    }
}
