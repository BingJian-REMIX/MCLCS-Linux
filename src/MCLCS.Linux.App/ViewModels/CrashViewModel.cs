using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using MCLCS.Core.Launcher;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Profiles;
using MCLCS.Core.Utils;
using MCLCS.Linux.App.Services;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 工具箱 → 崩溃分析（对齐 WPF CrashAnalyzer）：列出崩溃报告、读取原文、分析异常类型与修复建议，
/// 并能根据分析结果构建"自动修复方案"（含 §四.2 降级联动恢复）。
/// </summary>
public class CrashViewModel : ObservableObject
{
    private readonly string _gameRoot = GameConstants.DefaultGameRoot;

    private ObservableCollection<string> _reports = new();
    public ObservableCollection<string> Reports
    {
        get => _reports;
        set => SetField(ref _reports, value);
    }

    private string? _selectedReport;
    public string? SelectedReport
    {
        get => _selectedReport;
        set
        {
            if (SetField(ref _selectedReport, value))
                LoadSelected();
        }
    }

    private string _reportText = "";
    public string ReportText
    {
        get => _reportText;
        set => SetField(ref _reportText, value);
    }

    private CrashAnalysis? _analysis;
    public CrashAnalysis? Analysis
    {
        get => _analysis;
        set => SetField(ref _analysis, value);
    }

    private CrashRepairPlan? _repairPlan;
    /// <summary>当前崩溃对应的自动修复方案（可空；null 表示未构建）。</summary>
    public CrashRepairPlan? RepairPlan
    {
        get => _repairPlan;
        set
        {
            if (SetField(ref _repairPlan, value))
                NotifyRepairChanged();
        }
    }

    private string _status = LocaleManager.T("status.ready");
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    private bool _repairInProgress;
    public bool RepairInProgress
    {
        get => _repairInProgress;
        set => SetField(ref _repairInProgress, value);
    }

    private string _repairStatus = "";
    public string RepairStatus
    {
        get => _repairStatus;
        set => SetField(ref _repairStatus, value);
    }

    // ---- 修复方案派生属性（供 UI 绑定）----

    public bool CanRepair => RepairPlan?.CanRepair ?? false;
    public string RepairTitle => RepairPlan?.Title ?? "";
    public string RepairDescription => RepairPlan?.Description ?? "";
    public List<string> RepairSteps => RepairPlan?.Steps ?? new();
    public bool IsConflictPlan => RepairPlan?.Strategy == RepairStrategy.DisableConflictingMods;
    public bool IsMissingDepPlan => RepairPlan?.Strategy == RepairStrategy.InstallMissingModDependency;
    public List<ModConflictInfo> ConflictingMods => RepairPlan?.ConflictingMods ?? new();
    public List<string> MissingDependencies => RepairPlan?.MissingModDependencies ?? new();
    public bool HasDowngradeRecovery => RepairPlan?.DowngradeRecovery?.Applicable ?? false;
    public string DowngradeRecoveryReason => RepairPlan?.DowngradeRecovery?.Reason ?? "";

    /// <summary>非破坏性提示（方案保证不删除/不修改游戏原文件）。</summary>
    public string NonDestructiveNote =>
        (RepairPlan?.NonDestructive ?? false)
            ? "本修复全程非破坏性：仅修改启动器配置、外部 Java 或依赖缓存，或把冲突 Mod 重命名为 .disabled，绝不删除/改写游戏原文件（存档、配置、mod、版本 jar 等）。"
            : "";

    public ICommand RefreshCommand => new RelayCommand(_ => Refresh());
    public ICommand AnalyzeCommand => new RelayCommand(_ => AnalyzeSelected());
    public ICommand TryRepairCommand =>
        new AsyncRelayCommand(_ => TryRepairAsync(), _ => CanRepair && !RepairInProgress);
    public ICommand DowngradeRecoveryCommand =>
        new AsyncRelayCommand(p => DowngradeRecoveryAsync(p), _ => HasDowngradeRecovery && !RepairInProgress);

    public void Refresh()
    {
        Reports = new ObservableCollection<string>(CrashDetector.FindAllCrashReports(_gameRoot));
        Status = Reports.Count == 0
            ? LocaleManager.T("lbl.no_crash")
            : $"找到 {Reports.Count} 个崩溃报告";
    }

    private void LoadSelected()
    {
        RepairPlan = null;
        RepairStatus = "";
        if (_selectedReport is null)
        {
            ReportText = "";
            Analysis = null;
            return;
        }
        try
        {
            ReportText = File.ReadAllText(_selectedReport);
        }
        catch (Exception ex)
        {
            ReportText = ex.Message;
        }
        AnalyzeSelected();
    }

    private void AnalyzeSelected()
    {
        Analysis = string.IsNullOrEmpty(ReportText) ? null : CrashAnalyzer.Analyze(ReportText);
        BuildRepairPlan();
    }

    /// <summary>
    /// 根据崩溃分析构建自动修复方案。所有修复均由
    /// <see cref="LauncherService.ApplyRepairAsync"/> 在执行时判定能否成功（非破坏性）。
    /// </summary>
    private void BuildRepairPlan()
    {
        if (Analysis is null)
        {
            RepairPlan = null;
            return;
        }

        var profile = ProfileStore.Load(_gameRoot);
        var plan = CrashRepairEngine.BuildPlan(Analysis, profile, null, _gameRoot, profile.LastVersionId);

        // 冲突 Mod 方案：默认选中第一个保留，UI 可改
        if (plan.Strategy == RepairStrategy.DisableConflictingMods && plan.ConflictingMods.Count > 0)
            plan.ConflictingMods[0].IsKeepSelected = true;

        RepairPlan = plan;
    }

    private async Task TryRepairAsync()
    {
        if (RepairPlan is null || !RepairPlan.CanRepair) return;

        // 冲突 Mod 方案：把用户选中的"保留项"写入 KeepModFile
        if (RepairPlan.Strategy == RepairStrategy.DisableConflictingMods)
        {
            var keep = RepairPlan.ConflictingMods.FirstOrDefault(m => m.IsKeepSelected);
            if (keep is not null) RepairPlan.KeepModFile = keep.FilePath;
        }

        RepairInProgress = true;
        RepairStatus = "正在尝试自动修复…";
        var ok = await LauncherService.Instance.ApplyRepairAsync(_repairPlan);
        RepairInProgress = false;
        RepairStatus = ok
            ? "自动修复已执行完成，可重新启动游戏验证。"
            : "自动修复未成功完成，请查看日志或按下方建议手动处理。";
        Status = RepairStatus;
    }

    private async Task DowngradeRecoveryAsync(object? parameter)
    {
        if (RepairPlan is null) return;

        var action = parameter as string;
        RepairPlan.Strategy = action switch
        {
            "RevertBackup" => RepairStrategy.RevertDowngradeBackup,
            "RetryOther" => RepairStrategy.RetryDowngradeOtherMethod,
            "InstallOriginal" => RepairStrategy.InstallOriginalVersion,
            _ => RepairPlan.Strategy
        };

        RepairInProgress = true;
        RepairStatus = "正在执行降级联动恢复…";
        var ok = await LauncherService.Instance.ApplyRepairAsync(_repairPlan);
        RepairInProgress = false;
        RepairStatus = ok
            ? "降级联动恢复已完成，可重新启动游戏验证。"
            : "降级联动恢复未成功，请查看日志（原存档始终保留在备份目录）。";
        Status = RepairStatus;
    }

    private void NotifyRepairChanged()
    {
        OnPropertyChanged(nameof(CanRepair));
        OnPropertyChanged(nameof(RepairTitle));
        OnPropertyChanged(nameof(RepairDescription));
        OnPropertyChanged(nameof(RepairSteps));
        OnPropertyChanged(nameof(IsConflictPlan));
        OnPropertyChanged(nameof(IsMissingDepPlan));
        OnPropertyChanged(nameof(ConflictingMods));
        OnPropertyChanged(nameof(MissingDependencies));
        OnPropertyChanged(nameof(HasDowngradeRecovery));
        OnPropertyChanged(nameof(DowngradeRecoveryReason));
        OnPropertyChanged(nameof(NonDestructiveNote));
    }
}
