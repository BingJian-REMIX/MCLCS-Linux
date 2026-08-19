using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Save;
using MCLCS.Core.Toolbox;
using MCLCS.Core.Utils;
using MCLCS.Linux.App.Services;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 工具箱 → 存档管理器（对齐 WPF Save* 引擎）：
/// 列出存档，做兼容性与损坏检测，并支持快速降级（修改 DataVersion）。
/// </summary>
public class SavesViewModel : ObservableObject
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

    private string _targetVersion = "1.21.4";
    public string TargetVersion
    {
        get => _targetVersion;
        set => SetField(ref _targetVersion, value);
    }

    private ObservableCollection<SaveCompatibilityReport> _compat = new();
    public ObservableCollection<SaveCompatibilityReport> CompatReports
    {
        get => _compat;
        set => SetField(ref _compat, value);
    }

    private ObservableCollection<SaveCorruptionReport> _corruption = new();
    public ObservableCollection<SaveCorruptionReport> CorruptionReports
    {
        get => _corruption;
        set => SetField(ref _corruption, value);
    }

    private string _downgradeResult = "";
    public string DowngradeResult
    {
        get => _downgradeResult;
        set => SetField(ref _downgradeResult, value);
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

    public ICommand RefreshCommand => new RelayCommand(_ => Refresh());
    public ICommand AnalyzeCommand => new RelayCommand(_ => Analyze());
    public ICommand DowngradeCommand => new AsyncRelayCommand(_ => DowngradeAsync());

    // ── 对齐 WPF SavesView 真缺的 4 个命令：备份 / 删除 / 恢复（回滚）/ 提取种子 ──
    public ICommand BackupCommand => new AsyncRelayCommand(_ => BackupAsync());
    public ICommand DeleteCommand => new AsyncRelayCommand(_ => DeleteAsync());
    public ICommand RestoreCommand => new AsyncRelayCommand(_ => RestoreAsync());
    public ICommand ExtractSeedCommand => new RelayCommand(_ => ExtractSeed());

    public void Refresh()
    {
        var savesDir = Path.Combine(_gameRoot, "saves");
        Saves = Directory.Exists(savesDir)
            ? new ObservableCollection<string>(
                Directory.GetDirectories(savesDir).Select(Path.GetFileName).OfType<string>().OrderBy(x => x))
            : new ObservableCollection<string>();
        Status = Saves.Count == 0 ? "未找到任何存档" : $"共 {Saves.Count} 个存档";
    }

    public void Analyze()
    {
        var target = string.IsNullOrWhiteSpace(TargetVersion) ? "" : TargetVersion.Trim();
        CompatReports = new ObservableCollection<SaveCompatibilityReport>(
            SaveCompatibilityDetector.Scan(_gameRoot, target));
        CorruptionReports = new ObservableCollection<SaveCorruptionReport>(
            SaveCorruptionDetector.Scan(_gameRoot));
        Status = $"兼容性：{CompatReports.Count} 个存档；损坏检测：{CorruptionReports.Count} 份报告";
    }

    private async Task DowngradeAsync()
    {
        if (SelectedSave is null) { Status = "请先选择存档"; return; }
        var savePath = Path.Combine(_gameRoot, "saves", SelectedSave);
        if (!Directory.Exists(savePath)) { Status = "存档目录不存在"; return; }

        Busy = true;
        Status = $"正在降级「{SelectedSave}」到 {TargetVersion}…";
        try
        {
            var plan = await SaveDowngrader.DowngradeAsync(
                savePath, TargetVersion.Trim(), DowngradeMethod.QuickModifyDataVersion);
            DowngradeResult = plan.Success
                ? $"降级成功：{SelectedSave}（dv {plan.FromDataVersion} → {plan.ToDataVersion}）\n" +
                  string.Join("\n", plan.Summary)
                : $"降级失败：{plan.ErrorMessage}";
            Status = plan.Success ? "降级完成" : "降级失败";
        }
        catch (Exception ex)
        {
            DowngradeResult = $"异常：{ex.Message}";
            Status = "降级异常";
        }
        finally
        {
            Busy = false;
        }
    }

    private string _actionResult = "";
    /// <summary>备份 / 删除 / 恢复等操作的回显结果。</summary>
    public string ActionResult
    {
        get => _actionResult;
        set => SetField(ref _actionResult, value);
    }

    private string _seedText = "";
    /// <summary>最近一次提取到的种子；变更时由 View 复制到系统剪贴板。</summary>
    public string SeedText
    {
        get => _seedText;
        set => SetField(ref _seedText, value);
    }

    /// <summary>返回当前选中存档的目录（不存在返回 null）。</summary>
    private string? SelectedSavePath()
    {
        if (SelectedSave is null) return null;
        var p = Path.Combine(_gameRoot, "saves", SelectedSave);
        return Directory.Exists(p) ? p : null;
    }

    // ── backupsave：为选中存档创建一份手动备份（zip）──
    private async Task BackupAsync()
    {
        var name = SelectedSave;
        var path = SelectedSavePath();
        if (name is null || path is null) { Status = "请先选择存档"; return; }
        Busy = true;
        Status = $"正在备份「{name}」…";
        try
        {
            var result = await Task.Run(() =>
                BackupManager.Create(_gameRoot, path, BackupKind.Save, $"手动备份 {name}", auto: false));
            ActionResult = result.Ok
                ? $"已备份 {name}（{result.Record?.SizeText ?? "?"}）"
                : $"备份失败：{result.Error}";
            Status = result.Ok ? "备份完成" : "备份失败";
        }
        catch (Exception ex)
        {
            ActionResult = $"备份异常：{ex.Message}";
            Status = "备份异常";
        }
        finally
        {
            Busy = false;
            Refresh();
        }
    }

    // ── deletesave：删除选中存档（带二次确认）──
    private async Task DeleteAsync()
    {
        var name = SelectedSave;
        var path = SelectedSavePath();
        if (name is null || path is null) { Status = "请先选择存档"; return; }
        if (!await UIService.ConfirmAsync(
                $"确定删除存档「{name}」及其全部数据？此操作不可撤销。", "确认删除", danger: true))
        {
            Status = "已取消删除";
            return;
        }
        Busy = true;
        Status = $"正在删除「{name}」…";
        try
        {
            await Task.Run(() => Directory.Delete(path, recursive: true));
            ActionResult = $"已删除存档 {name}";
            Status = "删除完成";
        }
        catch (Exception ex)
        {
            ActionResult = $"删除失败：{ex.Message}";
            Status = "删除失败";
        }
        finally
        {
            Busy = false;
            Refresh();
        }
    }

    // ── restore：把选中存档回滚到其最新备份 ──
    private async Task RestoreAsync()
    {
        var name = SelectedSave;
        var path = SelectedSavePath();
        if (name is null || path is null) { Status = "请先选择存档"; return; }
        var savesDir = SaveCompatibilityDetector.SavesDir(_gameRoot);
        var backups = SaveCompatibilityDetector.FindBackups(savesDir, name);
        if (backups.Count == 0) { Status = $"{name} 没有可回滚的备份"; return; }
        Busy = true;
        Status = $"正在回滚「{name}」到最新备份…";
        try
        {
            var latest = backups[^1];
            var replaced = SaveDowngrader.RestoreBackupAsync(latest.BackupPath, path);
            ActionResult = string.IsNullOrEmpty(replaced)
                ? $"已回滚 {name} 到备份（{latest.CreatedUtc:yyyy-MM-dd HH:mm}）"
                : $"已回滚 {name} 到备份（原档另存于 {Path.GetFileName(replaced)}）";
            Status = "回滚完成";
        }
        catch (Exception ex)
        {
            ActionResult = $"回滚异常：{ex.Message}";
            Status = "回滚异常";
        }
        finally
        {
            Busy = false;
            Refresh();
        }
    }

    // ── extractseed：从 level.dat 读取 RandomSeed 并交予 View 复制到剪贴板 ──
    private void ExtractSeed()
    {
        var name = SelectedSave;
        var path = SelectedSavePath();
        if (name is null || path is null) { Status = "请先选择存档"; return; }
        var levelDat = SaveCompatibilityDetector.LevelDatPath(path);
        if (!File.Exists(levelDat)) { Status = "找不到 level.dat"; return; }
        try
        {
            var root = NbtFile.ReadGzip(levelDat);
            var seedTag = root.Find("RandomSeed");
            if (seedTag is null || seedTag.Type != NbtTagType.Long)
            {
                Status = "未找到种子（RandomSeed）";
                return;
            }
            SeedText = seedTag.LongValue.ToString();
            Status = $"种子 {SeedText} 已复制到剪贴板";
            Services.ToastService.Show("种子", $"{name}: {SeedText}", ToastKind.Success);
        }
        catch (Exception ex)
        {
            Status = $"种子提取失败：{ex.Message}";
        }
    }
}
