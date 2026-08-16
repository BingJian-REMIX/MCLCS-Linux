using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
using MCLCS.Core.Auth;
using MCLCS.Core.Download;
using MCLCS.Core.Launcher;
using MCLCS.Core.Localization;
using MCLCS.Core.Models;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Profiles;
using MCLCS.Core.Utils;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 游戏主页视图模型（对齐 WPF GameViewModel 的快速启动区）：
/// 选择已安装版本 / 账户 / 内存 / Java，调用 Core.GameLauncher.LaunchAsync 启动游戏。
/// 离线账户用 Core.OfflineAuthenticator 生成与官方一致的离线 UUID；微软/第三方账号则复用存储的令牌。
/// </summary>
public class GameHomeViewModel : ObservableObject
{
    private readonly string _gameRoot = GameConstants.DefaultGameRoot;

    // ===== 已安装版本 =====
    private ObservableCollection<VersionEntryVm> _versions = new();
    public ObservableCollection<VersionEntryVm> Versions
    {
        get => _versions;
        set => SetField(ref _versions, value);
    }

    private VersionEntryVm? _selectedVersion;
    public VersionEntryVm? SelectedVersion
    {
        get => _selectedVersion;
        set
        {
            if (SetField(ref _selectedVersion, value)) OnPropertyChanged(nameof(CanLaunch));
        }
    }

    // ===== 账户 =====
    private ObservableCollection<AccountEntry> _accounts = new();
    public ObservableCollection<AccountEntry> Accounts
    {
        get => _accounts;
        set => SetField(ref _accounts, value);
    }

    private AccountEntry? _selectedAccount;
    public AccountEntry? SelectedAccount
    {
        get => _selectedAccount;
        set => SetField(ref _selectedAccount, value);
    }

    /// <summary>离线昵称输入（无账户或想用临时名时）。</summary>
    private string _username = "";
    public string Username
    {
        get => _username;
        set => SetField(ref _username, value);
    }

    // ===== 内存 / Java =====
    private int _memoryMb = 2048;
    public int MemoryMb
    {
        get => _memoryMb;
        set => SetField(ref _memoryMb, value);
    }

    private ObservableCollection<JavaInfo> _javaList = new();
    public ObservableCollection<JavaInfo> JavaList
    {
        get => _javaList;
        set => SetField(ref _javaList, value);
    }

    private JavaInfo? _selectedJava;
    public JavaInfo? SelectedJava
    {
        get => _selectedJava;
        set
        {
            if (SetField(ref _selectedJava, value)) OnPropertyChanged(nameof(CanLaunch));
        }
    }

    // ===== 状态 =====
    private string _status = LocaleManager.T("status.ready");
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetField(ref _isBusy, value)) OnPropertyChanged(nameof(CanLaunch));
        }
    }

    /// <summary>启动按钮可用性：未运行且已选版本与 Java。</summary>
    public bool CanLaunch => !IsBusy && SelectedVersion is not null && SelectedJava is not null;

    public ICommand RefreshVersionsCommand { get; }
    public ICommand DetectJavaCommand { get; }
    public ICommand LaunchCommand { get; }

    public GameHomeViewModel()
    {
        RefreshVersionsCommand = new RelayCommand(_ => RefreshVersions());
        DetectJavaCommand = new AsyncRelayCommand(_ => DetectJavaAsync());
        LaunchCommand = new AsyncRelayCommand(_ => LaunchAsync());
        RefreshVersions();
        LoadAccounts();
        _ = DetectJavaAsync();
    }

    /// <summary>枚举 versions/ 下含 &lt;id&gt;/&lt;id&gt;.json 的目录（对齐 WPF LauncherService.ListInstalledVersions）。</summary>
    public void RefreshVersions()
    {
        var list = new ObservableCollection<VersionEntryVm>();
        try
        {
            var versionsDir = PathEx.VersionsDir(_gameRoot);
            if (Directory.Exists(versionsDir))
            {
                foreach (var dir in Directory.GetDirectories(versionsDir))
                {
                    var id = Path.GetFileName(dir);
                    var json = PathEx.VersionJsonPath(_gameRoot, id);
                    if (!File.Exists(json)) continue;
                    string type = "";
                    try
                    {
                        var v = System.Text.Json.JsonSerializer.Deserialize<VersionJson>(File.ReadAllText(json));
                        type = v?.Type ?? "";
                    }
                    catch { /* 忽略解析错误 */ }
                    list.Add(new VersionEntryVm { Id = id, Type = type });
                }
            }
        }
        catch (Exception ex)
        {
            Status = $"读取版本失败：{ex.Message}";
        }

        Versions = list;
        Status = Versions.Count > 0
            ? $"共发现 {Versions.Count} 个已安装版本"
            : "暂无已安装版本，请前往「下载 → 原版」安装";
        if (SelectedVersion is null) SelectedVersion = Versions.FirstOrDefault();
    }

    private void LoadAccounts()
    {
        Accounts = new ObservableCollection<AccountEntry>(AccountStore.Load(_gameRoot));
        SelectedAccount = AccountStore.GetLastUsed(_gameRoot);
        if (SelectedAccount is not null) Username = SelectedAccount.Username;
    }

    public async Task DetectJavaAsync()
    {
        try
        {
            var list = await JavaDetector.DetectAsync();
            JavaList = new ObservableCollection<JavaInfo>(list.OrderByDescending(j => j.MajorVersion));
            if (SelectedJava is null) SelectedJava = JavaList.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Status = $"Java 扫描失败：{ex.Message}";
        }
    }

    private async Task LaunchAsync()
    {
        var id = SelectedVersion?.Id;
        if (string.IsNullOrWhiteSpace(id)) { Status = "请先选择一个版本"; return; }
        if (SelectedJava is null) { Status = "未检测到 Java，请先扫描或安装 Java"; return; }

        IsBusy = true;
        try
        {
            var opts = new LaunchOptions { MaxMemoryMb = MemoryMb };
            var name = SelectedAccount?.Username ?? Username;
            if (SelectedAccount is { } a && !string.IsNullOrEmpty(a.Uuid))
            {
                // 已存有 UUID 的账户（微软 / 第三方 / 已登录离线）：复用存储令牌
                opts.Username = a.Username;
                opts.Uuid = a.Uuid;
                opts.AccessToken = a.AccessToken;
                opts.UserType = a.AuthType == "microsoft" ? "msa" : a.AuthType;
            }
            else
            {
                // 纯离线：用官方算法生成离线 UUID
                opts.Username = name;
                opts.Uuid = OfflineAuthenticator.GenerateOfflineUuid(name);
                opts.AccessToken = "0";
                opts.UserType = "mojang";
            }

            Status = $"正在启动 {id} …";
            using var client = new HttpClient { Timeout = System.Threading.Timeout.InfiniteTimeSpan };
            var result = await GameLauncher.LaunchAsync(_gameRoot, id, SelectedJava, opts, null);
            Status = result.Crashed
                ? $"游戏已退出（崩溃，退出码 {result.ExitCode}）"
                : $"游戏已退出（退出码 {result.ExitCode}）";
        }
        catch (Exception ex)
        {
            Status = $"启动失败：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

/// <summary>已安装版本展示模型。</summary>
public class VersionEntryVm
{
    public string Id { get; init; } = "";
    public string Type { get; init; } = "";
    public string Display => string.IsNullOrEmpty(Type) ? Id : $"{Id} ({Type})";
}
