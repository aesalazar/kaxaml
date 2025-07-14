using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KaxamlPlugins;
using Microsoft.Win32;

namespace Kaxaml.Plugins.Snapshot;

[Plugin(
    Name = "Snapshot",
    Icon = "Images\\picture.png",
    Description = "Capture and render content as an image (Ctrl+I)",
    ModifierKeys = ModifierKeys.Control,
    Key = Key.I
)]
public partial class SnapshotPlugin
{
    #region Constructors

    public SnapshotPlugin()
    {
        InitializeComponent();
        KaxamlInfo.ContentLoaded += KaxamlInfo_ContentLoaded;
    }

    #endregion Constructors

    #region Event Handlers

    private void KaxamlInfo_ContentLoaded()
    {
        RenderImage.Source = RenderContent();
    }

    #endregion Event Handlers

    #region Private Methods

    private void Copy(object sender, RoutedEventArgs e)
    {
        var bitmap = RenderContent();
        if (null != bitmap) Clipboard.SetImage(bitmap);
    }

    private BitmapSource? RenderContent()
    {
        var element = (FrameworkElement?)null;

        if (KaxamlInfo.Frame != null && KaxamlInfo.Frame.Content is FrameworkElement)
            element = KaxamlInfo.Frame.Content as FrameworkElement;
        else
            element = KaxamlInfo.Frame;


        if (element is { ActualWidth: > 0, ActualHeight: > 0 })
        {
            var rtb = new RenderTargetBitmap((int)element.ActualWidth, (int)element.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(element);
            return rtb;
        }

        return null;
    }

    private void Save(object sender, RoutedEventArgs e)
    {
        var rtb = RenderContent();
        if (null != rtb)
        {
            var sfd = new SaveFileDialog
            {
                Filter = "Image files (*.png)|*.png|All files (*.*)|*.*"
            };

            if (sfd.ShowDialog(KaxamlInfo.MainWindow) == true)
                using (var fs = new FileStream(sfd.FileName, FileMode.Create))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));
                    encoder.Save(fs);
                }
        }
    }

    #endregion Private Methods
}