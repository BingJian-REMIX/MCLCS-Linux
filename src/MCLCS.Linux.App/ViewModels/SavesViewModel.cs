using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Save;
using MCLCS.Core.Utils;

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
}
