using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using MCLCS.Core.Launcher;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Utils;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 工具箱 → 崩溃分析（对齐 WPF CrashAnalyzer）：列出崩溃报告、读取原文、分析异常类型与修复建议。
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

    private string _status = LocaleManager.T("status.ready");
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public ICommand RefreshCommand => new RelayCommand(_ => Refresh());
    public ICommand AnalyzeCommand => new RelayCommand(_ => AnalyzeSelected());

    public void Refresh()
    {
        Reports = new ObservableCollection<string>(CrashDetector.FindAllCrashReports(_gameRoot));
        Status = Reports.Count == 0
            ? LocaleManager.T("lbl.no_crash")
            : $"找到 {Reports.Count} 个崩溃报告";
    }

    private void LoadSelected()
    {
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
    }
}
