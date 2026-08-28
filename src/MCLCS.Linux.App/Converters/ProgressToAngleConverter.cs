using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MCLCS.Linux.App.Converters;

/// <summary>下载进度 (0-100) → Arc.SweepAngle (0-360)。</summary>
public class ProgressToAngleConverter : IValueConverter
{
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
            return Math.Max(0, Math.Min(100, d)) * 3.6;
        return 0d;
    }

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => null;
}
