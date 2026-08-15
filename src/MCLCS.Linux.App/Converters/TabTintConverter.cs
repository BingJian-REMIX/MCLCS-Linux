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
/// 顶部 0–10% 为选中主标签实色，55% 起渐隐到窗口底色，100% 为窗口底色。
/// 这样「索引贴与对应页面一体」，选中标签像粘在下方内容页上。
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
        grad.GradientStops.Add(new GradientStop(tabColor, 0.0));
        grad.GradientStops.Add(new GradientStop(tabColor, 0.10));
        grad.GradientStops.Add(new GradientStop(winBg, 0.55));
        grad.GradientStops.Add(new GradientStop(winBg, 1.0));
        return grad;
    }

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => null;
}
