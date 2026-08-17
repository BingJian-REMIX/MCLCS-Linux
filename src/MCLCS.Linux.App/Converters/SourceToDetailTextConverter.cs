using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MCLCS.Linux.App.Converters;

/// <summary>下载卡片"详情/安装"按钮文案：来源为 Minecraft 时显示"安装"，其余显示"详情"。</summary>
public class SourceToDetailTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s && s == "Minecraft" ? "安装" : "详情";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => null;
}
