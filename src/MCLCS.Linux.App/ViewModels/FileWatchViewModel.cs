using System.Collections.ObjectModel;
using System.Windows.Input;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Toolbox;
using MCLCS.Core.Utils;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 工具箱 → 文件变更监控（对齐 WPF FileChangeDetector）：对比游戏目录中
/// mods / resourcepacks / shaderpacks 的快照，列出新增 / 删除 / 修改。
/// </summary>
public class FileWatchViewModel : ObservableObject
{
    private readonly string _gameRoot = GameConstants.DefaultGameRoot;

    private ObservableCollection<FileChange> _changes = new();
    public ObservableCollection<FileChange> Changes
    {
        get => _changes;
        set => SetField(ref _changes, value);
    }

    private string _status = LocaleManager.T("status.ready");
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public ICommand ScanCommand => new RelayCommand(_ => Scan());

    private void Scan()
    {
        try
        {
            var diff = FileChangeDetector.DetectAndUpdate(_gameRoot);
            Changes = new ObservableCollection<FileChange>(diff.Changes);
            Status = Changes.Count == 0
                ? "未检测到变更（已更新基准快照）"
                : $"检测到 {Changes.Count} 处变更";
        }
        catch (Exception ex)
        {
            Status = $"扫描失败：{ex.Message}";
        }
    }
}
