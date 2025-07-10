using System.Windows;
using System.Windows.Media;

namespace KaxamlPlugins.Utilities;

public class ColorChangedEventArgs : RoutedEventArgs
{
    public ColorChangedEventArgs(RoutedEvent routedEvent, Color color)
    {
        RoutedEvent = routedEvent;
        Color = color;
    }

    public Color Color { get; set; }
}