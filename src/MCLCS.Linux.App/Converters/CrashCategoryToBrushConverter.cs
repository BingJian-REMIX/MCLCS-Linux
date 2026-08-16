using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using MCLCS.Core.Launcher;

namespace MCLCS.Linux.App.Converters;

/// <summary>把崩溃类别（CrashCategory）映射到颜色笔刷，用于分析结论高亮。</summary>
public class CrashCategoryToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture) => value switch
    {
        CrashCategory.JavaVersion or CrashCategory.OutOfMemory => new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B)),
        CrashCategory.ModConflict or CrashCategory.LinkageError or CrashCategory.MissingLibrary => new SolidColorBrush(Color.FromRgb(0xFF, 0xA5, 0x4D)),
        CrashCategory.OpenGL or CrashCategory.ResourcePackOrShader => new SolidColorBrush(Color.FromRgb(0x4D, 0xA5, 0xFF)),
        _ => new SolidColorBrush(Color.FromRgb(0xC8, 0xCD, 0xD6))
    };

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture) => null;
}
