using System;

namespace MCLCS.Linux.App.Converters;

/// <summary>
/// 图标高清（2x）开关（对齐 WPF Themes/IconManager）：
/// 开启后 <see cref="TokenToBitmapConverter"/> 优先加载 <c>@2x</c> 目录图标，
/// 在高 DPI（如 4K / 缩放 &gt; 100%）屏幕上渲染更清晰。
/// 状态由设置 → 通用「适配高分辨率屏幕」开关驱动并持久化到 profile.HighDpiIcons。
/// 变化时广播 <see cref="HighDpiChanged"/>，界面重建 / 页面切换时据此重新加载图标。
/// </summary>
public static class IconManager
{
    private static bool _highDpi;

    /// <summary>是否启用 2x 高清图标。</summary>
    public static bool HighDpi
    {
        get => _highDpi;
        set
        {
            if (_highDpi == value) return;
            _highDpi = value;
            HighDpiChanged?.Invoke();
        }
    }

    /// <summary>高清图标开关变化事件。</summary>
    public static event Action? HighDpiChanged;
}
