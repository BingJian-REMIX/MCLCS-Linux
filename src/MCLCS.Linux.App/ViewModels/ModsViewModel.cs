using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows.Input;
using MCLCS.Core.Download;
using MCLCS.Core.Localization;
using MCLCS.Core.Models;
using MCLCS.Core.Mods;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Utils;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 下载 → Mod（对齐 WPF ModrinthClient/ModManager）：搜索 Modrinth、浏览结果、
/// 查看已安装 Mod、卸载。
/// </summary>
public class ModsViewModel : ObservableObject
{
    private readonly ModrinthClient _client = new(new HttpClient());
    private readonly HttpClient _dl = new();
    private readonly ModManager _manager;
    private readonly string _gameRoot = GameConstants.DefaultGameRoot;

    private string _query = "";
    public string Query
    {
        get => _query;
        set => SetField(ref _query, value);
    }

    private string _gameVersion = "";
    public string GameVersion
    {
        get => _gameVersion;
        set => SetField(ref _gameVersion, value);
    }

    private ObservableCollection<ModrinthHit> _hits = new();
    public ObservableCollection<ModrinthHit> Hits
    {
        get => _hits;
        set => SetField(ref _hits, value);
    }

    private ObservableCollection<ModEntry> _installed = new();
    public ObservableCollection<ModEntry> Installed
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

    private bool _busy;
    public bool Busy
    {
        get => _busy;
        set => SetField(ref _busy, value);
    }

    public ModsViewModel()
    {
        _manager = new ModManager(_gameRoot, new HttpClient(), new HttpDownloader(new HttpClient()));
        RefreshInstalled();
    }

    public ICommand SearchCommand => new AsyncRelayCommand(_ => SearchAsync());
    public ICommand RefreshCommand => new RelayCommand(_ => RefreshInstalled());
    public ICommand UninstallCommand => new RelayCommand(o => Uninstall(o as ModEntry));
    public ICommand InstallHitCommand => new AsyncRelayCommand(o => InstallHitAsync(o as ModrinthHit));
    public ICommand OpenHitCommand => new RelayCommand(o => OpenHit(o as ModrinthHit));

    private async Task SearchAsync()
    {
        Busy = true;
        Status = "正在搜索 Modrinth…";
        try
        {
            var r = await _client.SearchAsync(
                Query,
                string.IsNullOrWhiteSpace(GameVersion) ? null : GameVersion,
                LoaderType.Any,
                ModrinthProjectType.Mod);
            Hits = new ObservableCollection<ModrinthHit>(r.Hits);
            Status = $"找到 {r.TotalHits} 个结果（显示 {Hits.Count} 个）";
        }
        catch (Exception ex)
        {
            Status = $"搜索失败：{ex.Message}";
        }
        finally
        {
            Busy = false;
        }
    }

    private void RefreshInstalled()
    {
        try
        {
            Installed = new ObservableCollection<ModEntry>(_manager.ListInstalledMods());
            Status = $"已安装 {Installed.Count} 个 Mod";
        }
        catch (Exception ex)
        {
            Status = $"读取已安装列表失败：{ex.Message}";
        }
    }

    private void Uninstall(ModEntry? entry)
    {
        if (entry is null) return;
        try
        {
            if (_manager.UninstallMod(entry.FileName))
            {
                Status = $"已卸载：{entry.Name}";
                RefreshInstalled();
            }
            else
            {
                Status = $"卸载失败：{entry.Name}";
            }
        }
        catch (Exception ex)
        {
            Status = $"卸载异常：{ex.Message}";
        }
    }

    /// <summary>下载搜索结果中的某个 Mod 的最新版本到 mods 目录（对齐 WPF 卡片“加入队列/下载”）。</summary>
    private async Task InstallHitAsync(ModrinthHit? hit)
    {
        if (hit is null) return;
        Busy = true;
        Status = $"正在下载 {hit.Title}…";
        try
        {
            var versions = await _client.GetVersionsAsync(hit.ProjectId);
            var ver = versions.FirstOrDefault();
            if (ver?.Files == null || ver.Files.Count == 0)
            {
                Status = "该 Mod 无可用文件";
                return;
            }
            var file = ver.Files.FirstOrDefault(f => f.Primary) ?? ver.Files[0];
            var modsDir = Path.Combine(_gameRoot, "mods");
            Directory.CreateDirectory(modsDir);
            var dest = Path.Combine(modsDir, file.FileName);
            var bytes = await _dl.GetByteArrayAsync(file.Url);
            await File.WriteAllBytesAsync(dest, bytes);
            Status = $"已下载：{file.FileName}";
            RefreshInstalled();
        }
        catch (Exception ex)
        {
            Status = $"下载失败：{ex.Message}";
        }
        finally
        {
            Busy = false;
        }
    }

    /// <summary>在浏览器打开 Modrinth 项目页（对齐 WPF 卡片“详情”）。</summary>
    private void OpenHit(ModrinthHit? hit)
    {
        if (hit is null || string.IsNullOrWhiteSpace(hit.Slug)) return;
        try
        {
            var url = $"https://modrinth.com/mod/{hit.Slug}";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            /* 无可用浏览器时静默忽略 */
        }
    }
}
