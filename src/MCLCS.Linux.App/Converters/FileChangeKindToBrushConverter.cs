using Avalonia.Data.Converters;
using Avalonia.Media;
using MCLCS.Core.Toolbox;
using System;
using System.Globalization;

namespace MCLCS.Linux.App.Converters;

/// <summary>文件变更类型 → 徽章背景色（新增绿 / 删除红 / 修改橙）。</summary>
public sealed class FileChangeKindToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => value is FileChangeKind kind ? kind switch
        {
            FileChangeKind.Added => new SolidColorBrush(Color.Parse("#27AE60")),
            FileChangeKind.Removed => new SolidColorBrush(Color.Parse("#E74C3C")),
            FileChangeKind.Modified => new SolidColorBrush(Color.Parse("#E67E22")),
            _ => new SolidColorBrush(Color.Parse("#7F8C8D")),
        } : new SolidColorBrush(Color.Parse("#7F8C8D"));

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
