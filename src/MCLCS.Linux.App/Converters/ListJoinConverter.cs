using System.Collections;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MCLCS.Linux.App.Converters;

/// <summary>把字符串集合拼接为逗号分隔文本（用于冲突涉及的数据包 / 问题列表）。</summary>
public class ListJoinConverter : IValueConverter
{
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => value is IEnumerable enumerable
            ? string.Join(", ", enumerable.OfType<object>().Select(x => x.ToString()))
            : value;

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture) => null;
}
