using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using MCLCS.Core.Theme;
using MCLCS.Linux.App.Themes;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

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
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
