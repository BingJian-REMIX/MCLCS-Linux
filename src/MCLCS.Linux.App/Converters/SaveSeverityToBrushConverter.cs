using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using MCLCS.Core.Save;

namespace MCLCS.Linux.App.Converters;

/// <summary>存档严重度 → 颜色笔刷。兼容 SaveCorruptionSeverity 与 SaveCompatibilitySeverity 两套枚举。</summary>
public class SaveSeverityToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var ok = new SolidColorBrush(Color.Parse("#27AE60"));
        var warn = new SolidColorBrush(Color.Parse("#E67E22"));
        var danger = new SolidColorBrush(Color.Parse("#E74C3C"));
        var mute = new SolidColorBrush(Color.Parse("#7F8C8D"));

        switch (value)
        {
            case SaveCorruptionSeverity s:
                return s switch
                {
                    SaveCorruptionSeverity.Corrupt => danger,
                    SaveCorruptionSeverity.Warning => warn,
                    _ => ok
                };
            case SaveCompatibilitySeverity s:
                return s switch
                {
                    SaveCompatibilitySeverity.MuchNewer => danger,
                    SaveCompatibilitySeverity.SlightlyNewer => warn,
                    SaveCompatibilitySeverity.Unknown => mute,
                    _ => ok
                };
            default:
                return ok;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
