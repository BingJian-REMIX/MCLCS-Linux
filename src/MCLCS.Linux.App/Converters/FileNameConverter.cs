using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;

namespace MCLCS.Linux.App.Converters;

/// <summary>把完整路径转换为文件名（用于列表展示）。</summary>
public class FileNameConverter : IValueConverter
{
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => value is string s && s.Length > 0 ? Path.GetFileName(s) : value;

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture) => null;
}
