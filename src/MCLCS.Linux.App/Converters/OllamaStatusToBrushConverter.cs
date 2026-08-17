using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using MCLCS.Core.Ai;

namespace MCLCS.Linux.App.Converters;

/// <summary>Ollama 服务状态 → 服务灯颜色（对齐 WPF OllamaStatusBrush）：Running=绿 / Starting=金 / 其余=红。</summary>
public class OllamaStatusToBrushConverter : IValueConverter
{
    private static readonly IBrush Running = new SolidColorBrush(Color.Parse("#32CD32")); // LimeGreen
    private static readonly IBrush Starting = new SolidColorBrush(Color.Parse("#FFD700")); // Gold
    private static readonly IBrush NotRunning = new SolidColorBrush(Color.Parse("#E53935")); // Red

    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
    {
        var s = value switch
        {
            OllamaServiceStatus st => st,
            string str when Enum.TryParse<OllamaServiceStatus>(str, out var parsed) => parsed,
            _ => (OllamaServiceStatus?)null
        };
        return s switch
        {
            OllamaServiceStatus.Running => Running,
            OllamaServiceStatus.Starting => Starting,
            _ => NotRunning
        };
    }

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
