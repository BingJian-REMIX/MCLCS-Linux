using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace MCLCS.Linux.App.Converters;

/// <summary>
/// 把图标 token（与 Core.UI.SidebarItem.Icon / WPF PngIcon Token 一致，即文件名）
/// 转为 Avalonia Bitmap，从程序集内嵌资源 <c>avares://MCLCS.Linux.App/Resources/Icons/{token}.png</c> 加载。
/// 对齐 WPF 的 PngIcon：token 缺失或加载失败时返回 null（空白，不回退矢量）。
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
        try
        {
            var uri = new Uri($"avares://MCLCS.Linux.App/Resources/Icons/{token}.png");
            return new Bitmap(AssetLoader.Open(uri));
        }
        catch
        {
            return null;
        }
    }
}
