using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using KaxamlPlugins;
using KaxamlPlugins.Controls;

namespace Kaxaml.Plugins.ColorPicker
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>

    [Plugin(
        Name = "Color Picker",
        Icon = "Images\\icon.png",
        Description = "Generate colors and save palletes (Ctrl+P)",
        ModifierKeys = ModifierKeys.Control,
        Key = Key.P
     )]

    public partial class ColorPickerPlugin : UserControl
    {
        public ColorPickerPlugin()
        {
            InitializeComponent();
            Colors = new ObservableCollection<Color>();
            ColorString = Properties.Settings.Default.Colors;

            KaxamlInfo.EditSelectionChanged += KaxamlInfo_EditSelectionChanged;
        }

        #region Sync Interaction Logic

        private ColorConverter _converter = new();

        private void KaxamlInfo_EditSelectionChanged(string? selectedText)
        {
            // wish we could do this without a try catch!
            try
            {
                SyncButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                if (ex.IsCriticalException())
                {
                    throw;
                }

                SyncButton.IsEnabled = false;
                SyncButton.IsChecked = false;
            }
        }

        private void SyncButtonChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                var c = (Color)ColorConverter.ConvertFromString(KaxamlInfo.Editor?.SelectedText);
                C.Color = c;
                C.ColorChanged += C_ColorChanged;
            }
            catch (Exception ex)
            {
                if (ex.IsCriticalException())
                {
                    throw;
                }

                SyncButton.IsEnabled = false;
            }
        }

        private void SyncButtonUnChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                C.ColorChanged -= C_ColorChanged;
            }
            catch (Exception ex)
            {
                if (ex.IsCriticalException())
                {
                    throw;
                }

                SyncButton.IsEnabled = false;
            }
        }

        private DispatcherTimer? _colorChangedTimer;
        private Color _colorChangedColor;

        private void C_ColorChanged(object? sender, ColorChangedEventArgs e)
        {
            if (SyncButton.IsChecked is true)
            {
                try
                {
                    _colorChangedTimer ??= new DispatcherTimer(
                        TimeSpan.FromMilliseconds(200), 
                        DispatcherPriority.Background,
                        _ColorChangedTimer_Tick,
                        Dispatcher);

                    _colorChangedTimer.Stop();
                    _colorChangedTimer.Start();

                    _colorChangedColor = e.Color;
                }
                catch (Exception ex)
                {
                    if (ex.IsCriticalException())
                    {
                        throw;
                    }
                }
            }
        }

        private void _ColorChangedTimer_Tick(object? sender, EventArgs e)
        {
            _colorChangedTimer?.Stop();

            KaxamlInfo.Editor?.ReplaceSelectedText(_colorChangedColor.ToString());
            KaxamlInfo.Parse();
        }

        #endregion

        #region Event Handlers  

        private void CopyColor(object o, EventArgs e)
        {
            Clipboard.SetText(C.Color.ToString());
        }

        private void SaveColor(object o, EventArgs e)
        {
            Colors.Add(C.Color);
        }

        private void RemoveColor(object o, EventArgs e)
        {
            var cm = (ContextMenu)ItemsControl.ItemsControlFromItemContainer(o as MenuItem);
            var lbi = (ListBoxItem)cm.PlacementTarget;
            Colors.Remove((Color)lbi.Content);
        }

        private void RemoveAllColors(object o, EventArgs e)
        {
            Colors.Clear();
        }

        private void SwatchMouseDown(object o, MouseEventArgs e)
        {
            var c = (Color)((FrameworkElement)o).DataContext;
            C.Color = c;
        }

        #endregion

        #region Colors (DependencyProperty)

        /// <summary>
        /// description of the property
        /// </summary>
        public ObservableCollection<Color> Colors
        { get => (ObservableCollection<Color>)GetValue(ColorsProperty); set => SetValue(ColorsProperty, value);
        }

        /// <summary>
        /// DependencyProperty for Colors
        /// </summary>
        public static readonly DependencyProperty ColorsProperty =
            DependencyProperty.Register(nameof(Colors), typeof(ObservableCollection<Color>), typeof(ColorPickerPlugin), new FrameworkPropertyMetadata(default(ObservableCollection<Color>), ColorsChanged));

        /// <summary>
        /// PropertyChangedCallback for Colors
        /// </summary>
        private static void ColorsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj is ColorPickerPlugin owner)
            {
                if (args.NewValue is ObservableCollection<Color> c)
                {
                    c.CollectionChanged += owner.c_CollectionChanged;
                }

            }
        }

        private void c_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!_updateInternal)
            {
                _updateInternal = true;

                var s = "";
                foreach (var c in Colors)
                {
                    s = s + c + _delimiter;
                }
                ColorString = s;

                _updateInternal = false;
            }
        }

        #endregion

        #region ColorString (DependencyProperty)

        /// <summary>
        /// description of the property
        /// </summary>
        public string ColorString
        { get => (string)GetValue(ColorStringProperty); set => SetValue(ColorStringProperty, value);
        }

        /// <summary>
        /// DependencyProperty for ColorString
        /// </summary>
        public static readonly DependencyProperty ColorStringProperty =
            DependencyProperty.Register(nameof(ColorString), typeof(string), typeof(ColorPickerPlugin), new FrameworkPropertyMetadata(
                default(string),
                ColorStringChanged));

        /// <summary>
        /// PropertyChangedCallback for ColorString
        /// </summary>
        private static void ColorStringChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj is ColorPickerPlugin owner)
            {
                Properties.Settings.Default.Colors = args.NewValue as string;

                if (!owner._updateInternal)
                {
                    owner._updateInternal = true;

                    owner.Colors.Clear();
                    var colors = ((string)args.NewValue).Split(owner._delimiter);

                    foreach (var s in colors)
                    {
                        try
                        {
                            if (s.Length > 3)
                            {
                                var c = ColorPickerUtil.ColorFromString(s);
                                owner.Colors.Add(c);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex.IsCriticalException())
                            {
                                throw;
                            }
                        }
                    }

                    owner._updateInternal = false;
                }
            }
        }

        #endregion

        private readonly char _delimiter = '|';
        private bool _updateInternal;
    }
}