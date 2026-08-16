using System.Windows.Input;
using MCLCS.Core.Ai;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 设置 → AI（对齐 WPF OllamaManager）：展示本地 Ollama 服务状态并允许检查。
/// </summary>
public class AiSettingsViewModel : ObservableObject
{
    private string _status = LocaleManager.T("status.ready");
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    private string _serviceStatus = "未知";
    public string ServiceStatus
    {
        get => _serviceStatus;
        set => SetField(ref _serviceStatus, value);
    }

    private bool _busy;
    public bool Busy
    {
        get => _busy;
        set => SetField(ref _busy, value);
    }

    public ICommand CheckCommand => new AsyncRelayCommand(_ => CheckAsync());

    private async Task CheckAsync()
    {
        Busy = true;
        try
        {
            var s = await OllamaManager.GetServiceStatusAsync();
            ServiceStatus = s switch
            {
                OllamaServiceStatus.Running => "运行中",
                OllamaServiceStatus.Starting => "启动中",
                _ => "未运行"
            };
            Status = $"Ollama 服务：{ServiceStatus}";
        }
        catch (Exception ex)
        {
            Status = $"检查失败：{ex.Message}";
        }
        finally
        {
            Busy = false;
        }
    }
}
