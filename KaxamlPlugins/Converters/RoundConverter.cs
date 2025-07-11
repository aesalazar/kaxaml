using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KaxamlPlugins.Converters;

public class RoundConverter : IValueConverter
{
    #region IValueConverter Members

    object IValueConverter.Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null && double.TryParse(value.ToString(), out var val)
            ? Math.Round(val, 2)
            : DependencyProperty.UnsetValue;
    }

    object IValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return DependencyProperty.UnsetValue;
    }

    #endregion
}