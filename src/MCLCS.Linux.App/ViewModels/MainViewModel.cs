using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MCLCS.Core.Launcher;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.UI;
using MCLCS.Linux.App;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>单个 Java 安装的展示模型（桥接 Core 的 JavaInfo）。</summary>
public class JavaEntry
{
    public string Exe { get; set; } = "";
    public int Major { get; set; }
    public string Raw { get; set; } = "";
    public override string ToString() => $"Java {Major}  {Exe}  ({Raw})";
}

/// <summary>
/// 主窗口视图模型：直接复用 Core.UI 的平台无关视图模型
/// （MainTabs / Sidebar / ObservableObject / TabThemeConfig），实现与 MCLCS.App 一致的四色标签 + 侧边栏布局，
/// 并在此之上接入本地化层与主题自定义（均来自 Core，非伪造）。
/// </summary>
public class MainViewModel : ObservableObject
{
    /// <summary>单例引用，供 KindToBrushConverter 在 XAML 模板加载时取主题色。</summary>
    public static MainViewModel? Instance { get; set; }

    /// <summary>四色主标签（来自 Core.UI.MainTabs.All），保留以兼容既有绑定。</summary>
    public IReadOnlyList<MainTabDefinition> Tabs => MainTabs.All;

    /// <summary>四色主标签的可绑定包装集合，驱动索引贴的展开 / 选中动画与重叠 Z 序。</summary>
    public ObservableCollection<TabItemViewModel> TabItems { get; }

    /// <summary>主题配色配置（Core.TabThemeConfig，可被用户自定义并持久化）。</summary>
    public TabThemeConfig Theme { get; } = new();

    private MainTabDefinition _selectedTab = MainTabs.Get(MainTabKind.Game);

    public MainViewModel()
    {
        var all = MainTabs.All;
        var items = new ObservableCollection<TabItemViewModel>();
        for (var i = 0; i < all.Count; i++)
            items.Add(new TabItemViewModel(all[i], i, all.Count));
        TabItems = items;
        _selectedTab = MainTabs.Get(MainTabKind.Game);
        _selectedSidebarId = Sidebar.For(_selectedTab.Kind).FirstOrDefault()?.Id ?? "";
        SyncTabSelection();
        // 语言切换时刷新所有数据驱动文本与索引贴显示名
        LocaleManager.LocaleChanged += OnLocaleChanged;
    }

    /// <summary>语言切换回调：刷新索引贴显示名与所有本地化文本属性。</summary>
    private void OnLocaleChanged(string _)
    {
        foreach (var t in TabItems)
            t.RaiseDisplayNameChanged();
        OnPropertyChanged(nameof(PanelTitle));
        OnPropertyChanged(nameof(PanelGroup));
        OnPropertyChanged(nameof(PanelDescription));
        OnPropertyChanged(nameof(InstalledCountText));
        OnPropertyChanged(nameof(RunningInstancesText));
        OnPropertyChanged(nameof(NetworkStatusText));
        OnPropertyChanged(nameof(JavaVersionText));
        OnPropertyChanged(nameof(Status));
        // 界面文案（标题栏 / 游戏主页 / 主题编辑器 / Java 区 / 下拉项）
        OnPropertyChanged(nameof(SearchWatermark));
        OnPropertyChanged(nameof(GameHomeTitle));
        OnPropertyChanged(nameof(GameHomeDesc));
        OnPropertyChanged(nameof(ThemeEditorTitle));
        OnPropertyChanged(nameof(ThemeEditorHint));
        OnPropertyChanged(nameof(LabelGame));
        OnPropertyChanged(nameof(LabelDownload));
        OnPropertyChanged(nameof(LabelToolbox));
        OnPropertyChanged(nameof(LabelSettings));
        OnPropertyChanged(nameof(JavaSectionTitle));
        OnPropertyChanged(nameof(JavaDetectButton));
        OnPropertyChanged(nameof(LangChinese));
        OnPropertyChanged(nameof(LangEnglish));
        OnPropertyChanged(nameof(ThemeDarkLabel));
        OnPropertyChanged(nameof(ThemeLightLabel));
    }

    /// <summary>把 TabItems 的 IsSelected 对齐到当前 SelectedTab，驱动索引贴展开 / Z 序动画。</summary>
    private void SyncTabSelection()
    {
        foreach (var t in TabItems)
            t.IsSelected = t.Kind == _selectedTab.Kind;
    }

    /// <summary>当前选中的主标签；切换时联动侧边栏集合与右面板标题。</summary>
    public MainTabDefinition SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (SetField(ref _selectedTab, value))
            {
                SelectedSidebarId = Sidebar.For(value.Kind).FirstOrDefault()?.Id ?? "";
                OnPropertyChanged(nameof(SidebarItems));
                OnPropertyChanged(nameof(PanelTitle));
                OnPropertyChanged(nameof(PanelGroup));
                OnPropertyChanged(nameof(PanelDescription));
                OnPropertyChanged(nameof(ShowThemeEditor));
                OnPropertyChanged(nameof(TitleBarColor));
                OnPropertyChanged(nameof(HasSidebar));
                OnPropertyChanged(nameof(SidebarItems));
                SyncTabSelection();
            }
        }
    }

    /// <summary>当前主标签下的副标签集合（来自 Core.UI.Sidebar.For）。</summary>
    public IReadOnlyList<SidebarItem> SidebarItems => Sidebar.For(_selectedTab.Kind);

    private string _selectedSidebarId = "";

    /// <summary>当前选中的副标签 Id；变化时联动右面板。</summary>
    public string SelectedSidebarId
    {
        get => _selectedSidebarId;
        set
        {
            if (SetField(ref _selectedSidebarId, value))
            {
                OnPropertyChanged(nameof(PanelTitle));
                OnPropertyChanged(nameof(PanelGroup));
                OnPropertyChanged(nameof(PanelDescription));
            }
        }
    }

    private SidebarItem? SelectedSidebarItem =>
        Sidebar.ById(_selectedTab.Kind, _selectedSidebarId);

    /// <summary>右面板标题（副标签本地化名，无副标签时取主标签名）。</summary>
    public string PanelTitle => Localization.Get(SelectedSidebarItem?.Title ?? _selectedTab.Title);

    /// <summary>右面板分组（副标签所在组，无则为空）。</summary>
    public string PanelGroup => SelectedSidebarItem?.Group is { } g ? Localization.Get(g) : "";

    /// <summary>右面板一句话说明（来自本地化描述表）。</summary>
    public string PanelDescription =>
        SelectedSidebarItem is { } s ? Localization.ToolDescription(s.Id) : "";

    /// <summary>是否在右面板展示主题编辑器（设置页 → 外观）。</summary>
    public bool ShowThemeEditor =>
        _selectedTab.Kind == MainTabKind.Settings && _selectedSidebarId == "appearance";

    /// <summary>主题色被改后，强制刷新主标签的 ItemsSource 以重算颜色，
    /// 并通知 SelectedTab 让标题栏底色 / 内容渐隐带 / 侧栏指示线一并按新色重算。</summary>
    public void RefreshTabs()
    {
        OnPropertyChanged(nameof(Tabs));
        OnPropertyChanged(nameof(TitleBarColor));
        OnPropertyChanged(nameof(SelectedTab));
    }

    /// <summary>标题栏背景色：跟随当前主标签（对齐 WPF 的 TitleBarBrush）。</summary>
    public string TitleBarColor => Theme.ColorOf(_selectedTab.Kind);

    private bool _sidebarExpanded;
    /// <summary>侧边栏是否展开（折叠时仅图标，悬停展开显示文字）。由界面层悬停事件驱动。</summary>
    public bool SidebarExpanded
    {
        get => _sidebarExpanded;
        set => SetField(ref _sidebarExpanded, value);
    }

    /// <summary>当前主标签是否带侧边栏（游戏页无侧边栏，对齐 WPF 规格 2.1）。</summary>
    public bool HasSidebar => Sidebar.For(_selectedTab.Kind).Count > 0;

    // ===== 状态栏（对齐 WPF 底部 StatusBar）=====
    private string? _javaVersionText;
    /// <summary>状态栏：Java 版本（检测后填入最高大版本；未检测时本地化占位）。</summary>
    public string JavaVersionText
    {
        get => _javaVersionText ?? LocaleManager.T("status.no_java");
        private set => SetField(ref _javaVersionText, value);
    }

    /// <summary>状态栏：已安装实例数（占位，待接入 Core 实例管理）。</summary>
    public string InstalledCountText => LocaleManager.Tf("status.installed", 0);

    /// <summary>状态栏：运行中的实例数（占位）。</summary>
    public string RunningInstancesText => LocaleManager.Tf("status.running", 0);

    /// <summary>状态栏：下载进度文本（占位）。</summary>
    public string DownloadText => "下载 0%";

    /// <summary>状态栏：下载进度（0-100，占位）。</summary>
    public double DownloadProgress => 0;

    /// <summary>状态栏：网络是否正常（占位，默认正常）。</summary>
    public bool IsNetworkOk => true;

    /// <summary>状态栏：网络状态文本（本地化）。</summary>
    public string NetworkStatusText =>
        IsNetworkOk ? LocaleManager.T("status.network_ok") : LocaleManager.T("status.network_offline");

    // ===== 本地化展示属性：标题栏 / 侧栏 / 状态栏之外的界面文案，随语言切换刷新 =====
    /// <summary>搜索框占位（lbl.search_mods）。</summary>
    public string SearchWatermark => LocaleManager.T("lbl.search_mods");
    /// <summary>游戏主页标题（tab.game）。</summary>
    public string GameHomeTitle => LocaleManager.T("tab.game");
    /// <summary>游戏主页描述（home.game.desc）。</summary>
    public string GameHomeDesc => LocaleManager.T("home.game.desc");
    /// <summary>四色主题编辑器标题（theme.editor.title）。</summary>
    public string ThemeEditorTitle => LocaleManager.T("theme.editor.title");
    /// <summary>四色主题编辑器提示（theme.editor.hint）。</summary>
    public string ThemeEditorHint => LocaleManager.T("theme.editor.hint");
    /// <summary>主题编辑器四色标签：游戏。</summary>
    public string LabelGame => LocaleManager.T("tab.game");
    /// <summary>主题编辑器四色标签：下载。</summary>
    public string LabelDownload => LocaleManager.T("tab.download");
    /// <summary>主题编辑器四色标签：工具箱。</summary>
    public string LabelToolbox => LocaleManager.T("tab.toolbox");
    /// <summary>主题编辑器四色标签：设置。</summary>
    public string LabelSettings => LocaleManager.T("tab.settings");
    /// <summary>Java 环境检测区标题（java.title）。</summary>
    public string JavaSectionTitle => LocaleManager.T("java.title");
    /// <summary>Java 检测按钮文案（java.detect）。</summary>
    public string JavaDetectButton => LocaleManager.T("java.detect");
    /// <summary>语言下拉：简体中文（lbl.chinese）。</summary>
    public string LangChinese => LocaleManager.T("lbl.chinese");
    /// <summary>语言下拉：English（lbl.english）。</summary>
    public string LangEnglish => LocaleManager.T("lbl.english");
    /// <summary>主题下拉：暗色（lbl.dark）。</summary>
    public string ThemeDarkLabel => LocaleManager.T("lbl.dark");
    /// <summary>主题下拉：亮色（lbl.light）。</summary>
    public string ThemeLightLabel => LocaleManager.T("lbl.light");

    /// <summary>已检测到的 Java 列表。</summary>
    public ObservableCollection<JavaEntry> JavaList { get; } = new();

    private string _status = LocaleManager.T("status.ready");
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    /// <summary>调用 Core 的 JavaDetector 在 Linux 上扫描 Java。</summary>
    public async Task DetectJavaAsync()
    {
        Status = LocaleManager.T("java.scanning");
        var list = await JavaDetector.DetectAsync();
        JavaList.Clear();
        foreach (var j in list.OrderByDescending(j => j.MajorVersion))
            JavaList.Add(new JavaEntry { Exe = j.JavaExe, Major = j.MajorVersion, Raw = j.RawVersion });
        Status = LocaleManager.Tf("java.detected", JavaList.Count);
        JavaVersionText = JavaList.Count > 0 ? $"Java {JavaList[0].Major}" : LocaleManager.T("status.no_java");
    }
}

/// <summary>
/// 四色索引贴的可绑定包装：驱动选中展开 / 折叠动画与重叠 Z 序。
/// 几何对齐 WPF Core.UI.MainTabDefinition 的 MainTabs 规则：
/// 选中页或游戏页（AlwaysExpanded）展开显示文字，其余折叠为色条；左压右叠放。
/// </summary>
public class TabItemViewModel : ObservableObject
{
    public MainTabDefinition Def { get; }
    public MainTabKind Kind => Def.Kind;
    /// <summary>本地化后的显示名（Def.Title 是 l10n key）。</summary>
    public string DisplayName => Localization.Get(Def.Title);
    /// <summary>游戏页恒展开（WPF 规格：AlwaysExpanded=true）。</summary>
    public bool AlwaysExpanded => Kind == MainTabKind.Game;
    /// <summary>在四色序列中的次序（0=游戏）。决定重叠方向与默认 Z 序。</summary>
    public int Order { get; }
    public int TotalTabs { get; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetField(ref _isSelected, value))
            {
                OnPropertyChanged(nameof(IsExpanded));
                OnPropertyChanged(nameof(ZIndex));
            }
        }
    }

    /// <summary>是否展开显示文字：游戏页恒展开，其余仅选中时展开。</summary>
    public bool IsExpanded => AlwaysExpanded || _isSelected;

    /// <summary>语言切换时由 VM 调用，强制刷新显示名（DisplayName 依赖当前语言）。</summary>
    public void RaiseDisplayNameChanged() => OnPropertyChanged(nameof(DisplayName));

    /// <summary>Z 序：选中页置顶（100），其余按「左压右」由 Order 决定（Order 越小越高）。</summary>
    public int ZIndex => _isSelected ? 100 : (TotalTabs - Order);

    public TabItemViewModel(MainTabDefinition def, int order, int total)
    {
        Def = def;
        Order = order;
        TotalTabs = total;
    }
}
