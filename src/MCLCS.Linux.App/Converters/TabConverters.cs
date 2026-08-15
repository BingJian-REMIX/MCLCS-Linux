using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using MCLCS.Core.UI;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Converters;

/// <summary>通用相等判定（多值）：values[0] == values[1] 时返回 true。用于标签/侧栏选中高亮。</summary>
public class EqualsConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, System.Type? targetType, object? parameter, CultureInfo? culture)
    {
        if (values.Count < 2) return false;
        return Equals(values[0], values[1]);
    }
}

/// <summary>
/// 主标签激活态取色（多值）：values[0]=该项 Kind，values[1]=当前选中 Kind。
/// 选中 → 提亮 1.12 的实色（对齐 WPF 的 Tab{Kind}ActiveBrush），未选中 → 实色。
/// </summary>
public class TabActiveBrushConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, System.Type? targetType, object? parameter, CultureInfo? culture)
    {
        if (values.Count < 2 || values[0] is not MainTabKind item || values[1] is not MainTabKind selected)
            return new SolidColorBrush(Colors.Gray);
        var theme = MainViewModel.Instance?.Theme;
        var hex = theme is not null && item == selected ? theme.ActiveColorOf(item) : theme?.ColorOf(item) ?? "#607D8B";
        return HexToBrushConverter.ToBrush(hex);
    }
}
