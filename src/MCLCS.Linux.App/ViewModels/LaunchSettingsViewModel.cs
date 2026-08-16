using System.Collections.ObjectModel;
using System.Windows.Input;
using MCLCS.Core.Launcher;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Profiles;
using MCLCS.Core.Utils;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 设置 → 启动（对齐 WPF LauncherProfile 的启动策略）：崩溃自动修复策略、
/// Java 发行商、缺失 Mod 依赖自动安装策略。
/// </summary>
public class LaunchSettingsViewModel : ObservableObject
{
    private readonly string _gameRoot = GameConstants.DefaultGameRoot;
    private readonly LauncherProfile _profile;

    public ObservableCollection<CrashRepairPolicy> RepairPolicies { get; } = new(
        new[] { CrashRepairPolicy.Always, CrashRepairPolicy.Ask, CrashRepairPolicy.Never });

    public ObservableCollection<JavaVendor> JavaVendors { get; } = new(
        new[] { JavaVendor.Auto, JavaVendor.Temurin, JavaVendor.Oracle });

    public ObservableCollection<AutoInstallPolicy> AutoInstallPolicies { get; } = new(
        new[] { AutoInstallPolicy.Always, AutoInstallPolicy.Ask, AutoInstallPolicy.Never });

    private CrashRepairPolicy _repairPolicy;
    public CrashRepairPolicy RepairPolicy
    {
        get => _repairPolicy;
        set => SetField(ref _repairPolicy, value);
    }

    private JavaVendor _javaVendor;
    public JavaVendor JavaVendor
    {
        get => _javaVendor;
        set => SetField(ref _javaVendor, value);
    }

    private AutoInstallPolicy _autoInstall;
    public AutoInstallPolicy AutoInstall
    {
        get => _autoInstall;
        set => SetField(ref _autoInstall, value);
    }

    private string _status = LocaleManager.T("status.ready");
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public LaunchSettingsViewModel()
    {
        _profile = ProfileStore.Load(_gameRoot);
        _repairPolicy = _profile.RepairPolicy;
        _javaVendor = _profile.PreferredJavaVendor;
        _autoInstall = _profile.AutoInstallMissingMods;
    }

    public ICommand SaveCommand => new RelayCommand(_ => Save());

    private void Save()
    {
        try
        {
            _profile.RepairPolicy = RepairPolicy;
            _profile.PreferredJavaVendor = JavaVendor;
            _profile.AutoInstallMissingMods = AutoInstall;
            ProfileStore.Save(_profile);
            Status = "启动设置已保存";
        }
        catch (Exception ex)
        {
            Status = $"保存失败：{ex.Message}";
        }
    }
}
