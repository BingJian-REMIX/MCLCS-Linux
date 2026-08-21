using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Statistics;
using MCLCS.Linux.App.Services;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 年度游戏报告（对齐 WPF AnnualReportView）：把会话流水聚合成可展示 / 可导出的年度总结，
/// 并支持导出 / 导入分享 Token（完全离线，Base64Url + Deflate）。
/// 核心聚合逻辑全部复用 Core.Statistics.AnnualReport。
/// </summary>
public class AnnualReportViewModel : ObservableObject
{
    private readonly string _gameRoot = LauncherService.Instance.GameRoot;

    private int _year = DateTime.Now.Year;
    private AnnualReportData _data = new();
    private AnnualReportData? _shared;
    private string _tokenText = "";
    private string _importText = "";
    private string _narrative = "";
    private string _status = LocaleManager.T("status.ready");
    private bool _busy;

    /// <summary>可选择的年份（当前年前 4 年 … 当前年）。</summary>
    public int[] Years { get; } =
        Enumerable.Range(0, 5).Select(o => DateTime.Now.Year - o).Reverse().ToArray();

    public int Year
    {
        get => _year;
        set
        {
            if (!SetField(ref _year, value)) return;
            _ = GenerateAsync();
        }
    }

    /// <summary>本地生成的报告。</summary>
    public AnnualReportData Data
    {
        get => _data;
        set => SetField(ref _data, value);
    }

    /// <summary>从分享 Token 导入的报告（无则 null）。</summary>
    public AnnualReportData? Shared
    {
        get => _shared;
        set
        {
            SetField(ref _shared, value);
            OnPropertyChanged(nameof(SharedTitleText));
        }
    }

    public string SharedTitleText => Shared is not null ? AnnualReport.Title(Shared) : "";
    public string TitleText => AnnualReport.Title(Data);

    /// <summary>一句话本地解读（不依赖 AI）。</summary>
    public string Narrative
    {
        get => _narrative;
        set => SetField(ref _narrative, value);
    }

    public string TokenText
    {
        get => _tokenText;
        set => SetField(ref _tokenText, value);
    }

    public string ImportText
    {
        get => _importText;
        set => SetField(ref _importText, value);
    }

    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public bool IsBusy
    {
        get => _busy;
        set => SetField(ref _busy, value);
    }

    public ObservableCollection<MonthlyBar> MonthlyBars { get; } = new();
    public ObservableCollection<VersionRow> TopVersionRows { get; } = new();

    public ICommand GenerateCommand => new AsyncRelayCommand(_ => GenerateAsync(), _ => !IsBusy);
    public ICommand CopyTokenCommand => new RelayCommand(_ => CopyToken());
    public ICommand ExportMdCommand => new RelayCommand(_ => ExportMarkdown());
    public ICommand ImportCommand => new RelayCommand(_ => ImportToken());
    public ICommand ClearSharedCommand => new RelayCommand(_ => ClearShared());

    public AnnualReportViewModel()
    {
        _ = GenerateAsync();
    }

    private async Task GenerateAsync()
    {
        IsBusy = true;
        try
        {
            var d = await Task.Run(() => AnnualReport.GenerateFrom(_gameRoot, _year));
            Data = d;
            TokenText = d.HasData ? AnnualReport.ExportToken(d) : "";
            Narrative = d.HasData ? Interpret(d) : "今年还没有游玩记录，去开一局吧。";
            RebuildBars(d);
            RebuildVersions(d);
            Shared = null;
            Status = d.HasData
                ? $"{d.Year} 年报告已生成 · 称号「{TitleText}」"
                : $"{d.Year} 年还没有游玩记录";
        }
        catch (Exception ex)
        {
            Status = $"生成报告失败：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(TitleText));
        }
    }

    /// <summary>把月度时长重算成柱状（最大月满格 160px）。</summary>
    private void RebuildBars(AnnualReportData d)
    {
        MonthlyBars.Clear();
        var max = d.MonthlyMinutes.Max();
        for (var i = 0; i < 12; i++)
        {
            var hours = Math.Round(d.MonthlyMinutes[i] / 60, 1);
            var ratio = max <= 0 ? 0 : d.MonthlyMinutes[i] / max;
            MonthlyBars.Add(new MonthlyBar
            {
                Month = i + 1,
                Hours = hours,
                BarWidth = Math.Max(2, (int)Math.Round(ratio * 160))
            });
        }
    }

    private void RebuildVersions(AnnualReportData d)
    {
        TopVersionRows.Clear();
        foreach (var (ver, min) in d.TopVersions)
            TopVersionRows.Add(new VersionRow { Version = ver, Hours = Math.Round(min / 60, 1) });
    }

    /// <summary>本地生成一句年度解读（不依赖 AI）。</summary>
    private static string Interpret(AnnualReportData d)
    {
        var parts = new List<string>
        {
            $"这一年你启动了 {d.SessionCount} 次，总共玩了 {d.TotalHours} 小时，",
            $"在 {d.ActiveDays} 天里留下了足迹，最长连续 {d.LongestStreakDays} 天没断过。"
        };
        if (d.PeakMonth > 0) parts.Add($"{d.PeakMonth} 月是你最投入的月份。");
        if (d.PeakHour >= 0) parts.Add($"你最常在 {d.PeakHour}:00 前后开局。");
        if (d.NightOwlRatio >= 0.2) parts.Add($"有 {d.NightOwlRatio:P0} 的开局发生在凌晨，是个不折不扣的夜猫子。");
        if (d.CrashCount > 0) parts.Add($"全年崩溃 {d.CrashCount} 次（{d.CrashRate:P1}），");
        if (d.TopVersions.Count > 0) parts.Add($"最爱的是 {d.TopVersions[0].Version}。");
        return string.Concat(parts);
    }

    private void CopyToken()
    {
        if (string.IsNullOrWhiteSpace(TokenText))
        {
            Status = "没有可导出的 Token（该年无数据）";
            return;
        }
        try
        {
            // 复制到系统剪贴板由 View 订阅 SeedText/TokenText 触发；此处先给出状态回显
            Status = "分享 Token 已显示在上方，可手动复制";
            Services.ToastService.Show("年度报告", "分享 Token 已生成", ToastKind.Success);
        }
        catch (Exception ex)
        {
            Status = $"复制失败：{ex.Message}";
        }
    }

    private async void ExportMarkdown()
    {
        if (!Data.HasData) { Status = "该年没有数据可导出"; return; }

        var path = await UIService.SaveFileAsync("保存年度报告", $"{Data.Year} 年度报告.md", "*.md");
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            File.WriteAllText(path, AnnualReport.RenderMarkdown(Data));
            Services.ToastService.Show("年度报告", $"已导出到 {Path.GetFileName(path)}", ToastKind.Success);
            Status = $"已导出：{path}";
        }
        catch (Exception ex)
        {
            Status = $"导出失败：{ex.Message}";
        }
    }

    private void ImportToken()
    {
        var token = ImportText?.Trim();
        if (string.IsNullOrWhiteSpace(token)) { Status = "请先粘贴分享 Token"; return; }

        var d = AnnualReport.ImportToken(token);
        if (d is null)
        {
            Status = "Token 无法解析（格式错误或已损坏）";
            Services.ToastService.Show("年度报告", "Token 解析失败", ToastKind.Error);
            return;
        }

        Shared = d;
        Status = $"已载入分享报告：{d.Year} 年 · 称号「{AnnualReport.Title(d)}」";
        Services.ToastService.Show("年度报告", $"已导入 {d.Year} 年报告", ToastKind.Success);
    }

    private void ClearShared()
    {
        Shared = null;
        ImportText = "";
        Status = "已清除导入的分享报告";
    }
}

/// <summary>月度分布的柱状条目（视图友好）。</summary>
public class MonthlyBar : ObservableObject
{
    public int Month { get; init; }
    public double Hours { get; init; }
    public int BarWidth { get; init; }
}

/// <summary>版本排行的一行（视图友好）。</summary>
public class VersionRow : ObservableObject
{
    public string Version { get; init; } = "";
    public double Hours { get; init; }
}
