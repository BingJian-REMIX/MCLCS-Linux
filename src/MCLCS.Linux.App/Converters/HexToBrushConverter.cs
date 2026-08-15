using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MCLCS.Linux.App.Converters;

/// <summary>把 Core.UI 的 #RRGGBB 配色串转为 Avalonia 画刷（四色标签用）。</summary>
public class HexToBrushConverter : IValueConverter
{
    public object? Convert(object? value, System.Type targetType, object? parameter, CultureInfo culture)
        => ToBrush(value as string);

    public object? ConvertBack(object? value, System.Type targetType, object? parameter, CultureInfo culture)
        => null;

    /// <summary>静态版，供 KindToBrushConverter 等复用。非法 / 空输入降级为 Gray。</summary>
    public static Brush ToBrush(string? hex)
    {
        if (!string.IsNullOrWhiteSpace(hex) && Color.TryParse(hex, out var c))
            return new SolidColorBrush(c);
        return new SolidColorBrush(Colors.Gray);
    }

    /// <summary>仅取色（用于渐变停靠点）。非法 / 空输入降级为 Gray。</summary>
    public static Color ToColor(string? hex)
    {
        if (!string.IsNullOrWhiteSpace(hex) && Color.TryParse(hex, out var c))
            return c;
        return Colors.Gray;
    }
}

/// <summary>bool → 画刷：true 取绿（正常），false 取红（异常）。状态栏网络指示用。</summary>
public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, System.Type? targetType, object? parameter, CultureInfo? culture)
        => value is true ? new SolidColorBrush(Color.Parse("#22C55E")) : new SolidColorBrush(Color.Parse("#E74C3C"));

    public object? ConvertBack(object? value, System.Type? targetType, object? parameter, CultureInfo? culture)
        => null;
}
