using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MCLCS.Core.Launcher;
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

    /// <summary>四色主标签（来自 Core.UI.MainTabs.All）。</summary>
    public IReadOnlyList<MainTabDefinition> Tabs => MainTabs.All;

    /// <summary>主题配色配置（Core.TabThemeConfig，可被用户自定义并持久化）。</summary>
    public TabThemeConfig Theme { get; } = new();

    private MainTabDefinition _selectedTab = MainTabs.Get(MainTabKind.Download);

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
            }
        }
    }

    /// <summary>当前主标签下的副标签集合（来自 Core.UI.Sidebar.For）。</summary>
    public IReadOnlyList<SidebarItem> SidebarItems => Sidebar.For(_selectedTab.Kind);

    private string _selectedSidebarId = Sidebar.Download[0].Id;

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

    /// <summary>主题色被改后，强制刷新主标签的 ItemsSource 以重算颜色。</summary>
    public void RefreshTabs() => OnPropertyChanged(nameof(Tabs));

    /// <summary>已检测到的 Java 列表。</summary>
    public ObservableCollection<JavaEntry> JavaList { get; } = new();

    private string _status = "就绪";
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    /// <summary>调用 Core 的 JavaDetector 在 Linux 上扫描 Java。</summary>
    public async Task DetectJavaAsync()
    {
        Status = "正在扫描 Java（JAVA_HOME / /usr/lib/jvm / /opt/java / PATH）...";
        var list = await JavaDetector.DetectAsync();
        JavaList.Clear();
        foreach (var j in list.OrderByDescending(j => j.MajorVersion))
            JavaList.Add(new JavaEntry { Exe = j.JavaExe, Major = j.MajorVersion, Raw = j.RawVersion });
        Status = $"检测到 {JavaList.Count} 个 Java 安装";
    }
}
