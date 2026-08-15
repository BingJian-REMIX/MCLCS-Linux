using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using MCLCS.Core.UI;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Converters;

/// <summary>
/// 按 <see cref="MainTabKind"/> 从 <see cref="MainViewModel.Theme"/>(Core.TabThemeConfig) 取色，
/// 使标签颜色可被用户在设置页实时自定义。取不到时降级 Gray。
/// </summary>
public class KindToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
    {
        if (value is MainTabKind kind && MainViewModel.Instance is { } vm)
            return HexToBrushConverter.ToBrush(vm.Theme.ColorOf(kind));
        return new SolidColorBrush(Colors.Gray);
    }

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => null;
}
