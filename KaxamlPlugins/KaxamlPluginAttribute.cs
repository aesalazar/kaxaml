using System.Windows.Input;

namespace Kaxaml.Plugins
{
    [AttributeUsage(AttributeTargets.Class)]

    public class PluginAttribute : Attribute
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public Key Key { get; set; }

        public ModifierKeys ModifierKeys { get; set; }

        public string Icon { get; set; }

    }
}
