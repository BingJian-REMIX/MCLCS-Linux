using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using MCLCS.Core.UI;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Converters;

/// <summary>
/// 内容区「同色渐隐带」：把标题栏选中索引贴的颜色向下延续到页面（对齐 WPF 的
/// <c>ApplyPageTint</c>）。返回竖向 <see cref="LinearGradientBrush"/>：
/// 顶部用低饱和度混色保持可读性，向下快速渐隐到窗口底色。
/// 这样「索引贴与对应页面一体」且文字对比度足够。
/// </summary>
public class TabTintConverter : IValueConverter
{
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
    {
        var tabColor = Colors.Gray;
        if (value is MainTabKind kind && MainViewModel.Instance is { } vm)
            tabColor = HexToBrushConverter.ToColor(vm.Theme.ColorOf(kind));

        var winBg = Colors.Black;
        if (Application.Current?.Resources.TryGetValue("WindowBackground", out var bg) == true
            && bg is SolidColorBrush sb)
            winBg = sb.Color;

        var grad = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative)
        };
        // 低饱和混色：让文字（SecondaryForeground/PrimaryForeground）在工具页等强色标签下仍可读
        grad.GradientStops.Add(new GradientStop(Blend(tabColor, winBg, 0.22), 0.0));
        grad.GradientStops.Add(new GradientStop(Blend(tabColor, winBg, 0.10), 0.14));
        grad.GradientStops.Add(new GradientStop(winBg, 0.40));
        grad.GradientStops.Add(new GradientStop(winBg, 1.0));
        return grad;
    }

    private static Color Blend(Color src, Color dst, double srcRatio)
    {
        var r = srcRatio;
        var inv = 1.0 - r;
        return Color.FromArgb(
            255,
            (byte)(src.R * r + dst.R * inv),
            (byte)(src.G * r + dst.G * inv),
            (byte)(src.B * r + dst.B * inv));
    }

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => null;
}
