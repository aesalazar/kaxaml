using System.ComponentModel;

namespace Kaxaml.Plugins.XamlScrubber.Properties;

internal sealed partial class Settings
{
    public Settings()
    {
        PropertyChanged += Settings_PropertyChanged;
    }

    private void Settings_PropertyChanged(object? _, PropertyChangedEventArgs __)
    {
        Default.Save();
    }
}