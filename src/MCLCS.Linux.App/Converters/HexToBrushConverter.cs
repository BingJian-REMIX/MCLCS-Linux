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
}
