using System.Collections.ObjectModel;
using System.Windows.Input;
using MCLCS.Core.Download;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 设置 → 下载（对齐 WPF MirrorPolicy）：展示当前镜像策略下的下载端点清单，
/// 并测试镜像连通性（带回退）。
/// </summary>
public class DownloadSettingsViewModel : ObservableObject
{
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
        Refresh();
    }

    public ICommand RefreshCommand => new RelayCommand(_ => Refresh());
    public ICommand TestCommand => new AsyncRelayCommand(_ => TestAsync());

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
