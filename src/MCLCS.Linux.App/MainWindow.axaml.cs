using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MCLCS.Core.UI;
using MCLCS.Linux.App.ViewModels;
using System.Diagnostics;

namespace MCLCS.Linux.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private bool _isMax = true;

    public MainWindow()
    {
        _vm = new MainViewModel();
        MainViewModel.Instance = _vm;
        DataContext = _vm;
        InitializeComponent();
        SyncSidebarSelection();
        UpdateMaxIcon();
        // 上屏且屏幕信息就绪后再铺满（构造函数里 Screens.Primary 尚未可用）
        Opened += (_, _) => FitToScreen();
    }

    /// <summary>按主屏工作区尺寸铺满窗口（避免固定尺寸在大屏上留黑边）。
    /// 优先用 Screens.Primary（真实桌面 WM 下可靠）；无 WM 的 X11（如 Xvfb）下
    /// Primary 为 null，回退用 xdotool getdisplaygeometry 取显示尺寸。</summary>
    private void FitToScreen()
    {
        var area = GetScreenArea();
        if (area is null) return;
        Position = area.Value.TopLeft;
        Width = area.Value.Width;
        Height = area.Value.Height;
    }

    private PixelRect? GetScreenArea()
    {
        var screen = Screens.Primary;
        if (screen is not null) return screen.WorkingArea;
        // 回退：headless / 无 WM 环境下 Screens.Primary 为 null
        try
        {
            var psi = new ProcessStartInfo("xdotool", "getdisplaygeometry")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc is null) return null;
            var outp = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            var parts = outp.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 &&
                int.TryParse(parts[0], out var w) && int.TryParse(parts[1], out var h) &&
                w > 0 && h > 0)
                return new PixelRect(0, 0, w, h);
        }
        catch { }
        return null;
    }

    /// <summary>切换主标签：联动侧边栏集合、标题栏色、右面板，并同步 ListBox 选中项。</summary>
    private void Tab_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: MainTabKind kind } && DataContext is MainViewModel vm)
        {
            vm.SelectedTab = MainTabs.Get(kind);
            SyncSidebarSelection();
        }
    }

    private void Sidebar_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: SidebarItem item } && DataContext is MainViewModel vm)
            vm.SelectedSidebarId = item.Id;
    }

    /// <summary>把 ListBox 选中项对齐到 VM 当前 SelectedSidebarId（切主标签后调用）。</summary>
    private void SyncSidebarSelection()
    {
        if (SidebarList is null) return;
        SidebarList.SelectedItem = _vm.SidebarItems.FirstOrDefault(i => i.Id == _vm.SelectedSidebarId);
    }

    // ===== 侧边栏折叠 / 展开（对齐 WPF 悬停逻辑，200ms 动画由 XAML DoubleTransition 实现）=====
    private void Sidebar_Enter(object? sender, PointerEventArgs e)
    {
        SidebarRoot.Width = 152;
        _vm.SidebarExpanded = true;
    }

    private void Sidebar_Leave(object? sender, PointerEventArgs e)
    {
        SidebarRoot.Width = 56;
        _vm.SidebarExpanded = false;
    }

    // ===== 窗口控制 =====
    /// <summary>标题栏按下拖拽。但若按下落在交互控件（标签按钮 / 窗口控制 / 搜索框）上，
    /// 则不触发拖拽，交给控件自身的 Click 处理——否则 BeginMoveDrag 会吞掉标签点击，
    /// 导致主标签永远切不动（同 WPF bug #8：索引贴 MouseLeftButtonDown 需截断冒泡）。</summary>
    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var el = e.Source as Control;
        while (el is not null)
        {
            if (el is Button or TextBox) return; // 落在交互控件上：不拖拽，让其 Click 正常派发
            el = el.Parent as Control;
        }
        BeginMoveDrag(e);
    }

    private void BtnMin_Click(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void BtnMax_Click(object? sender, RoutedEventArgs e)
    {
        if (_isMax)
        {
            // 还原为默认窗口尺寸并居中
            var screen = Screens.Primary;
            Width = 1100;
            Height = 720;
            if (screen is not null)
            {
                var x = screen.WorkingArea.X + (screen.WorkingArea.Width - 1100) / 2;
                var y = screen.WorkingArea.Y + (screen.WorkingArea.Height - 720) / 2;
                Position = new PixelPoint(x, y);
            }
            _isMax = false;
        }
        else
        {
            FitToScreen();
            _isMax = true;
        }
        UpdateMaxIcon();
    }

    /// <summary>根据窗口状态切换最大化/还原图标。</summary>
    private void UpdateMaxIcon()
    {
        if (MaxIconNormal is null || MaxIconRestored is null) return;
        var max = WindowState == WindowState.Maximized;
        MaxIconNormal.IsVisible = !max;
        MaxIconRestored.IsVisible = max;
    }

    private void BtnClose_Click(object? sender, RoutedEventArgs e) => Close();

    // ===== Java 检测 / 主题编辑（保留既有逻辑）=====
    private async void DetectJava_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            await vm.DetectJavaAsync();
    }

    private void ThemeColor_Changed(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox tb || tb.Tag is not string tag || DataContext is not MainViewModel vm)
            return;
        switch (tag)
        {
            case "game": vm.Theme.Game = tb.Text; break;
            case "download": vm.Theme.Download = tb.Text; break;
            case "toolbox": vm.Theme.Toolbox = tb.Text; break;
            case "settings": vm.Theme.Settings = tb.Text; break;
        }
        vm.RefreshTabs();
    }
}
