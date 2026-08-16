using System.Globalization;
using Avalonia.Data.Converters;

namespace MCLCS.Linux.App.Converters;

/// <summary>把集合计数（int）转为可见性：大于 0 可见。</summary>
public class CountToVisibleConverter : IValueConverter
{
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => value is int n && n > 0;

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture) => null;
}
