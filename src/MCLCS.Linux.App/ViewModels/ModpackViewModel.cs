using System.Windows.Input;
using MCLCS.Core.Installers;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Toolbox;
using MCLCS.Core.Utils;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 下载 → 整合包（对齐 WPF ModpackInstaller/ModpackExporter）：
/// 安装本地 .mrpack 整合包，导出当前实例为整合包。
/// </summary>
public class ModpackViewModel : ObservableObject
{
    private readonly string _gameRoot = GameConstants.DefaultGameRoot;

    private string _mrpackPath = "";
    public string MrpackPath
    {
        get => _mrpackPath;
        set => SetField(ref _mrpackPath, value);
    }

    private bool _isolated = true;
    public bool Isolated
    {
        get => _isolated;
        set => SetField(ref _isolated, value);
    }

    private string _versionId = "";
    public string VersionId
    {
        get => _versionId;
        set => SetField(ref _versionId, value);
    }

    private string _destZip = "";
    public string DestZip
    {
        get => _destZip;
        set => SetField(ref _destZip, value);
    }

    private string _status = LocaleManager.T("status.ready");
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    private bool _busy;
    public bool Busy
    {
        get => _busy;
        set => SetField(ref _busy, value);
    }

    private ModpackInstaller CreateInstaller()
        => new(_gameRoot, new HttpClient(), new MCLCS.Core.Download.HttpDownloader(new HttpClient()));

    public ICommand InstallCommand => new AsyncRelayCommand(_ => InstallAsync());
    public ICommand ExportCommand => new RelayCommand(_ => Export());

    private async Task InstallAsync()
    {
        if (string.IsNullOrWhiteSpace(MrpackPath) || !File.Exists(MrpackPath))
        {
            Status = "请填写有效的 .mrpack 整合包路径";
            return;
        }
        Busy = true;
        Status = "正在安装整合包…";
        try
        {
            var installer = CreateInstaller();
            var r = await installer.InstallAsync(MrpackPath, Isolated);
            Status = $"安装完成：{r.Name}（MC {r.VersionId}，{r.ModCount} 个 Mod）→ {r.GameDir}";
        }
        catch (Exception ex)
        {
            Status = $"安装失败：{ex.Message}";
        }
        finally
        {
            Busy = false;
        }
    }

    private void Export()
    {
        if (string.IsNullOrWhiteSpace(VersionId))
        {
            Status = "请填写要导出的版本号";
            return;
        }
        if (string.IsNullOrWhiteSpace(DestZip))
        {
            Status = "请填写导出 zip 路径";
            return;
        }
        try
        {
            var path = ModpackExporter.Export(_gameRoot, VersionId, DestZip);
            Status = $"导出成功：{path}";
        }
        catch (Exception ex)
        {
            Status = $"导出失败：{ex.Message}";
        }
    }
}
