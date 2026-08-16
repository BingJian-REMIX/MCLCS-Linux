using System.Collections.Generic;
using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Input;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Toolbox;
using MCLCS.Core.Utils;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 工具箱 → 备份（对齐 WPF BackupManager）：列出 / 删除 / 清理过期 / 触发计划备份。
/// 备份记录持久化于 gameRoot/backups/index.json。
/// </summary>
public class BackupViewModel : ObservableObject
{
    private readonly string _gameRoot = GameConstants.DefaultGameRoot;

    private ObservableCollection<BackupRecord> _records = new();
    public ObservableCollection<BackupRecord> Records
    {
        get => _records;
        set => SetField(ref _records, value);
    }

    private string _status = LocaleManager.T("status.ready");
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand PruneCommand { get; }
    public ICommand RunScheduledCommand { get; }

    public BackupViewModel()
    {
        RefreshCommand = new RelayCommand(_ => Refresh());
        DeleteCommand = new RelayCommand(p => Delete(p as BackupRecord));
        PruneCommand = new RelayCommand(_ => Prune());
        RunScheduledCommand = new RelayCommand(_ => RunScheduled());
        Refresh();
    }

    public void Refresh()
    {
        Records = new ObservableCollection<BackupRecord>(BackupManager.List(_gameRoot));
        Status = $"共 {Records.Count} 份备份";
    }

    public void Delete(BackupRecord? rec)
    {
        if (rec is null) return;
        if (BackupManager.Delete(_gameRoot, rec.Id))
        {
            Status = $"已删除备份：{rec.SourceName}";
            Refresh();
        }
        else
            Status = "删除失败";
    }

    public void Prune()
    {
        var n = BackupManager.Prune(_gameRoot, new BackupPolicy());
        Status = n > 0 ? $"已清理 {n} 份过期备份" : "没有需要清理的过期备份";
        Refresh();
    }

    public void RunScheduled()
    {
        var savesDir = Path.Combine(_gameRoot, "saves");
        var sources = Directory.Exists(savesDir)
            ? Directory.GetDirectories(savesDir).Select(d => Path.GetFileName(d)!).ToList()
            : new List<string>();
        if (sources.Count == 0) { Status = "没有可备份的存档（saves/ 为空）"; return; }
        var policy = new BackupPolicy { Schedule = BackupSchedule.Daily };
        var n = BackupManager.RunScheduledIfDue(_gameRoot, sources, policy, BackupKind.Save);
        Status = n > 0 ? $"已按计划创建 {n} 份存档备份" : "当前未到计划备份时间（或策略关闭）";
        Refresh();
    }
}
