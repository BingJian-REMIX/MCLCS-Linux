using Avalonia.Controls;
using Avalonia.Media;

namespace MCLCS.Linux.App.Themes;

/// <summary>
/// 主题调色板：暗色 / 亮色两套，键名与 <c>App.axaml</c> 的 DynamicResource 引用一致。
/// 由 <c>App</c> 在启动时 / 切换时把对应字典的值写入 <c>Application.Resources</c>，
/// 所有 <c>{DynamicResource Key}</c> 引用即时刷新。
/// </summary>
public static class ThemePalettes
{
    /// <summary>暗色调色板（对齐 MCLCS-WPF 的 DarkTheme.xaml / Palette.xaml）。</summary>
    public static ResourceDictionary Dark() => new()
    {
        { "WindowBackground", new SolidColorBrush(Color.Parse("#0F1115")) },
        { "ControlBackground", new SolidColorBrush(Color.Parse("#1A1D24")) },
        { "ControlBorder", new SolidColorBrush(Color.Parse("#2A2F3A")) },
        { "ControlHoverBackground", new SolidColorBrush(Color.Parse("#252830")) },

        { "PrimaryForeground", new SolidColorBrush(Color.Parse("#FFFFFF")) },
        { "SecondaryForeground", new SolidColorBrush(Color.Parse("#C8CDD6")) },
        { "StatusForeground", new SolidColorBrush(Color.Parse("#C8CDD6")) },

        { "AccentBrush", new SolidColorBrush(Color.Parse("#3B82F6")) },
        { "DangerBrush", new SolidColorBrush(Color.Parse("#E74C3C")) },

        { "InputBackground", new SolidColorBrush(Color.Parse("#14161C")) },
        { "InputBorder", new SolidColorBrush(Color.Parse("#2A2F3A")) },
        { "InputFocusBorder", new SolidColorBrush(Color.Parse("#3B82F6")) },

        { "ProgressBackground", new SolidColorBrush(Color.Parse("#2A2F3A")) },
    };

    /// <summary>亮色调色板（同键名，浅色可读配色）。</summary>
    public static ResourceDictionary Light() => new()
    {
        { "WindowBackground", new SolidColorBrush(Color.Parse("#F5F6F8")) },
        { "ControlBackground", new SolidColorBrush(Color.Parse("#FFFFFF")) },
        { "ControlBorder", new SolidColorBrush(Color.Parse("#D0D5DD")) },
        { "ControlHoverBackground", new SolidColorBrush(Color.Parse("#EDEFF3")) },

        { "PrimaryForeground", new SolidColorBrush(Color.Parse("#1A1D24")) },
        { "SecondaryForeground", new SolidColorBrush(Color.Parse("#5B6472")) },
        { "StatusForeground", new SolidColorBrush(Color.Parse("#5B6472")) },

        { "AccentBrush", new SolidColorBrush(Color.Parse("#3B82F6")) },
        { "DangerBrush", new SolidColorBrush(Color.Parse("#E74C3C")) },

        { "InputBackground", new SolidColorBrush(Color.Parse("#FFFFFF")) },
        { "InputBorder", new SolidColorBrush(Color.Parse("#D0D5DD")) },
        { "InputFocusBorder", new SolidColorBrush(Color.Parse("#3B82F6")) },

        { "ProgressBackground", new SolidColorBrush(Color.Parse("#E2E6EC")) },
    };
}
