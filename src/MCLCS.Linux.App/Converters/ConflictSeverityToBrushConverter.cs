using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using MCLCS.Core.Toolbox;

namespace MCLCS.Linux.App.Converters;

/// <summary>把数据包冲突严重度（ConflictSeverity）映射到颜色笔刷。</summary>
public class ConflictSeverityToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture) => value switch
    {
        ConflictSeverity.Critical => new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B)),
        ConflictSeverity.Warning => new SolidColorBrush(Color.FromRgb(0xFF, 0xD9, 0x3D)),
        ConflictSeverity.Info => new SolidColorBrush(Color.FromRgb(0x7F, 0x8C, 0x8D)),
        _ => new SolidColorBrush(Color.FromRgb(0xC8, 0xCD, 0xD6))
    };

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture) => null;
}
