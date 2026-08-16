using System.Collections.ObjectModel;
using System.Windows.Input;
using MCLCS.Core.Download;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Profiles;
using MCLCS.Core.Utils;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 设置 → 下载（对齐 WPF 下载设置）：下载源偏好、最大并发下载数、服务器资源包缓存开关；
/// 三项均持久化到 LauncherProfile，并由运行时真正消费（下载源驱动 MirrorPolicy 重排、并发数传入 HttpDownloader）。
/// 另保留镜像端点清单展示与连通性测试。
/// </summary>
public class DownloadSettingsViewModel : ObservableObject
{
    private readonly string _profileRoot = GameConstants.DefaultGameRoot;
    private readonly LauncherProfile _profile;

    // ---- 下载源偏好 ----
    public ObservableCollection<DownloadSourcePreference> SourceOptions { get; } = new(
        Enum.GetValues<DownloadSourcePreference>());

    private DownloadSourcePreference _downloadSource = DownloadSourcePreference.MirrorFirst;
    public DownloadSourcePreference DownloadSource
    {
        get => _downloadSource;
        set => SetField(ref _downloadSource, value);
    }

    private int _maxConcurrent = 8;
    public int MaxConcurrentDownloads
    {
        get => _maxConcurrent;
        set => SetField(ref _maxConcurrent, value);
    }

    private bool _serverPackCacheEnabled = true;
    public bool ServerPackCacheEnabled
    {
        get => _serverPackCacheEnabled;
        set => SetField(ref _serverPackCacheEnabled, value);
    }

    // ---- 镜像端点清单 / 连通性测试 ----
    private ObservableCollection<string> _mirrorUrls = new();
    public ObservableCollection<string> MirrorUrls
    {
        get => _mirrorUrls;
        set => SetField(ref _mirrorUrls, value);
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

    public DownloadSettingsViewModel()
    {
        _profile = ProfileStore.Load(_profileRoot);
        _downloadSource = _profile.DownloadSource;
        _maxConcurrent = _profile.MaxConcurrentDownloads;
        _serverPackCacheEnabled = _profile.ServerPackCacheEnabled;
        Refresh();
    }

    public ICommand RefreshCommand => new RelayCommand(_ => Refresh());
    public ICommand TestCommand => new AsyncRelayCommand(_ => TestAsync());
    public ICommand SaveCommand => new RelayCommand(_ => Save());

    private void Save()
    {
        try
        {
            _profile.DownloadSource = DownloadSource;
            _profile.MaxConcurrentDownloads = Math.Clamp(MaxConcurrentDownloads, 1, 64);
            _profile.ServerPackCacheEnabled = ServerPackCacheEnabled;
            ProfileStore.Save(_profile);
            // 立即生效：下载源偏好驱动 MirrorPolicy 重排（最大并发在下次构造下载器时读取）
            MirrorPolicy.Preference = DownloadSource;
            Status = "下载设置已保存";
        }
        catch (Exception ex)
        {
            Status = $"保存失败：{ex.Message}";
        }
    }

    private void Refresh()
    {
        try
        {
            var urls = MirrorPolicy.VersionManifestUrls().ToList();
            MirrorUrls = new ObservableCollection<string>(urls);
            Status = $"当前 {urls.Count} 个版本清单镜像端点";
        }
        catch (Exception ex)
        {
            Status = $"读取镜像策略失败：{ex.Message}";
        }
    }

    private async Task TestAsync()
    {
        Busy = true;
        Status = "正在测试镜像连通性…";
        try
        {
            var urls = MirrorPolicy.VersionManifestUrls().ToList();
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            var results = new List<string>();
            foreach (var u in urls)
            {
                try
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    using var resp = await client.GetAsync(u, HttpCompletionOption.ResponseHeadersRead);
                    sw.Stop();
                    results.Add($"{resp.StatusCode}  {sw.ElapsedMilliseconds}ms  {u}");
                }
                catch (Exception ex)
                {
                    results.Add($"FAIL  {ex.GetType().Name}  {u}");
                }
            }
            Status = "连通性测试完成";
            MirrorUrls = new ObservableCollection<string>(results);
        }
        catch (Exception ex)
        {
            Status = $"测试失败：{ex.Message}";
        }
        finally
        {
            Busy = false;
        }
    }
}
