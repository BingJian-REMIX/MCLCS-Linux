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

        // 卡片：半透明玻璃质感（对齐 WPF CardBackground=#B31A1D24，alpha≈0.70）
        { "CardBackground", new SolidColorBrush(Color.Parse("#B31A1D24")) },
        { "CardBorderHover", new SolidColorBrush(Color.Parse("#3B82F6")) },
        { "CardShadow", BoxShadows.Parse("0 2 10 0 #00000047") },

        { "ScrimBrush", new SolidColorBrush(Color.Parse("#00000080")) },
        { "AccentHover", new SolidColorBrush(Color.Parse("#2B6FE0")) },

        { "PrimaryForeground", new SolidColorBrush(Color.Parse("#FFFFFF")) },
        { "SecondaryForeground", new SolidColorBrush(Color.Parse("#C8CDD6")) },
        { "StatusForeground", new SolidColorBrush(Color.Parse("#C8CDD6")) },

        // 语义化文字色：统一随主题翻转，避免页面硬编码 hex 导致亮/暗下亮度异常
        { "CodeForeground", new SolidColorBrush(Color.Parse("#9cdcfe")) },       // 代码 / 高亮提示（暗底浅蓝）
        { "WarningForeground", new SolidColorBrush(Color.Parse("#E67E22")) },    // 警告（橙）
        { "SuccessForeground", new SolidColorBrush(Color.Parse("#27AE60")) },    // 成功（绿）

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
        { "WindowBackground", new SolidColorBrush(Color.Parse("#F5F5F5")) },
        { "ControlBackground", new SolidColorBrush(Color.Parse("#FFFFFF")) },
        { "ControlBorder", new SolidColorBrush(Color.Parse("#D0D5DD")) },
        { "ControlHoverBackground", new SolidColorBrush(Color.Parse("#EDEFF3")) },

        // 卡片：近白玻璃（对齐 WPF Light CardBackground=#E6FFFFFF，alpha≈0.90）
        { "CardBackground", new SolidColorBrush(Color.Parse("#E6FFFFFF")) },
        { "CardBorderHover", new SolidColorBrush(Color.Parse("#2563EB")) },
        { "CardShadow", BoxShadows.Parse("0 2 10 0 #0000002E") },

        { "ScrimBrush", new SolidColorBrush(Color.Parse("#00000059")) },
        { "AccentHover", new SolidColorBrush(Color.Parse("#1D4ED8")) },

        { "PrimaryForeground", new SolidColorBrush(Color.Parse("#1A1D24")) },
        { "SecondaryForeground", new SolidColorBrush(Color.Parse("#5B6472")) },
        { "StatusForeground", new SolidColorBrush(Color.Parse("#5B6472")) },

        // 语义化文字色：与暗色同一套语义，亮底改用深一档保证对比度（统一亮暗亮度）
        { "CodeForeground", new SolidColorBrush(Color.Parse("#0B5394")) },       // 代码 / 高亮提示（亮底深蓝）
        { "WarningForeground", new SolidColorBrush(Color.Parse("#B45309")) },    // 警告（深橙）
        { "SuccessForeground", new SolidColorBrush(Color.Parse("#15803D")) },    // 成功（深绿）

        { "AccentBrush", new SolidColorBrush(Color.Parse("#2563EB")) },
        { "DangerBrush", new SolidColorBrush(Color.Parse("#DC2626")) },

        { "InputBackground", new SolidColorBrush(Color.Parse("#FFFFFF")) },
        { "InputBorder", new SolidColorBrush(Color.Parse("#D0D5DD")) },
        { "InputFocusBorder", new SolidColorBrush(Color.Parse("#3B82F6")) },

        { "ProgressBackground", new SolidColorBrush(Color.Parse("#E2E6EC")) },
    };
}
