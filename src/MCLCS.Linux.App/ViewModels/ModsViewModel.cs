using System.Collections.ObjectModel;
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
}
