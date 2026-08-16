using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MCLCS.Linux.App.Converters;

/// <summary>布尔 → 颜色笔刷。true 绿、false 红（用于网络可达性等状态指示）。</summary>
public class BoolToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush Ok = new(Color.Parse("#27AE60"));
    private static readonly SolidColorBrush Bad = new(Color.Parse("#E74C3C"));

    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => value is true ? Ok : Bad;

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
