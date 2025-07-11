using System.Windows.Input;

namespace KaxamlPlugins;

[AttributeUsage(AttributeTargets.Class)]
public class PluginAttribute : Attribute
{
    public required string Name { get; set; }

    public required string Description { get; set; }

    public Key Key { get; set; }

    public ModifierKeys ModifierKeys { get; set; }

    public required string Icon { get; set; }

    public override string ToString()
    {
        return $"PluginAttribute: {Name
        } | Description: {Description
        } | Key: {Key
        } | Modifiers: {ModifierKeys
        } | Icon: {Icon}";
    }
}