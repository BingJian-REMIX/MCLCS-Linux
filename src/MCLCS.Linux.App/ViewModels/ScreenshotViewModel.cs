using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Toolbox;
using MCLCS.Core.Utils;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>截图展示模型（含勾选状态）。</summary>
public class ScreenshotVm
{
    public ScreenshotInfo Raw { get; init; } = new();
    public bool Selected { get; set; }
    public string Name => Raw.Name;
    public string FullPath => Raw.FullPath;
    public DateTime CreatedUtc => Raw.CreatedUtc;
    public string SizeText => Raw.SizeBytes switch
    {
        < 1024 => $"{Raw.SizeBytes} B",
        < 1024 * 1024 => $"{Raw.SizeBytes / 1024.0:F1} KB",
        _ => $"{Raw.SizeBytes / 1024.0 / 1024:F1} MB"
    };
}

/// <summary>
/// 工具箱 → 截图（对齐 WPF ScreenshotManager）：浏览、打包分享、批量删除游戏截图（screenshots/）。
/// </summary>
public class ScreenshotViewModel : ObservableObject
{
    private readonly string _gameRoot = GameConstants.DefaultGameRoot;

    private ObservableCollection<ScreenshotVm> _shots = new();
    public ObservableCollection<ScreenshotVm> Shots
    {
        get => _shots;
        set => SetField(ref _shots, value);
    }

    private string _status = LocaleManager.T("status.ready");
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand PackageCommand { get; }

    public ScreenshotViewModel()
    {
        RefreshCommand = new RelayCommand(_ => Refresh());
        DeleteCommand = new RelayCommand(_ => DeleteSelected());
        PackageCommand = new RelayCommand(_ => PackageSelected());
        Refresh();
    }

    public void Refresh()
    {
        Shots = new ObservableCollection<ScreenshotVm>(
            ScreenshotManager.ListScreenshots(_gameRoot).Select(s => new ScreenshotVm { Raw = s }));
        Status = Shots.Count > 0 ? $"共 {Shots.Count} 张截图" : "screenshots/ 下暂无截图";
    }

    public void DeleteSelected()
    {
        var paths = Shots.Where(s => s.Selected).Select(s => s.FullPath).ToList();
        if (paths.Count == 0) { Status = "请先勾选要删除的截图"; return; }
        var n = ScreenshotManager.DeleteScreenshots(paths);
        Status = $"已删除 {n} 张截图";
        Refresh();
    }

    public void PackageSelected()
    {
        var paths = Shots.Where(s => s.Selected).Select(s => s.FullPath).ToList();
        if (paths.Count == 0) { Status = "请先勾选要打包的截图"; return; }
        var dest = Path.Combine(Path.GetTempPath(), $"mclcs_screenshots_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
        try
        {
            var outPath = ScreenshotManager.Package(paths, dest);
            Status = $"已打包 {paths.Count} 张截图到：{outPath}";
        }
        catch (Exception ex)
        {
            Status = $"打包失败：{ex.Message}";
        }
    }
}
