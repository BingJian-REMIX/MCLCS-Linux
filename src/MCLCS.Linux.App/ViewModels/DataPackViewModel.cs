using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Toolbox;
using MCLCS.Core.Utils;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 工具箱 → 数据包冲突检测（对齐 WPF DataPackConflictDetector）：
/// 选择存档后扫描其 datapacks，列出数据包、冲突（含建议）与格式告警。
/// </summary>
public class DataPackViewModel : ObservableObject
{
    private readonly string _gameRoot = GameConstants.DefaultGameRoot;

    private ObservableCollection<string> _saves = new();
    public ObservableCollection<string> Saves
    {
        get => _saves;
        set => SetField(ref _saves, value);
    }

    private string? _selectedSave;
    public string? SelectedSave
    {
        get => _selectedSave;
        set => SetField(ref _selectedSave, value);
    }

    private ObservableCollection<DataPackInfo> _packs = new();
    public ObservableCollection<DataPackInfo> Packs
    {
        get => _packs;
        set => SetField(ref _packs, value);
    }

    private ObservableCollection<DataPackConflict> _conflicts = new();
    public ObservableCollection<DataPackConflict> Conflicts
    {
        get => _conflicts;
        set => SetField(ref _conflicts, value);
    }

    private ObservableCollection<string> _formatWarnings = new();
    public ObservableCollection<string> FormatWarnings
    {
        get => _formatWarnings;
        set => SetField(ref _formatWarnings, value);
    }

    private string _status = LocaleManager.T("status.ready");
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public ICommand RefreshSavesCommand => new RelayCommand(_ => RefreshSaves());
    public ICommand ScanCommand => new RelayCommand(_ => Scan());

    public void RefreshSaves()
    {
        var savesDir = Path.Combine(_gameRoot, "saves");
        Saves = Directory.Exists(savesDir)
            ? new ObservableCollection<string>(
                Directory.GetDirectories(savesDir).Select(Path.GetFileName).OfType<string>().OrderBy(x => x))
            : new ObservableCollection<string>();
        Status = Saves.Count == 0 ? "未找到任何存档" : $"共 {Saves.Count} 个存档";
    }

    public void Scan()
    {
        if (SelectedSave is null)
        {
            Status = "请先在上方选择存档";
            return;
        }
        var dpDir = Path.Combine(_gameRoot, "saves", SelectedSave, "datapacks");
        if (!Directory.Exists(dpDir))
        {
            Status = $"存档「{SelectedSave}」无 datapacks 目录";
            Packs = new(); Conflicts = new(); FormatWarnings = new();
            return;
        }
        var report = DataPackConflictDetector.Scan(dpDir, null, _gameRoot);
        Packs = new ObservableCollection<DataPackInfo>(report.Packs);
        Conflicts = new ObservableCollection<DataPackConflict>(report.Conflicts);
        FormatWarnings = new ObservableCollection<string>(report.FormatWarnings);
        Status = $"扫描完成：{Packs.Count} 个数据包，{Conflicts.Count} 处冲突，{FormatWarnings.Count} 条格式告警";
    }
}
