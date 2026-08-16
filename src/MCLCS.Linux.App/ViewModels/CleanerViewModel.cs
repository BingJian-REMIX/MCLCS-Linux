using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Toolbox;
using MCLCS.Core.Utils;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>冗余文件展示模型（含勾选状态）。</summary>
public class RedundantFileVm
{
    public RedundantFile Raw { get; init; } = new();
    public bool Selected { get; set; } = true;
    public string RelativePath => Raw.RelativePath;
    public long SizeBytes => Raw.SizeBytes;
    public string SizeText => SizeBytes switch
    {
        < 1024 => $"{SizeBytes} B",
        < 1024 * 1024 => $"{SizeBytes / 1024.0:F1} KB",
        _ => $"{SizeBytes / 1024.0 / 1024:F1} MB"
    };
}

/// <summary>
/// 工具箱 → 清理冗余（对齐 WPF RedundantFileCleaner）：扫描未被任何已安装版本引用的
/// libraries/ 与 assets/ 文件，列出并可清理（默认移入回收目录，不直接删除）。
/// </summary>
public class CleanerViewModel : ObservableObject
{
    private readonly string _gameRoot = GameConstants.DefaultGameRoot;

    private ObservableCollection<RedundantFileVm> _files = new();
    public ObservableCollection<RedundantFileVm> Files
    {
        get => _files;
        set => SetField(ref _files, value);
    }

    private bool _deleteDirectly;
    public bool DeleteDirectly
    {
        get => _deleteDirectly;
        set => SetField(ref _deleteDirectly, value);
    }

    private string _status = LocaleManager.T("status.ready");
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public int TotalCount => Files.Count;
    public long TotalBytes => Files.Sum(f => f.SizeBytes);
    public string TotalSizeText => TotalBytes switch
    {
        < 1024 => $"{TotalBytes} B",
        < 1024 * 1024 => $"{TotalBytes / 1024.0:F1} KB",
        _ => $"{TotalBytes / 1024.0 / 1024:F1} MB"
    };

    public ICommand ScanCommand { get; }
    public ICommand CleanCommand { get; }

    public CleanerViewModel()
    {
        ScanCommand = new RelayCommand(_ => Scan());
        CleanCommand = new RelayCommand(_ => Clean(), _ => Files.Count > 0);
        Scan();
    }

    public void Scan()
    {
        var list = RedundantFileCleaner.Scan(_gameRoot)
            .Select(r => new RedundantFileVm { Raw = r })
            .ToList();
        Files = new ObservableCollection<RedundantFileVm>(list);
        Status = list.Count > 0
            ? $"发现 {list.Count} 个冗余文件，共 {TotalSizeText}"
            : "未扫描到冗余文件（库与资源均被已安装版本引用）";
    }

    public void Clean()
    {
        var chosen = Files.Where(f => f.Selected).Select(f => f.Raw).ToList();
        if (chosen.Count == 0) { Status = "请先勾选要清理的文件"; return; }
        try
        {
            var n = RedundantFileCleaner.Clean(chosen, _gameRoot, DeleteDirectly);
            Status = DeleteDirectly
                ? $"已直接删除 {n} 个文件"
                : $"已移入回收目录 {n} 个文件（gameRoot/mclcs_redundant_trash，可还原）";
            Scan();
        }
        catch (Exception ex)
        {
            Status = $"清理失败：{ex.Message}";
        }
    }
}
