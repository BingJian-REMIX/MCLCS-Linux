using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MCLCS.Linux.App.Converters;

/// <summary>布尔 → 可见性（反相）：true 折叠，false 显示。Avalonia 的 IsVisible 为 bool，故返回 bool。</summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => !(value is true);

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => value is true;
}
