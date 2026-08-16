using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Toolbox;
using MCLCS.Core.Utils;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 工具箱 → 日志（对齐 WPF LogManager）：列出 / 读取 / 搜索 / 过滤 / 导出游戏日志与崩溃报告。
/// 文件级操作，无副作用（导出为复制）。
/// </summary>
public class LogViewModel : ObservableObject
{
    private readonly string _gameRoot = GameConstants.DefaultGameRoot;

    private ObservableCollection<LogFileInfo> _logs = new();
    public ObservableCollection<LogFileInfo> Logs
    {
        get => _logs;
        set => SetField(ref _logs, value);
    }

    private LogFileInfo? _selectedLog;
    public LogFileInfo? SelectedLog
    {
        get => _selectedLog;
        set
        {
            if (SetField(ref _selectedLog, value))
                _ = LoadSelected();
        }
    }

    private ObservableCollection<LogLine> _lines = new();
    public ObservableCollection<LogLine> Lines
    {
        get => _lines;
        set => SetField(ref _lines, value);
    }

    private string _filterText = "";
    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetField(ref _filterText, value)) ApplyFilter();
        }
    }

    private bool _onlyErrors;
    public bool OnlyErrors
    {
        get => _onlyErrors;
        set
        {
            if (SetField(ref _onlyErrors, value)) ApplyFilter();
        }
    }

    private string _status = LocaleManager.T("status.ready");
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    private List<LogLine> _allLines = new();

    public ICommand RefreshCommand { get; }
    public ICommand ExportCommand { get; }

    public LogViewModel()
    {
        RefreshCommand = new RelayCommand(_ => Refresh());
        ExportCommand = new RelayCommand(_ => ExportSelected(), _ => SelectedLog is not null);
        Refresh();
    }

    public void Refresh()
    {
        Logs = new ObservableCollection<LogFileInfo>(LogManager.ListLogs(_gameRoot));
        Status = $"共 {Logs.Count} 个日志 / 崩溃报告文件";
    }

    private void ApplyFilter()
    {
        Lines = new ObservableCollection<LogLine>(LogManager.Filter(_allLines, FilterText, OnlyErrors));
    }

    private Task LoadSelected()
    {
        if (SelectedLog is null)
        {
            _allLines = new List<LogLine>();
            Lines = new ObservableCollection<LogLine>();
            return Task.CompletedTask;
        }
        try
        {
            var text = LogManager.ReadLog(SelectedLog.FullPath);
            _allLines = LogManager.ParseLines(text);
            ApplyFilter();
            Status = $"已加载 {SelectedLog.Name}（{_allLines.Count} 行）";
        }
        catch (Exception ex)
        {
            Status = $"读取失败：{ex.Message}";
        }
        return Task.CompletedTask;
    }

    private void ExportSelected()
    {
        if (SelectedLog is null) return;
        var dest = Path.Combine(Path.GetTempPath(), "mclcs_" + SelectedLog.Name);
        Status = LogManager.Export(SelectedLog.FullPath, dest)
            ? $"已导出到：{dest}"
            : "导出失败";
    }
}
