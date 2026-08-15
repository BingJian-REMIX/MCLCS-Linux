using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MCLCS.Core.Launcher;
using MCLCS.Core.Mvvm;
using MCLCS.Core.UI;

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
/// （MainTabs / Sidebar / ObservableObject），实现与 MCLCS.App 一致的四色标签 + 侧边栏布局。
/// </summary>
public class MainViewModel : ObservableObject
{
    /// <summary>四色主标签（来自 Core.UI.MainTabs.All）。</summary>
    public IReadOnlyList<MainTabDefinition> Tabs => MainTabs.All;

    private MainTabDefinition _selectedTab = MainTabs.Get(MainTabKind.Download);

    /// <summary>当前选中的主标签；切换时联动侧边栏集合。</summary>
    public MainTabDefinition SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (SetField(ref _selectedTab, value))
                OnPropertyChanged(nameof(SidebarItems));
        }
    }

    /// <summary>当前主标签下的副标签集合（来自 Core.UI.Sidebar.For）。</summary>
    public IReadOnlyList<SidebarItem> SidebarItems => Sidebar.For(_selectedTab.Kind);

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
