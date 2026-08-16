using System;
using System.Globalization;
using Avalonia.Data.Converters;
using MCLCS.Core.Ai;

namespace MCLCS.Linux.App.Converters;

/// <summary>Ollama 服务状态 → 中文文本。</summary>
public class OllamaStatusToTextConverter : IValueConverter
{
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
            OllamaServiceStatus.NotRunning => "未运行",
            OllamaServiceStatus.Starting => "启动中",
            OllamaServiceStatus.Running => "运行中",
            _ => value?.ToString() ?? ""
        };
    }

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
