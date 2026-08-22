using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using MCLCS.Core.Download;
using MCLCS.Core.Profiles;
using MCLCS.Core.Theme;
using MCLCS.Core.Utils;
using MCLCS.Linux.App.Themes;

namespace MCLCS.Linux.App;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // 必须最先执行（对齐 WPF App.xaml.cs）：读取用户自定义的游戏目录覆盖，
        // 之后所有 GameConstants.DefaultGameRoot 才是正确值，各页面构造时缓存的目录才不会错。
        GameConstants.LoadGameRootOverride();

        // 同步下载源偏好到 MirrorPolicy（设置 → 下载），使各镜像 URL 按用户优先级重排。
        MirrorPolicy.Preference = ProfileStore.Load(GameConstants.DefaultGameRoot).DownloadSource;

        // 主题接入：优先读取持久化偏好（mclcs_theme.json）；
        // 无偏好文件时默认暗色，保持 Linux 版既定外观。
        ThemeManager.LoadPreference(AppConfig.DataRoot);
        if (!File.Exists(Path.Combine(AppConfig.DataRoot, "mclcs_theme.json")))
            ThemeManager.Current = ThemeType.Dark;
        ApplyTheme(ThemeManager.Current);
        ThemeManager.OnThemeChanged += ApplyTheme;

        // 高清图标偏好：profile.HighDpiIcons → IconManager（图标加载切 2x 目录，对齐 WPF）
        Converters.IconManager.HighDpi =
            ProfileStore.Load(GameConstants.DefaultGameRoot).HighDpiIcons;

        // 外观：把 profile 中持久化的主题色 / 字体缩放 / 背景图真正应用到运行时（对齐 WPF，修复空壳）
        ApplyAppearanceFromProfile();
    }

    /// <summary>把选定主题的调色板写入 Application.Resources，并切换 Fluent 主题变体。</summary>
    private void ApplyTheme(ThemeType type)
    {
        var dict = type == ThemeType.Light ? ThemePalettes.Light() : ThemePalettes.Dark();
        var app = Application.Current!;
        foreach (var key in dict.Keys)
            if (key is string s) app.Resources[s] = dict[key];
        app.RequestedThemeVariant = type == ThemeType.Light ? ThemeVariant.Light : ThemeVariant.Dark;
    }

    /// <summary>启动时把 profile 持久化的外观设置真正应用到运行时（对齐 WPF App.ApplyAccentColor/FontScale/BackgroundImage）。</summary>
    public static void ApplyAppearanceFromProfile()
    {
        var profile = ProfileStore.Load(GameConstants.DefaultGameRoot);
        ApplyAccentColor(profile.ThemeColor);
        ApplyFontScale(profile.FontScale);
        ApplyBackgroundImage(profile.BackgroundImagePath);
    }

    /// <summary>主题色：覆盖全局 Accent 系列资源（对齐 WPF bug #11：侧栏/按键/开关主题色失效）。</summary>
    public static void ApplyAccentColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return;
        var s = hex!.Trim();
        if (!s.StartsWith("#", StringComparison.Ordinal)) s = "#" + s;
        if (!Color.TryParse(s, out var color)) return;
        var res = Application.Current!.Resources;
        res["AccentBrush"] = new SolidColorBrush(color);
        res["InputFocusBorder"] = new SolidColorBrush(color);
        res["CardBorderHover"] = new SolidColorBrush(color);
    }

    /// <summary>字体缩放：设置主窗口字号。Avalonia 下字号沿视觉树继承，未显式设置 FontSize 的控件随之缩放。</summary>
    public static void ApplyFontScale(double scale)
    {
        if (scale <= 0) scale = 1.0;
        var fontSize = 13.0 * scale;
        if (CurrentMainWindow is { } mw)
            mw.FontSize = fontSize;
        Application.Current!.Resources["BaseFontSize"] = fontSize;
    }

    /// <summary>背景图片：应用到主窗口（对齐 WPF bug #20：此前仅持久化路径、从未真正渲染）。</summary>
    public static void ApplyBackgroundImage(string? path)
    {
        try
        {
            CurrentMainWindow?.SetBackgroundImage(string.IsNullOrWhiteSpace(path) ? null : path);
        }
        catch
        {
            // 窗口尚未就绪等异常静默忽略
        }
    }

    /// <summary>当前主窗口（启动期尚未创建时为 null）。</summary>
    private static MainWindow? CurrentMainWindow =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime dt
            ? dt.MainWindow as MainWindow
            : null;

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // MainWindow 构造函数已自建并绑定 MainViewModel（含 Instance 单例），
            // 此处不再重复 new，否则对象初始化器会用一个新实例覆盖 DataContext，
            // 导致 Tab_Click/ShowPage 操作的是不同 VM 实例（内容页永远无法切换）。
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();

#if SCREENSHOT
        ScreenshotCapture.Run();
#endif
    }
}
