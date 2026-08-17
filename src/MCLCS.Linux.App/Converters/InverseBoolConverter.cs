using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MCLCS.Linux.App.Converters;

/// <summary>布尔反转（用于 ToggleButton.IsChecked 等需要纯 bool 反转的绑定）。
/// 与 InverseBoolToVisibilityConverter 区别：后者 ConvertBack 走"可见性→bool"语义（value is true），
/// 本转换器 Convert / ConvertBack 都是 value is false。</summary>
public class InverseBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => !(value is true);

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => !(value is true);
}
