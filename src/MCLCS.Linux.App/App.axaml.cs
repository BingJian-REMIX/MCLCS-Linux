using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
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

        // 主题接入：优先读取持久化偏好（mclcs_theme.json）；
        // 无偏好文件时默认暗色，保持 Linux 版既定外观。
        ThemeManager.LoadPreference(AppConfig.DataRoot);
        if (!File.Exists(Path.Combine(AppConfig.DataRoot, "mclcs_theme.json")))
            ThemeManager.Current = ThemeType.Dark;
        ApplyTheme(ThemeManager.Current);
        ThemeManager.OnThemeChanged += ApplyTheme;
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
    }
}
