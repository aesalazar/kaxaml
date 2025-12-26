using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Kaxaml.Plugins.XamlScrubber.XamlPrettyPrint;

/// <summary>
/// XAML Attribute/Value pair with comparison logic for sorting.
/// </summary>
public sealed class XamlAttributeValuePair : IComparable
{
    private readonly AttributeType _attributeType;

    public XamlAttributeValuePair(string name, string value)
    {
        Name = name;
        Value = value;

        if (name.StartsWith("xmlns"))
        {
            _attributeType = AttributeType.Namespace;
        }
        else
        {
            switch (name)
            {
                case "Key":
                case "x:Key":
                    _attributeType = AttributeType.Key;
                    break;

                case nameof(FrameworkElement.Name):
                case "x:Name":
                    _attributeType = AttributeType.Name;
                    break;

                case "x:Class":
                    _attributeType = AttributeType.Class;
                    break;

                case "Canvas.Top":
                case "Canvas.Left":
                case "Canvas.Bottom":
                case "Canvas.Right":
                case "Grid.Row":
                case "Grid.RowSpan":
                case "Grid.Column":
                case "Grid.ColumnSpan":
                    _attributeType = AttributeType.AttachedLayout;
                    break;

                case nameof(FrameworkElement.Width):
                case nameof(FrameworkElement.Height):
                case nameof(FrameworkElement.MaxWidth):
                case nameof(FrameworkElement.MinWidth):
                case nameof(FrameworkElement.MinHeight):
                case nameof(FrameworkElement.MaxHeight):
                    _attributeType = AttributeType.CoreLayout;
                    break;

                case nameof(FrameworkElement.Margin):
                case nameof(VerticalAlignment):
                case nameof(HorizontalAlignment):
                case "Panel.ZIndex":
                    _attributeType = AttributeType.StandardLayout;
                    break;

                case "mc:Ignorable":
                case "d:IsDataSource":
                case "d:LayoutOverrides":
                case "d:IsStaticText":
                    _attributeType = AttributeType.BlendGoo;
                    break;

                default:
                    _attributeType = AttributeType.Other;
                    break;
            }
        }
    }

    public string Name { get; }

    public string Value { get; }

    #region IComparable Members

    public int CompareTo(object? obj)
    {
        if (obj is not XamlAttributeValuePair other) return 0;
        if (_attributeType != other._attributeType)
            return _attributeType.CompareTo(other._attributeType);

        return Name switch
        {
            // special cases where we want things out of normal order
            nameof(LinearGradientBrush.StartPoint) when other.Name.Equals(nameof(LinearGradientBrush.EndPoint)) => -1,
            nameof(LinearGradientBrush.EndPoint) when other.Name.Equals(nameof(LinearGradientBrush.StartPoint)) => 1,

            nameof(FrameworkElement.Width) when other.Name.Equals(nameof(FrameworkElement.Height)) => -1,
            nameof(FrameworkElement.Height) when other.Name.Equals(nameof(FrameworkElement.Width)) => 1,

            "Offset" when other.Name.Equals(nameof(Color)) => -1,
            nameof(Color) when other.Name.Equals("Offset") => 1,

            nameof(Setter.TargetName) when other.Name.Equals(nameof(Setter.Property)) => -1,
            nameof(Setter.Property) when other.Name.Equals(nameof(Setter.TargetName)) => 1,

            _ => string.Compare(Name, other.Name, StringComparison.Ordinal)
        };
    }

    /// <summary>
    /// Determines if the element and its value are its default.
    /// </summary>
    /// <returns></returns>
    public static bool IsCommonDefault(XamlAttributeValuePair xamlAttributeValuePair)
    {
        var name = xamlAttributeValuePair.Name;
        var value = xamlAttributeValuePair.Value;
        return (name == nameof(HorizontalAlignment) && value == nameof(HorizontalAlignment.Stretch)) ||
               (name == nameof(VerticalAlignment) && value == nameof(VerticalAlignment.Stretch)) ||
               (name == nameof(FrameworkElement.Margin) && value == "0") ||
               (name == nameof(FrameworkElement.Margin) && value == "0,0,0,0") ||
               (name == nameof(FrameworkElement.Opacity) && value == "1") ||
               (name == nameof(FontWeight) && value == "{x:Null}") ||
               (name == nameof(Control.Background) && value == "{x:Null}") ||
               (name == nameof(Shape.Stroke) && value == "{x:Null}") ||
               (name == nameof(Shape.Fill) && value == "{x:Null}") ||
               (name == nameof(Visibility) && value == nameof(Visibility.Visible)) ||
               (name == "RowSpan" && value == "1") ||
               (name == "ColumnSpan" && value == "1") ||
               (name == nameof(Style.BasedOn) && value == "{x:Null}") ||
               (name != nameof(ColumnDefinition) && name != nameof(RowDefinition) && name == nameof(FrameworkElement.Width) && value == nameof(GridLength.Auto)) ||
               (name != nameof(ColumnDefinition) && name != nameof(RowDefinition) && name == nameof(FrameworkElement.Height) && value == nameof(GridLength.Auto));
    }

    /// <summary>
    /// Indicates if line breaking on certain elements should be suppressed because they are generally very short..
    /// </summary>
    public static bool ForceNoLineBreaks(string elementName) => elementName
        is nameof(RadialGradientBrush)
        or nameof(GradientStop)
        or nameof(LinearGradientBrush)
        or nameof(ScaleTransform)
        or nameof(SkewTransform)
        or nameof(RotateTransform)
        or nameof(TranslateTransform)
        or nameof(Trigger)
        or nameof(Setter);

    #endregion

    // note that these are declared in priority order for easy sorting
    private enum AttributeType
    {
        Key = 10,
        Name = 20,
        Class = 30,
        Namespace = 40,
        CoreLayout = 50,
        AttachedLayout = 60,
        StandardLayout = 70,
        Other = 1000,
        BlendGoo = 2000
    }
}