using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace KaxamlPlugins.Converters;

/// <summary>
/// Abstract base class for value converters that also acts as a MarkupExtension.
/// Enables usage in XAML without needing to declare a resource.
/// </summary>
/// <typeparam name="T">The type of the converter subclass.</typeparam>
public abstract class BaseConverter<T> : MarkupExtension, IValueConverter where T : class, new()
{
    // Singleton instance for XAML usage
    private static T? _instance;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return _instance ??= new T();
    }

    /// <summary>
    /// Override this method to implement forward conversion logic.
    /// </summary>
    public abstract object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture);

    /// <summary>
    /// Override this method to implement backward conversion logic.
    /// </summary>
    public abstract object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture);
}