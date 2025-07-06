using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Kaxaml.Plugins

{
    public partial class About : UserControl
    {
        public About()
        {
            InitializeComponent();
            AddHandler(Hyperlink.RequestNavigateEvent, new RoutedEventHandler(HandleRequestNavigate), false);
            Loaded += About_Loaded;
        }

        private void About_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= About_Loaded;
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            VersionTextBlock.Text = version is not null 
                ? $"v{version.Major}.{version.Minor}.{version.Build}"
                : "UNKNOWN";
        }

        private void HandleRequestNavigate(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Hyperlink hl)
            {
                var navigateUri = hl.NavigateUri.ToString();
                Process.Start(new ProcessStartInfo(navigateUri));
                e.Handled = true;
            }
        }

    }
}
