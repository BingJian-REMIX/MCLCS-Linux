using System.Collections.ObjectModel;
using System.Windows.Input;
using MCLCS.Core.Launcher;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Profiles;
using MCLCS.Core.Utils;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 设置 → 启动（对齐 WPF Settings→Launch）：Java 路径 + 自动检测、内存分配、默认用户名、
/// 额外 JVM 参数、崩溃自动修复策略、Java 发行商、缺失 Mod 依赖自动安装策略。
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

    /// <summary>Java 可执行文件路径（对齐 WPF Settings→Launch 的「Java 路径」）。
    /// 留空表示启动时自动检测最优 Java。</summary>
    private string _javaPath = "";
    public string JavaPath
    {
        get => _javaPath;
        set => SetField(ref _javaPath, value);
    }

    private string _defaultUsername = "";
    public string DefaultUsername
    {
        get => _defaultUsername;
        set => SetField(ref _defaultUsername, value);
    }

    private string _minMemoryMb = "512";
    public string MinMemoryMb
    {
        get => _minMemoryMb;
        set => SetField(ref _minMemoryMb, value);
    }

    private string _maxMemoryMb = "2048";
    public string MaxMemoryMb
    {
        get => _maxMemoryMb;
        set => SetField(ref _maxMemoryMb, value);
    }

    private string _extraJvmArgs = "";
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

    public LaunchSettingsViewModel()
    {
        _profile = ProfileStore.Load(_gameRoot);
        _repairPolicy = _profile.RepairPolicy;
        _javaVendor = _profile.PreferredJavaVendor;
        _autoInstall = _profile.AutoInstallMissingMods;
        _javaPath = _profile.JavaPath ?? "";
        _defaultUsername = _profile.DefaultUsername;
        _minMemoryMb = _profile.MinMemoryMb.ToString();
        _maxMemoryMb = _profile.MaxMemoryMb.ToString();
        _extraJvmArgs = string.Join(" ", _profile.ExtraJvmArgs);
        AutoDetectJavaCommand = new AsyncRelayCommand(_ => AutoDetectJavaAsync());
    }

    public ICommand SaveCommand => new RelayCommand(_ => Save());

    public ICommand AutoDetectJavaCommand { get; }

    private void Save()
    {
        try
        {
            _profile.RepairPolicy = RepairPolicy;
            _profile.PreferredJavaVendor = JavaVendor;
            _profile.AutoInstallMissingMods = AutoInstall;
            _profile.JavaPath = string.IsNullOrWhiteSpace(JavaPath) ? null : JavaPath;
            _profile.DefaultUsername = string.IsNullOrWhiteSpace(DefaultUsername) ? "Player" : DefaultUsername.Trim();
            if (int.TryParse(MinMemoryMb, out var min) && min > 0) _profile.MinMemoryMb = min;
            if (int.TryParse(MaxMemoryMb, out var max) && max > 0) _profile.MaxMemoryMb = max;
            _profile.ExtraJvmArgs = ExtraJvmArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            ProfileStore.Save(_profile);
            Status = "启动设置已保存";
        }
        catch (Exception ex)
        {
            Status = $"保存失败：{ex.Message}";
        }
    }

    /// <summary>自动检测最优 Java（对齐 WPF AutoDetectJavaAsync）：取版本 ≥ 最低要求的 Java，写入 JavaPath。</summary>
    private async Task AutoDetectJavaAsync()
    {
        try
        {
            var best = await JavaDetector.FindBestAsync(GameConstants.MinimumJavaMajorVersion);
            if (best is not null)
            {
                JavaPath = best.JavaExe;
                Status = $"已选择 Java {best.MajorVersion}（{best.JavaExe}）";
            }
            else
            {
                Status = $"未检测到 Java {GameConstants.MinimumJavaMajorVersion}+，请手动指定路径";
            }
        }
        catch (Exception ex)
        {
            Status = $"Java 检测失败：{ex.Message}";
        }
    }
}
