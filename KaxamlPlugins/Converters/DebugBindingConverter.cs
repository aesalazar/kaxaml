using System.Diagnostics;
using System.Globalization;

namespace KaxamlPlugins.Converters;

/// <summary>
/// Allows for debugging bindings.
/// Usage: Converter="{x:Static local:DebugBindingConverter.Instance}"
/// </summary>
public class DebugBindingConverter : BaseConverter<DebugBindingConverter>
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        Debug.WriteLine($"[DebugBindingConverter] Convert Value: {value}");
        return value;
    }

    public override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        Debug.WriteLine($"[DebugBindingConverter] ConvertBack Value: {value}");
        return value;
    }
}