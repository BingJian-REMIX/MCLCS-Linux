using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MCLCS.Linux.App.Converters;

/// <summary>下载卡片来源非 Minecraft → true（控制"加入队列"按钮显隐）。</summary>
public class SourceNotMinecraftConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => !(value is string s && s == "Minecraft");

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => null;
}
