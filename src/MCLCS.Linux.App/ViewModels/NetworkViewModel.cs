using System.Collections.ObjectModel;
using System.Windows.Input;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Toolbox;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 工具箱 → 网络诊断（对齐 WPF NetworkDiagnostics）：探测核心镜像 / API 端点连通性与延迟。
/// </summary>
public class NetworkViewModel : ObservableObject
{
    private ObservableCollection<DiagnosticResult> _results = new();
    public ObservableCollection<DiagnosticResult> Results
    {
        get => _results;
        set => SetField(ref _results, value);
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

    public ICommand DiagnoseCommand => new AsyncRelayCommand(_ => DiagnoseAsync());

    private async Task DiagnoseAsync()
    {
        Busy = true;
        Status = "正在诊断网络连通性…";
        try
        {
            var list = await NetworkDiagnostics.DiagnoseAsync();
            Results = new ObservableCollection<DiagnosticResult>(list);
            var ok = 0;
            foreach (var r in list) if (r.Reachable) ok++;
            Status = $"诊断完成：{ok}/{list.Count} 个端点可达";
        }
        catch (Exception ex)
        {
            Status = $"诊断失败：{ex.Message}";
        }
        finally
        {
            Busy = false;
        }
    }
}
