using System.Windows;

namespace KaxamlPlugins.Controls;

public class DropDownColorPicker : ColorPicker
{
    static DropDownColorPicker()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DropDownColorPicker), new FrameworkPropertyMetadata(typeof(DropDownColorPicker)));
    }
}