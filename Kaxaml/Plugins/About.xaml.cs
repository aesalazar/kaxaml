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
            AddHandler(Hyperlink.RequestNavigateEvent, new RoutedEventHandler(this.HandleRequestNavigate), false);
            Loaded += About_Loaded;
        }

        private void About_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= About_Loaded;
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            VersionTextBlock.Text = $"v{version.Major}.{version.Minor}.{version.Build}";
        }

        void HandleRequestNavigate(object sender, RoutedEventArgs e)
        {
            Hyperlink hl = (e.OriginalSource as Hyperlink);
            if (hl != null)
            {
                string navigateUri = hl.NavigateUri.ToString();
                Process.Start(new ProcessStartInfo(navigateUri));
                e.Handled = true;
            }
        }

    }
}
