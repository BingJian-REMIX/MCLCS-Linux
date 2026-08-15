using System;
using System.Globalization;
using Avalonia.Data.Converters;
using MCLCS.Linux.App;

namespace MCLCS.Linux.App.Converters;

/// <summary>把 Core.UI 的 l10n key（如 <c>tab.game</c>）翻译为本地化显示名。</summary>
public class KeyToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => Localization.Get(value as string);

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        => null;
}
