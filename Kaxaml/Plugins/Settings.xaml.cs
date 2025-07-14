using System.Windows;
using System.Windows.Controls;
using Kaxaml.Controls;

namespace Kaxaml.Plugins;

public partial class Settings : UserControl
{
    #region Constructors

    public Settings()
    {
        InitializeComponent();
    }

    #endregion Constructors

    #region Private Methods

    private void EditAgDefaultXaml(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.AgDefaultXaml = XamlEditorDialog.ShowModal(Properties.Settings.Default.AgDefaultXaml, "Default Silverlight Xaml", Application.Current.MainWindow);
    }

    private void EditWpfDefaultXaml(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.WPFDefaultXaml = XamlEditorDialog.ShowModal(Properties.Settings.Default.WPFDefaultXaml, "Default WPF Xaml", Application.Current.MainWindow);
    }

    #endregion Private Methods
}