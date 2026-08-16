using System;
using System.Globalization;
using Avalonia.Data.Converters;
using MCLCS.Core.Tokens;

namespace MCLCS.Linux.App.Converters;

/// <summary>挂机指令 → 中文可读描述（调用 AfkInstruction.Describe()）。</summary>
public class AfkInstructionToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => value is AfkInstruction inst ? inst.Describe() : "";

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
