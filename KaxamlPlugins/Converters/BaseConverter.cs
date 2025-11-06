using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace KaxamlPlugins.Converters;

/// <summary>
/// Abstract base class for value converters that also acts as a MarkupExtension.
/// Enables usage in XAML without needing to declare a resource.
/// </summary>
/// <typeparam name="TConverter">The type of the converter subclass.</typeparam>
public abstract class BaseConverter<TConverter> : MarkupExtension, IValueConverter where TConverter : class, new()
{
    // Singleton instance for XAML usage
    private static TConverter? _instance;

    /// <summary>
    /// Override this method to implement forward conversion logic.
    /// </summary>
    public abstract object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture);

    /// <summary>
    /// Override this method to implement backward conversion logic.
    /// </summary>
    public abstract object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture);

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        _instance ??= new TConverter();
}