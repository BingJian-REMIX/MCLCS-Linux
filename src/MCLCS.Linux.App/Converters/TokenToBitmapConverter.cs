using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using MCLCS.Core.Theme;

namespace MCLCS.Linux.App.Converters;

/// <summary>
/// 把图标 token（与 Core.UI.SidebarItem.Icon / WPF PngIcon Token 一致，即文件名）
/// 转为 Avalonia Bitmap。对齐 WPF IconImage 的主题化加载：
/// <list type="bullet">
/// <item>按 <see cref="ThemeManager.Current"/> 从 <c>Resources/Icons/{light|dark}/{token}.png</c> 加载
///（dark=白系图标 / light=黑系图标，WPF 原版四套图标已随仓同步）；</item>
/// <item><see cref="IconManager.HighDpi"/> 开启时优先 <c>@2x</c> 高清目录；</item>
/// <item>主题目录缺失时回退顶层 <c>Resources/Icons/{token}.png</c>（Linux 独有图标如窗口控制 / 播放器）。</item>
/// </list>
/// token 缺失或加载失败返回 null（空白，不回退矢量）。
/// </summary>
public class TokenToBitmapConverter : IValueConverter
{
    public object? Convert(object? value, System.Type? targetType, object? parameter, CultureInfo? culture)
        => ToBitmap(value as string);

    public object? ConvertBack(object? value, System.Type? targetType, object? parameter, CultureInfo? culture)
        => null;

    /// <summary>静态版，供代码侧（如窗口控制图标）复用。token 为空或加载失败返回 null。</summary>
    public static Bitmap? ToBitmap(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        foreach (var rel in ResolveCandidates(token))
        {
            try
            {
                var uri = new Uri($"avares://MCLCS.Linux.App/{rel}");
                using var stream = AssetLoader.Open(uri);
                if (stream is not null)
                    return new Bitmap(stream);
            }
            catch
            {
                // 尝试下一个候选
            }
        }
        return null;
    }

    /// <summary>按当前主题 / 高清开关解析资源候选路径（主题目录优先，顶层回退）。供单元测试直接验证选择逻辑。</summary>
    public static IReadOnlyList<string> ResolveCandidates(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return Array.Empty<string>();

        var theme = ThemeManager.Current == ThemeType.Light ? "light" : "dark";
        var suffix = IconManager.HighDpi ? "@2x" : "";
        var themeDir = theme + suffix;

        return new[]
        {
            $"Resources/Icons/{themeDir}/{token}.png",
            $"Resources/Icons/{token}.png"
        };
    }
}
