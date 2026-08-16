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
/// 额外 JVM 参数、崩溃自动修复策略、Java 发行商、缺失 Mod 依赖自动安装策略、
/// 游戏目录、启动前存档兼容性检测、启动预热、游戏内 HUD。
/// 各开关/路径在保存时持久化到 LauncherProfile，并由启动链路（GameHomeViewModel）真正消费。
/// </summary>
public class LaunchSettingsViewModel : ObservableObject
{
    private readonly string _profileRoot = GameConstants.DefaultGameRoot;
    private readonly LauncherProfile _profile;

    public ObservableCollection<CrashRepairPolicy> RepairPolicies { get; } = new(
        new[] { CrashRepairPolicy.Always, CrashRepairPolicy.Ask, CrashRepairPolicy.Never });

    public ObservableCollection<JavaVendor> JavaVendors { get; } = new(
        new[] { JavaVendor.Auto, JavaVendor.Temurin, JavaVendor.Oracle });

    public ObservableCollection<AutoInstallPolicy> AutoInstallPolicies { get; } = new(
        new[] { AutoInstallPolicy.Always, AutoInstallPolicy.Ask, AutoInstallPolicy.Never });

    public ObservableCollection<PrewarmMode> PrewarmModes { get; } = new(
        Enum.GetValues<PrewarmMode>());

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

    /// <summary>Java 可执行文件路径（对齐 WPF Settings→Launch 的「Java 路径」）。留空表示启动时自动检测最优 Java。</summary>
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

    // ---- 游戏目录 / 启动钩子（对齐 WPF Settings→Launch）----

    /// <summary>游戏目录（.minecraft）。留空或默认值表示使用系统默认目录。已链接 core：启动与扫描均以其为根。</summary>
    private string _gameRoot = "";
    public string GameRoot
    {
        get => _gameRoot;
        set => SetField(ref _gameRoot, value);
    }

    /// <summary>启动前存档兼容性检测（对齐 WPF LaunchCompatCheckEnabled）。</summary>
    private bool _launchCompatCheck;
    public bool LaunchCompatCheck
    {
        get => _launchCompatCheck;
        set => SetField(ref _launchCompatCheck, value);
    }

    /// <summary>启动预热模式（对齐 WPF Prewarm：Off/Light/Full）。</summary>
    private PrewarmMode _prewarmMode = PrewarmMode.Light;
    public PrewarmMode PrewarmMode
    {
        get => _prewarmMode;
        set => SetField(ref _prewarmMode, value);
    }

    /// <summary>游戏内 HUD 悬浮窗（对齐 WPF Hud.Enabled）。</summary>
    private bool _hudEnabled;
    public bool HudEnabled
    {
        get => _hudEnabled;
        set => SetField(ref _hudEnabled, value);
    }

    private string _status = LocaleManager.T("status.ready");
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public LaunchSettingsViewModel()
    {
        _profile = ProfileStore.Load(_profileRoot);
        _repairPolicy = _profile.RepairPolicy;
        _javaVendor = _profile.PreferredJavaVendor;
        _autoInstall = _profile.AutoInstallMissingMods;
        _javaPath = _profile.JavaPath ?? "";
        _defaultUsername = _profile.DefaultUsername;
        _minMemoryMb = _profile.MinMemoryMb.ToString();
        _maxMemoryMb = _profile.MaxMemoryMb.ToString();
        _extraJvmArgs = string.Join(" ", _profile.ExtraJvmArgs);
        _gameRoot = _profile.GameRoot;
        _launchCompatCheck = _profile.LaunchCompatCheckEnabled;
        _prewarmMode = _profile.Prewarm.Mode;
        _hudEnabled = _profile.Hud.Enabled;
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
            _profile.GameRoot = string.IsNullOrWhiteSpace(GameRoot) ? GameConstants.DefaultGameRoot : GameRoot.Trim();
            _profile.LaunchCompatCheckEnabled = LaunchCompatCheck;
            _profile.Prewarm.Mode = PrewarmMode;
            _profile.Hud.Enabled = HudEnabled;
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
