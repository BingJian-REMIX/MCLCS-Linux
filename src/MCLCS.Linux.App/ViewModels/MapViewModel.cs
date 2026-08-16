using System.Windows.Input;
using MCLCS.Core.Download;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Utils;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 下载 → 地图（对齐 WPF MapInstaller）：安装本地地图 zip 到 saves/，自动检测根目录前缀。
/// </summary>
public class MapViewModel : ObservableObject
{
    private readonly string _gameRoot = GameConstants.DefaultGameRoot;

    private string _zipPath = "";
    public string ZipPath
    {
        get => _zipPath;
        set => SetField(ref _zipPath, value);
    }

    private string _status = LocaleManager.T("status.ready");
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public ICommand InstallCommand => new RelayCommand(_ => Install());

    private void Install()
    {
        if (string.IsNullOrWhiteSpace(ZipPath) || !File.Exists(ZipPath))
        {
            Status = "请填写有效的地图 zip 路径";
            return;
        }
        try
        {
            var r = MapInstaller.Install(ZipPath, _gameRoot);
            Status = r.Ok
                ? $"安装成功：{r.SaveName} → {r.SaveDir}"
                : $"安装失败：{r.Error}";
        }
        catch (Exception ex)
        {
            Status = $"安装异常：{ex.Message}";
        }
    }
}
