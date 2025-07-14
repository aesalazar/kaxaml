namespace Kaxaml.Properties;

internal sealed partial class Settings
{
    public Settings()
    {
        PropertyChanged += Settings_PropertyChanged;
    }

    private void Settings_PropertyChanged(object? _, System.ComponentModel.PropertyChangedEventArgs __)
    {
        Default.Save();
    }
}