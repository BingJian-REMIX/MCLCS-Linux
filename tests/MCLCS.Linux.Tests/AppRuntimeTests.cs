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
/// 无头运行时验证：不依赖显示，直接验证「视图模型 → Core 数据 → 本地化 → 颜色转换器」整条链路，
/// 用于捕捉编译通过但运行时才暴露的隐式问题。
/// </summary>
public class AppRuntimeTests
{
    [Fact]
    public void MainViewModel_Tabs_来自_Core_四色标签()
    {
        var vm = new MainViewModel();
        Assert.Equal(4, vm.Tabs.Count);
        Assert.Equal("tab.game", vm.Tabs[0].Title); // VM 仍暴露原始 key（本地化在显示层做）
        Assert.Equal("#4CAF50", vm.Tabs[0].DefaultColor);
        Assert.Equal("#2196F3", vm.Tabs[1].DefaultColor);
        Assert.Equal("#FF9800", vm.Tabs[2].DefaultColor);
        Assert.Equal("#607D8B", vm.Tabs[3].DefaultColor);
    }

    [Fact]
    public void KeyToTextConverter_把_l10nKey_翻译为中文()
    {
        var conv = new KeyToTextConverter();
        Assert.Equal("游戏", conv.Convert("tab.game", typeof(string), null, null));
        Assert.Equal("工具箱", conv.Convert("tab.toolbox", typeof(string), null, null));
        Assert.Equal("崩溃分析", conv.Convert("tool.crash", typeof(string), null, null));
        // 未知 key 原样返回，不丢信息
        Assert.Equal("unknown.key", conv.Convert("unknown.key", typeof(string), null, null));
        Assert.Equal("", conv.Convert(null, typeof(string), null, null));
    }

    [Fact]
    public void Localization_ToolDescription_覆盖_工具箱20项()
    {
        foreach (var item in Sidebar.Toolbox)
            Assert.NotEmpty(Localization.ToolDescription(item.Id));
        // 未登记项优雅降级
        Assert.Equal("（待接入 Core 能力）", Localization.ToolDescription("nope"));
    }

    [Fact]
    public void MainViewModel_SelectedTab_联动_SidebarItems()
    {
        var vm = new MainViewModel();
        // 默认主页为游戏页（用户要求），游戏页无侧边栏
        Assert.Equal(MainTabKind.Game, vm.SelectedTab.Kind);
        Assert.Empty(vm.SidebarItems);
        // 切换到 Download（副标签集合与 Core 一致）
        vm.SelectedTab = MainTabs.Get(MainTabKind.Download);
        Assert.Equal(Sidebar.Download.Count, vm.SidebarItems.Count);
        // 切换到 Toolbox（副标签集合与 Core 一致）
        vm.SelectedTab = MainTabs.Get(MainTabKind.Toolbox);
        Assert.Equal(Sidebar.Toolbox.Count, vm.SidebarItems.Count);
    }

    [Fact]
    public void MainViewModel_选中副标签_联动_右面板()
    {
        var vm = new MainViewModel();
        vm.SelectedTab = MainTabs.Get(MainTabKind.Toolbox);
        vm.SelectedSidebarId = "crash";
        Assert.Equal("崩溃分析", vm.PanelTitle);
        Assert.Equal("诊断与排障", vm.PanelGroup);
        Assert.NotEmpty(vm.PanelDescription);
    }

    [Fact]
    public void MainViewModel_外观页_展示主题编辑器()
    {
        var vm = new MainViewModel();
        vm.SelectedTab = MainTabs.Get(MainTabKind.Settings);
        vm.SelectedSidebarId = "appearance";
        Assert.True(vm.ShowThemeEditor);
        // Core.TabThemeConfig 真实可用：默认四色与 Core 常量一致
        Assert.Equal(MainTabs.DefaultGameColor, vm.Theme.ColorOf(MainTabKind.Game));
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
    public void KindToBrushConverter_按_Theme_取色_降级Gray()
    {
        var conv = new KindToBrushConverter();
        MainViewModel.Instance = new MainViewModel();
        var brush = conv.Convert(MainTabKind.Game, typeof(SolidColorBrush), null, null);
        Assert.IsType<SolidColorBrush>(brush);
        // 未知类型降级
        var bad = conv.Convert(123, typeof(SolidColorBrush), null, null);
        Assert.IsType<SolidColorBrush>(bad);
    }
}
