using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using MCLCS.Core.Download;
using MCLCS.Core.Installers;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Utils;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 下载 → 原版安装 视图模型（对齐 WPF InstallViewModel）：
/// 选择安装类型（Vanilla / Fabric / Forge / Quilt / NeoForge）与版本号，调用 Core 对应 Installer 安装，
/// 安装日志实时回显到界面（Core IDownloader + ILogger）。
/// </summary>
public class InstallViewModel : ObservableObject
{
    private readonly string _gameRoot = GameConstants.DefaultGameRoot;

    public ObservableCollection<string> InstallTypes { get; } =
        new() { "Vanilla", "Fabric", "Forge", "Quilt", "NeoForge" };

    private string _selectedType = "Vanilla";
    public string SelectedType
    {
        get => _selectedType;
        set => SetField(ref _selectedType, value);
    }

    private string _versionId = "";
    public string VersionId
    {
        get => _versionId;
        set => SetField(ref _versionId, value);
    }

    private string _log = "";
    public string Log
    {
        get => _log;
        set => SetField(ref _log, value);
    }

    private double _progress;
    public double Progress
    {
        get => _progress;
        set => SetField(ref _progress, value);
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetField(ref _isBusy, value)) OnPropertyChanged(nameof(CanInstall));
        }
    }

    public bool CanInstall => !IsBusy && !string.IsNullOrWhiteSpace(VersionId);

    public ICommand InstallCommand { get; }

    public InstallViewModel()
    {
        InstallCommand = new AsyncRelayCommand(_ => InstallAsync());
    }

    private async Task InstallAsync()
    {
        if (string.IsNullOrWhiteSpace(VersionId))
        {
            Log = "请输入要安装的版本号（例如 1.20.1；加载器可填对应 MC 版本，如 Forge 填 1.20.1）";
            return;
        }

        IsBusy = true;
        Progress = 0;
        Log = $"开始安装 {SelectedType} {VersionId} …\n";
        try
        {
            var logger = new LogAdapter(msg => Log += msg + "\n");
            using var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
            var downloader = new HttpDownloader(client, maxConcurrency: 8, logger);
            var progress = new Progress<(int Done, int Total)>(p =>
            {
                Progress = p.Total > 0 ? p.Done * 100.0 / p.Total : 0;
                Log += $"[{p.Done}/{p.Total}] {SelectedType} {VersionId}\n";
            });

            switch (SelectedType)
            {
                case "Vanilla":
                    await new VanillaInstaller(_gameRoot, client, downloader, logger)
                        .InstallAsync(VersionId, progress, default);
                    break;
                case "Fabric":
                    await new FabricInstaller(_gameRoot, client, downloader, logger)
                        .InstallAsync(VersionId, progress, default);
                    break;
                case "Forge":
                    await new ForgeInstaller(_gameRoot, client, downloader, logger)
                        .InstallAsync(VersionId, progress, default);
                    break;
                case "Quilt":
                    await new QuiltInstaller(_gameRoot, client, downloader, logger)
                        .InstallAsync(VersionId, progress, default);
                    break;
                case "NeoForge":
                    await new NeoForgeInstaller(_gameRoot, client, downloader, logger)
                        .InstallAsync(VersionId, progress, default);
                    break;
            }

            Log += "安装完成。\n";
        }
        catch (System.Exception ex)
        {
            Log += $"安装出错：{ex.Message}\n";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>把 Core ILogger 输出接到 VM 日志文本（追加）。</summary>
    private sealed class LogAdapter : ILogger
    {
        private readonly System.Action<string> _sink;
        public LogAdapter(System.Action<string> sink) => _sink = sink;
        public void Log(string message) => _sink(message);
    }
}
