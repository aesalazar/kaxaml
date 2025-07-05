namespace Kaxaml.Plugins.ColorPicker.Properties {
    
    internal sealed partial class Settings {
        
        public Settings() 
        {
            PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Settings_PropertyChanged);
        }

        void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Default.Save();
        }
    }
}
