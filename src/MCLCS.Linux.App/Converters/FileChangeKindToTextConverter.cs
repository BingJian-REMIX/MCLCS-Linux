using System;
using System.Globalization;
using Avalonia.Data.Converters;
using MCLCS.Core.Toolbox;

namespace MCLCS.Linux.App.Converters;

/// <summary>文件变更类型 → 中文文本。</summary>
public class FileChangeKindToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            FileChangeKind.Added => "新增",
            FileChangeKind.Removed => "删除",
            FileChangeKind.Modified => "修改",
            _ => value?.ToString() ?? ""
        };

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
