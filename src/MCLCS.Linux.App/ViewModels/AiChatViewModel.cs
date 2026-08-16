using System.Windows.Input;
using MCLCS.Core.Ai;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 工具箱 → AI 助手（对齐 WPF Assistant / OllamaManager）：检查本地 Ollama 服务状态，
/// 并把粘贴的崩溃日志交给 Assistant 解读。
/// </summary>
public class AiChatViewModel : ObservableObject
{
    private string _input = "";
    public string Input
    {
        get => _input;
        set => SetField(ref _input, value);
    }

    private string _output = "";
    public string Output
    {
        get => _output;
        set => SetField(ref _output, value);
    }

    private string _ollamaStatus = "未知";
    public string OllamaStatus
    {
        get => _ollamaStatus;
        set => SetField(ref _ollamaStatus, value);
    }

    private bool _ollamaRunning;
    public bool OllamaRunning
    {
        get => _ollamaRunning;
        set => SetField(ref _ollamaRunning, value);
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

    public ICommand CheckStatusCommand => new AsyncRelayCommand(_ => CheckStatusAsync());
    public ICommand SendCommand => new AsyncRelayCommand(_ => SendAsync());

    private async Task CheckStatusAsync()
    {
        try
        {
            var s = await OllamaManager.GetServiceStatusAsync();
            OllamaStatus = s.ToString();
            OllamaRunning = s == OllamaServiceStatus.Running;
            Status = $"Ollama 服务：{OllamaStatus}";
        }
        catch (Exception ex)
        {
            Status = $"状态检查失败：{ex.Message}";
        }
    }

    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(Input))
        {
            Status = "请输入内容（可粘贴崩溃日志）";
            return;
        }
        Busy = true;
        Status = "AI 正在分析…";
        try
        {
            var ans = await Assistant.InterpretCrashAsync(Input);
            Output = ans;
            Status = "分析完成";
        }
        catch (Exception ex)
        {
            Output = $"分析失败：{ex.Message}";
            Status = "分析失败";
        }
        finally
        {
            Busy = false;
        }
    }
}
