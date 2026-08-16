using System;
using System.Globalization;
using Avalonia.Data.Converters;
using MCLCS.Core.Save;

namespace MCLCS.Linux.App.Converters;

/// <summary>NBT 标签类型 → 中文/简写。</summary>
public class NbtTagTypeToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            NbtTagType.End => "End",
            NbtTagType.Byte => "Byte",
            NbtTagType.Short => "Short",
            NbtTagType.Int => "Int",
            NbtTagType.Long => "Long",
            NbtTagType.Float => "Float",
            NbtTagType.Double => "Double",
            NbtTagType.ByteArray => "ByteArray",
            NbtTagType.String => "String",
            NbtTagType.List => "List",
            NbtTagType.Compound => "Compound",
            NbtTagType.IntArray => "IntArray",
            NbtTagType.LongArray => "LongArray",
            _ => value?.ToString() ?? ""
        };

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
