using System;
using System.Globalization;
using Avalonia.Data.Converters;
using MCLCS.Core.Tokens;

namespace MCLCS.Linux.App.Converters;

/// <summary>挂机指令类型 → 中文说明。</summary>
public class AfkOpKindToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            AfkOpKind.FunctionKey => "功能键 F",
            AfkOpKind.Delay => "延迟等待",
            AfkOpKind.LongPress => "长按",
            AfkOpKind.KeyCode => "虚拟键码 K",
            AfkOpKind.Click => "连点",
            AfkOpKind.Repeat => "整体重复",
            _ => value?.ToString() ?? ""
        };

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
