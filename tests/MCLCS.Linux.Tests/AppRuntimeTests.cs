using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using MCLCS.Core.Launcher;
using MCLCS.Core.UI;
using MCLCS.Linux.App;
using MCLCS.Linux.App.Converters;
using MCLCS.Linux.App.ViewModels;
using Xunit;

namespace MCLCS.Linux.Tests;

/// <summary>
/// 无头运行时验证：不依赖显示，直接验证「视图模型 → Core 数据 → 颜色转换器」整条链路，
/// 用于捕捉编译通过但运行时才暴露的隐式问题。
/// </summary>
public class AppRuntimeTests
{
    [Fact]
    public void MainViewModel_Tabs_来自_Core_四色标签()
    {
        var vm = new MainViewModel();
        Assert.Equal(4, vm.Tabs.Count);
        Assert.Equal("tab.game", vm.Tabs[0].Title);
        Assert.Equal("#4CAF50", vm.Tabs[0].DefaultColor);
        Assert.Equal("#2196F3", vm.Tabs[1].DefaultColor);
        Assert.Equal("#FF9800", vm.Tabs[2].DefaultColor);
        Assert.Equal("#607D8B", vm.Tabs[3].DefaultColor);
    }

    [Fact]
    public void MainViewModel_SelectedTab_联动_SidebarItems()
    {
        var vm = new MainViewModel();
        // 默认选中 Download（副标签集合与 Core 一致）
        Assert.Equal(MainTabKind.Download, vm.SelectedTab.Kind);
        Assert.Equal(Sidebar.Download.Count, vm.SidebarItems.Count);
        // 切换到 Toolbox（副标签集合与 Core 一致）
        vm.SelectedTab = MainTabs.Get(MainTabKind.Toolbox);
        Assert.Equal(Sidebar.Toolbox.Count, vm.SidebarItems.Count);
        // 游戏页无侧边栏
        vm.SelectedTab = MainTabs.Get(MainTabKind.Game);
        Assert.Empty(vm.SidebarItems);
    }

    [Fact]
    public async Task MainViewModel_DetectJavaAsync_在Linux真实可用()
    {
        var vm = new MainViewModel();
        await vm.DetectJavaAsync();
        // 本机已探到 Java 20（sdkman 路径），证明 Core.JavaDetector 在 Linux 可用
        Assert.True(vm.JavaList.Count >= 1, "应至少检测到 1 个 Java 安装");
        Assert.Contains(vm.JavaList, j => j.Major >= 8);
        Assert.DoesNotContain("就绪", vm.Status); // Status 已被更新
    }

    [Fact]
    public void HexToBrushConverter_解析_Core_四色()
    {
        var conv = new HexToBrushConverter();
        foreach (var hex in new[] { "#4CAF50", "#2196F3", "#FF9800", "#607D8B" })
        {
            var brush = conv.Convert(hex, typeof(SolidColorBrush), null, null);
            Assert.IsType<SolidColorBrush>(brush);
            Assert.NotNull(((SolidColorBrush)brush!).Color);
        }
        // 非法输入优雅降级为 Gray
        var fallback = conv.Convert("not-a-color", typeof(SolidColorBrush), null, null);
        Assert.IsType<SolidColorBrush>(fallback);
    }

    [Fact]
    public void 隐式问题_标题为本地化Key而非显示名()
    {
        // Core 的 Title 存的是 l10n key（如 tab.game），Avalonia 界面尚未接入本地化层，
        // 因此按钮/侧边栏会直接显示原始 key 而非中文。记录为已知缺口。
        var vm = new MainViewModel();
        Assert.All(vm.Tabs, t => Assert.Contains(".", t.Title));
        Assert.Contains(".", vm.SidebarItems.First().Title);
    }
}
