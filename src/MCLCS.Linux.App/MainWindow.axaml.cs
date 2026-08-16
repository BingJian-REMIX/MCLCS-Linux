using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MCLCS.Core.Localization;
using MCLCS.Core.Theme;
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
        // 语言 / 主题切换控件初始态（与当前 LocaleManager / ThemeManager 对齐）
        if (LangCombo is not null)
            LangCombo.SelectedIndex = LocaleManager.CurrentLocale == "en_US" ? 1 : 0;
        if (ThemeCombo is not null)
            ThemeCombo.SelectedIndex = ThemeManager.Current == ThemeType.Light ? 1 : 0;
        // 语言切换时重绑侧栏（走 KeyToTextConverter 的项需重绑才能刷新）
        LocaleManager.LocaleChanged += OnLocaleChanged;
        // 上屏且屏幕信息就绪后再铺满（构造函数里 Screens.Primary 尚未可用）
        Opened += (_, _) => FitToScreen();
        Opened += (_, _) => AutoDemo();
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

    /// <summary>切页展开动画：内容宿主淡入 + 上移（对齐设计稿 @keyframes pageIn：opacity 0→1 + translateY(18px)→0，200ms ease）。
    /// 先把内容落到隐藏态，再于下一渲染帧恢复，触发 0→1 / 18px→0 过渡。</summary>
    private void PlayContentEnter()
    {
        if (ContentHost is null) return;
        ContentHost.Opacity = 0;
        ContentHost.RenderTransform = TransformOperations.Parse("translateY(18px)");
        Dispatcher.UIThread.Post(() =>
        {
            if (ContentHost is null) return;
            ContentHost.Opacity = 1;
            ContentHost.RenderTransform = TransformOperations.Parse("translateY(0px)");
        }, DispatcherPriority.Render);
    }

    // ===== 弹窗 / Toast 演示（开发用，验证设计稿三种对话框变体）=====
    private async void Demo_Center(object? sender, RoutedEventArgs e)
    {
        var r = await DialogService.Instance.ShowAsync(new DialogOptions
        {
            Title = "确认操作",
            Content = "这是居中的通用询问对话框（askuserquestion 变体 A）。",
            Buttons = new[]
            {
                new DialogButton("取消", DialogResults.Cancel, isCancel: true),
                new DialogButton("确定", DialogResults.Ok, DialogButtonKind.Primary, isDefault: true),
            }
        });
        if (r is not null)
            ToastService.Instance.Show(new ToastOptions { Title = "对话框结果", Message = $"选择：{r}" });
    }

    // 标题栏下载按钮：打开下载队列（锚定到该按钮中心弹出）
    private void DownloadBtn_Click(object? sender, RoutedEventArgs e) => QueueShow(sender as Control);

    // 演示按钮「右上队列」：同样以下载按钮为锚点弹出
    private void Demo_Queue(object? sender, RoutedEventArgs e) => QueueShow(sender as Control);

    // 下载队列：以标题栏下载按钮为锚点向下弹出（水平中线对齐按钮中心 + 弹出动画）
    private async void QueueShow(Control? anchor = null)
    {
        await DialogService.Instance.ShowAsync(new DialogOptions
        {
            Title = "下载队列",
            Width = 420,
            Anchor = anchor ?? DownloadBtn,
            Content = BuildQueueContent(),
            Buttons = new[] { new DialogButton("开始下载", "start", DialogButtonKind.Primary, isDefault: true) }
        });
    }

    private async void Demo_Music(object? sender, RoutedEventArgs e)
    {
        await DialogService.Instance.ShowAsync(new DialogOptions
        {
            Title = "播放列表",
            Alignment = DialogAlignment.BottomRight,
            Width = 320,
            Content = BuildMusicContent(),
            Buttons = new[] { new DialogButton("关闭", DialogResults.Cancel, isCancel: true) }
        });
    }

    private void Demo_Toast(object? sender, RoutedEventArgs e) =>
        ToastService.Instance.Show(new ToastOptions
        {
            Title = "已加入下载队列",
            Message = "天空之城 · 地图",
            ActionText = "查看",
            Action = () => ToastService.Instance.Show(new ToastOptions { Title = "提示", Message = "（演示）跳转到下载页" })
        });

    // 临时：无头渲染验证用——按环境变量自动打开对应对话框 / Toast 变体（验证后移除）
    private void AutoDemo()
    {
        switch (System.Environment.GetEnvironmentVariable("MCLCS_DEMO"))
        {
            case "center": Demo_Center(null, null!); break;
            case "queue": Demo_Queue(null, null!); break;
            case "music": Demo_Music(null, null!); break;
            case "toast": Demo_Toast(null, null!); break;
        }
    }

    private Control BuildQueueContent()
    {
        var sp = new StackPanel { Spacing = 8 };
        sp.Children.Add(QueueItem("Sodium 0.5.8 (Fabric 1.21.4)", "下载中 42%", 42));
        sp.Children.Add(QueueItem("BSL Shaders v8.3", "等待中", 0));
        return sp;
    }

    private Control QueueItem(string title, string status, double pct)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(0, 4)
        };
        grid.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 12,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        });
        grid.Children.Add(new TextBlock
        {
            Text = status,
            FontSize = 12,
            Foreground = (IBrush?)Application.Current.FindResource("SecondaryForeground"),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        });
        var bar = new ProgressBar
        {
            Value = pct,
            Minimum = 0,
            Maximum = 100,
            Height = 8,
            Margin = new Thickness(0, 4, 0, 0),
            Foreground = (IBrush?)Application.Current.FindResource("AccentBrush"),
            Background = (IBrush?)Application.Current.FindResource("ProgressBackground")
        };
        var wrap = new StackPanel { Spacing = 2 };
        wrap.Children.Add(grid);
        wrap.Children.Add(bar);
        return wrap;
    }

    private Control BuildMusicContent()
    {
        var sp = new StackPanel { Spacing = 4 };
        foreach (var name in new[] { "C418 - Sweden", "C418 - Wet Hands", "Lena Raine - Pigstep", "C418 - Subwoofer Lullaby" })
            sp.Children.Add(new TextBlock
            {
                Text = name,
                FontSize = 13,
                Padding = new Thickness(0, 4),
                Foreground = (IBrush?)Application.Current.FindResource("PrimaryForeground")
            });
        return sp;
    }

    /// <summary>切换主标签：联动侧边栏集合、标题栏色、右面板，并同步 ListBox 选中项。</summary>
    private void Tab_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: MainTabKind kind } && DataContext is MainViewModel vm)
        {
            vm.SelectedTab = MainTabs.Get(kind);
            SyncSidebarSelection();
            PlayContentEnter();
        }
    }

    private void Sidebar_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: SidebarItem item } && DataContext is MainViewModel vm)
            vm.SelectedSidebarId = item.Id;
        PlayContentEnter();
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

    // ===== 语言 / 主题切换 =====
    private void LangCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (LangCombo is null) return;
        LocaleManager.CurrentLocale = LangCombo.SelectedIndex == 1 ? "en_US" : "zh_CN";
    }

    private void ThemeCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ThemeCombo is null) return;
        var t = ThemeCombo.SelectedIndex == 1 ? ThemeType.Light : ThemeType.Dark;
        if (ThemeManager.Current == t) return; // 初始设定或重复选择不重复保存
        ThemeManager.Current = t;
        ThemeManager.SavePreference(AppConfig.DataRoot);
    }

    /// <summary>语言切换时重绑侧栏列表（走 KeyToTextConverter 的项需重建项才能刷新文本）；
    /// 其余由 {loc:Loc} 绑定自动刷新。</summary>
    private void OnLocaleChanged(string _)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (SidebarList is not null)
            {
                SidebarList.ItemsSource = null;
                SidebarList.ItemsSource = _vm.SidebarItems;
            }
        });
    }

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
