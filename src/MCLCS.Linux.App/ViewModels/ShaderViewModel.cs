using System.Collections.ObjectModel;
using System.Windows.Input;
using MCLCS.Core.Download;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Utils;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 下载 → 光影（对齐 WPF ExtraResourceInstaller）：安装本地光影包 zip 到 shaderpacks/，
/// 并列出已安装光影。
/// </summary>
public class ShaderViewModel : ObservableObject
{
    private readonly string _gameRoot = GameConstants.DefaultGameRoot;

    private string _zipPath = "";
    public string ZipPath
    {
        get => _zipPath;
        set => SetField(ref _zipPath, value);
    }

    private ObservableCollection<string> _installed = new();
    public ObservableCollection<string> Installed
    {
        get => _installed;
        set => SetField(ref _installed, value);
    }

    private string _status = LocaleManager.T("status.ready");
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public ShaderViewModel()
    {
        RefreshInstalled();
    }

    public ICommand InstallCommand => new RelayCommand(_ => Install());
    public ICommand RefreshCommand => new RelayCommand(_ => RefreshInstalled());

    private void Install()
    {
        if (string.IsNullOrWhiteSpace(ZipPath) || !File.Exists(ZipPath))
        {
            Status = "请填写有效的光影包 zip 路径";
            return;
        }
        try
        {
            var r = ExtraResourceInstaller.Install(ZipPath, _gameRoot);
            Status = r.Ok ? $"安装成功：{r.Summary}" : $"安装失败：{r.Error}";
            if (r.Ok) RefreshInstalled();
        }
        catch (Exception ex)
        {
            Status = $"安装异常：{ex.Message}";
        }
    }

    private void RefreshInstalled()
    {
        var dir = Path.Combine(_gameRoot, "shaderpacks");
        Installed = Directory.Exists(dir)
            ? new ObservableCollection<string>(Directory.GetFileSystemEntries(dir).Select(Path.GetFileName).Where(n => n is not null).Select(n => n!))
            : new ObservableCollection<string>();
    }
}
