using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Converters;

/// <summary>索引贴宽度：展开 130 / 折叠 56（对齐 WPF MainTabs.ExpandedWidth/CollapsedWidth）。</summary>
public class TabWidthConverter : IValueConverter
{
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        => value is true ? 130.0 : 56.0;

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        => null;
}

/// <summary>索引贴背景：选中取主题实色，未选中暗化 0.68 以凸显激活页。</summary>
public class TabBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        if (value is not TabItemViewModel tvm)
            return new SolidColorBrush(Colors.Gray);
        var hex = MainViewModel.Instance?.Theme?.ColorOf(tvm.Kind) ?? "#888888";
        var c = HexToBrushConverter.ToColor(hex);
        if (!tvm.IsSelected)
            c = Darken(c, 0.68);
        return new SolidColorBrush(c);
    }

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        => null;

    private static Color Darken(Color c, double f) =>
        Color.FromRgb(Clamp((int)(c.R * f)), Clamp((int)(c.G * f)), Clamp((int)(c.B * f)));

    private static byte Clamp(int v) => v < 0 ? (byte)0 : v > 255 ? (byte)255 : (byte)v;
}

/// <summary>索引贴外边距：首项无偏移，其余左移形成重叠（对齐 WPF MainTabs.CollapsedOverlap=20）。</summary>
public class TabMarginConverter : IValueConverter
{
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        => value is int order && order > 0 ? new Thickness(-20, 0, 0, 0) : new Thickness(0);

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        => null;
}

/// <summary>索引贴圆角（对齐 WPF TabCornerRadius）：最左贴左圆右直、最右贴右圆左直、中间全直，
/// 使重叠接缝侧切直、外侧圆角，整体呈圆头药丸外观。</summary>
public class TabCornerConverter : IValueConverter
{
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        if (value is not TabItemViewModel t) return new CornerRadius(0);
        if (t.Order == 0) return new CornerRadius(8, 0, 0, 8);                 // 最左：左圆右直
        if (t.Order == t.TotalTabs - 1) return new CornerRadius(0, 8, 8, 0);  // 最右：右圆左直
        return new CornerRadius(0);                                           // 中间：全直
    }

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        => null;
}
