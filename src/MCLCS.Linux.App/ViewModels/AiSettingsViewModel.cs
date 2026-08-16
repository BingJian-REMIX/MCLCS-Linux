using System;
using System.Windows.Input;
using MCLCS.Core.Ai;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Profiles;
using MCLCS.Core.Utils;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 设置 → AI（对齐 WPF OllamaManager / 外部 API 配置）：展示本地 Ollama 服务状态并允许检查，
/// 同时把 AI 配置（启用 / 部署方式 / 端点 / 模型 / Key / 本地模型 / 三项能力开关）持久化到
/// LauncherProfile.Ai，并在保存后同步到全局 Assistant.Config 供运行时真正消费。
/// </summary>
public class AiSettingsViewModel : ObservableObject
{
    private readonly LauncherProfile _profile;
    private readonly AiConfig _ai;

    private bool _enabled;
    private AiMode _mode = AiMode.External;
    private string _endpoint = "https://api.openai.com/v1/chat/completions";
    private string _model = "gpt-4o-mini";
    private string? _apiKey;
    private string _selectedLocalModel = OllamaModels.Default.OllamaTag;
    private bool _crashInterpret = true;
    private bool _recommendReason = true;
    private bool _modTranslate = true;

    // ---- 服务状态（只读探测）----
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

    // ---- 配置（绑定到 profile.Ai 的副本，Save 时回写）----
    public bool Enabled { get => _enabled; set => SetField(ref _enabled, value); }
    public AiMode Mode
    {
        get => _mode;
        set
        {
            if (SetField(ref _mode, value))
            {
                OnPropertyChanged(nameof(IsLocalMode));
                OnPropertyChanged(nameof(IsExternalMode));
            }
        }
    }
    public bool IsLocalMode => _mode == AiMode.Local;
    public bool IsExternalMode => _mode != AiMode.Local;
    public string Endpoint { get => _endpoint; set => SetField(ref _endpoint, value); }
    public string Model { get => _model; set => SetField(ref _model, value); }
    public string? ApiKey { get => _apiKey; set => SetField(ref _apiKey, value); }
    public string SelectedLocalModel { get => _selectedLocalModel; set => SetField(ref _selectedLocalModel, value); }
    public bool CrashInterpret { get => _crashInterpret; set => SetField(ref _crashInterpret, value); }
    public bool RecommendReason { get => _recommendReason; set => SetField(ref _recommendReason, value); }
    public bool ModTranslate { get => _modTranslate; set => SetField(ref _modTranslate, value); }

    public ICommand CheckCommand => new AsyncRelayCommand(_ => CheckAsync());
    public ICommand SaveCommand => new RelayCommand(_ => Save());

    public AiSettingsViewModel()
    {
        _profile = ProfileStore.Load(GameConstants.DefaultGameRoot);
        _ai = _profile.Ai;
        _enabled = _ai.Enabled;
        _mode = _ai.Mode;
        _endpoint = _ai.Endpoint;
        _model = _ai.Model;
        _apiKey = _ai.ApiKey;
        _selectedLocalModel = _ai.SelectedLocalModel;
        _crashInterpret = _ai.CrashInterpret;
        _recommendReason = _ai.RecommendReason;
        _modTranslate = _ai.ModTranslate;
    }

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

    private void Save()
    {
        try
        {
            _ai.Enabled = _enabled;
            _ai.Mode = _mode;
            _ai.Endpoint = _endpoint;
            _ai.Model = _model;
            _ai.ApiKey = _apiKey;
            _ai.SelectedLocalModel = _selectedLocalModel;
            _ai.CrashInterpret = _crashInterpret;
            _ai.RecommendReason = _recommendReason;
            _ai.ModTranslate = _modTranslate;
            ProfileStore.Save(_profile);
            // 立即生效：运行时 AI 助手读取全局配置
            Assistant.Config = _profile.Ai;
            Status = "AI 设置已保存";
        }
        catch (Exception ex)
        {
            Status = $"保存失败：{ex.Message}";
        }
    }
}
