using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MCLCS.Linux.App.Converters;

/// <summary>布尔 → 可见性。true 显示，false 折叠（用于「校验通过」这类正向后示）。</summary>
public class BoolToVisibleConverter : IValueConverter
{
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => value is true;

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => value is true;
}
