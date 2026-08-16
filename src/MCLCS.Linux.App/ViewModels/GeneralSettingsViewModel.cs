using System.Windows.Input;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Profiles;
using MCLCS.Core.Utils;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 设置 → 常规（对齐 WPF LauncherProfile）：编辑内存 / JVM 参数 / Java 路径 / 分辨率，
/// 持久化到 mclcs_profile.json。
/// </summary>
public class GeneralSettingsViewModel : ObservableObject
{
    private readonly string _gameRoot = GameConstants.DefaultGameRoot;
    private readonly LauncherProfile _profile;

    private string _username;
    public string Username
    {
        get => _username;
        set => SetField(ref _username, value);
    }

    private string _maxMemoryMb;
    public string MaxMemoryMb
    {
        get => _maxMemoryMb;
        set => SetField(ref _maxMemoryMb, value);
    }

    private string _minMemoryMb;
    public string MinMemoryMb
    {
        get => _minMemoryMb;
        set => SetField(ref _minMemoryMb, value);
    }

    private string _javaPath;
    public string JavaPath
    {
        get => _javaPath;
        set => SetField(ref _javaPath, value);
    }

    private string _extraJvmArgs;
    public string ExtraJvmArgs
    {
        get => _extraJvmArgs;
        set => SetField(ref _extraJvmArgs, value);
    }

    private string _status = LocaleManager.T("status.ready");
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public GeneralSettingsViewModel()
    {
        _profile = ProfileStore.Load(_gameRoot);
        _username = _profile.DefaultUsername;
        _maxMemoryMb = _profile.MaxMemoryMb.ToString();
        _minMemoryMb = _profile.MinMemoryMb.ToString();
        _javaPath = _profile.JavaPath ?? "";
        _extraJvmArgs = string.Join(" ", _profile.ExtraJvmArgs);
    }

    public ICommand SaveCommand => new RelayCommand(_ => Save());

    private void Save()
    {
        try
        {
            _profile.DefaultUsername = string.IsNullOrWhiteSpace(Username) ? "Player" : Username.Trim();
            if (int.TryParse(MaxMemoryMb, out var max) && max > 0) _profile.MaxMemoryMb = max;
            if (int.TryParse(MinMemoryMb, out var min) && min > 0) _profile.MinMemoryMb = min;
            _profile.JavaPath = string.IsNullOrWhiteSpace(JavaPath) ? null : JavaPath.Trim();
            _profile.ExtraJvmArgs = ExtraJvmArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            ProfileStore.Save(_profile);
            Status = "设置已保存";
        }
        catch (Exception ex)
        {
            Status = $"保存失败：{ex.Message}";
        }
    }
}
