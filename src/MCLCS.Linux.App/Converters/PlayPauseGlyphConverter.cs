using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MCLCS.Linux.App.Converters;

/// <summary>播放状态 → 图标字形：播放中显示暂停符 ⏸，否则显示播放符 ▶。</summary>
public class PlayPauseGlyphConverter : IValueConverter
{
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => value is true ? "⏸" : "▶";

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => null;
}
