using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace KaxamlPlugins.Controls
{
    public class DropDownColorPicker : ColorPicker
    {
        static DropDownColorPicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DropDownColorPicker), new FrameworkPropertyMetadata(typeof(DropDownColorPicker)));
        }
    }


    public class ColorPicker : Control
    {
        static ColorPicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorPicker), new FrameworkPropertyMetadata(typeof(ColorPicker)));
        }

        /// <summary>
        /// ColorBrush Property
        /// </summary>

        public SolidColorBrush ColorBrush
        { get => (SolidColorBrush)GetValue(ColorBrushProperty); set => SetValue(ColorBrushProperty, value);
        }
        public static readonly DependencyProperty ColorBrushProperty =
            DependencyProperty.Register(nameof(ColorBrush), typeof(SolidColorBrush), typeof(ColorPicker), new UIPropertyMetadata(Brushes.Black));



        /// <summary>
        /// Color Property
        /// </summary>
        private bool _hsbSetInternally;

        private bool _rgbSetInternally;

        public static void OnColorChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var c = (ColorPicker)o;


            if (e.NewValue is Color color)
            {
                if (!c._hsbSetInternally)
                {
                    // update HSB value based on new value of color

                    double h = 0;
                    double s = 0;
                    double b = 0;
                    ColorPickerUtil.HsbFromColor(color, ref h, ref s, ref b);

                    c._hsbSetInternally = true;

                    c.Alpha = color.A / 255;
                    c.Hue = h;
                    c.Saturation = s;
                    c.Brightness = b;

                    c._hsbSetInternally = false;
                }

                if (!c._rgbSetInternally)
                {
                    // update RGB value based on new value of color

                    c._rgbSetInternally = true;

                    c.A = color.A;
                    c.R = color.R;
                    c.G = color.G;
                    c.B = color.B;

                    c._rgbSetInternally = false;
                }

                c.RaiseColorChangedEvent(color);

            }
        }

        public Color Color
        { get => (Color)GetValue(ColorProperty); set => SetValue(ColorProperty, value);
        }
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(nameof(Color), typeof(Color), typeof(ColorPicker), new UIPropertyMetadata(Colors.Black, OnColorChanged));

        /// <summary>
        /// Hue Property
        /// </summary>

        public double Hue
        { get => (double)GetValue(HueProperty); set => SetValue(HueProperty, value);
        }

        public static readonly DependencyProperty HueProperty =
            DependencyProperty.Register(nameof(Hue), typeof(double), typeof(ColorPicker),
            new FrameworkPropertyMetadata(0.0,
            UpdateColorHsb,
            HueCoerce));

        public static object HueCoerce(DependencyObject d, object hue)
        {
            var v = (double)hue;
            if (v < 0) return 0.0;
            if (v > 1) return 1.0;
            return v;
        }

        /// <summary>
        /// Brightness Property
        /// </summary>

        public double Brightness
        { get => (double)GetValue(BrightnessProperty); set => SetValue(BrightnessProperty, value);
        }

        public static readonly DependencyProperty BrightnessProperty =
            DependencyProperty.Register(nameof(Brightness), typeof(double), typeof(ColorPicker),
            new FrameworkPropertyMetadata(0.0,
            UpdateColorHsb,
            BrightnessCoerce));

        public static object BrightnessCoerce(DependencyObject d, object brightness)
        {
            var v = (double)brightness;
            if (v < 0) return 0.0;
            if (v > 1) return 1.0;
            return v;
        }

        /// <summary>
        /// Saturation Property
        /// </summary>

        public double Saturation
        { get => (double)GetValue(SaturationProperty); set => SetValue(SaturationProperty, value);
        }

        public static readonly DependencyProperty SaturationProperty =
            DependencyProperty.Register(nameof(Saturation), typeof(double), typeof(ColorPicker),
            new FrameworkPropertyMetadata(0.0,
            UpdateColorHsb,
            SaturationCoerce));

        public static object SaturationCoerce(DependencyObject d, object saturation)
        {
            var v = (double)saturation;
            if (v < 0) return 0.0;
            if (v > 1) return 1.0;
            return v;
        }

        /// <summary>
        /// Alpha Property
        /// </summary>

        public double Alpha
        { get => (double)GetValue(AlphaProperty); set => SetValue(AlphaProperty, value);
        }

        public static readonly DependencyProperty AlphaProperty =
            DependencyProperty.Register(nameof(Alpha), typeof(double), typeof(ColorPicker),
            new FrameworkPropertyMetadata(1.0,
            UpdateColorHsb,
            AlphaCoerce));

        public static object AlphaCoerce(DependencyObject d, object alpha)
        {
            var v = (double)alpha;
            if (v < 0) return 0.0;
            if (v > 1) return 1.0;
            return v;
        }


        /// <summary>
        /// Shared property changed callback to update the Color property
        /// </summary>

        public static void UpdateColorHsb(object o, DependencyPropertyChangedEventArgs e)
        {
            var c = (ColorPicker)o;
            var n = ColorPickerUtil.ColorFromAhsb(c.Alpha, c.Hue, c.Saturation, c.Brightness);

            c._hsbSetInternally = true;

            c.Color = n;
            c.ColorBrush = new SolidColorBrush(n);

            c._hsbSetInternally = false;
        }

        /// <summary>
        /// R Property
        /// </summary>

        public int R
        { get => (int)GetValue(RProperty); set => SetValue(RProperty, value);
        }

        public static readonly DependencyProperty RProperty =
            DependencyProperty.Register(nameof(R), typeof(int), typeof(ColorPicker),
            new FrameworkPropertyMetadata(default(int),
            UpdateColorRgb,
            RgbCoerce));


        /// <summary>
        /// G Property
        /// </summary>

        public int G
        { get => (int)GetValue(GProperty); set => SetValue(GProperty, value);
        }

        public static readonly DependencyProperty GProperty =
            DependencyProperty.Register(nameof(G), typeof(int), typeof(ColorPicker),
            new FrameworkPropertyMetadata(default(int),
            UpdateColorRgb,
            RgbCoerce));

        /// <summary>
        /// B Property
        /// </summary>

        public int B
        { get => (int)GetValue(BProperty); set => SetValue(BProperty, value);
        }

        public static readonly DependencyProperty BProperty =
            DependencyProperty.Register(nameof(B), typeof(int), typeof(ColorPicker),
            new FrameworkPropertyMetadata(default(int),
            UpdateColorRgb,
            RgbCoerce));


        /// <summary>
        /// A Property
        /// </summary>

        public int A
        { get => (int)GetValue(AProperty); set => SetValue(AProperty, value);
        }

        public static readonly DependencyProperty AProperty =
            DependencyProperty.Register(nameof(A), typeof(int), typeof(ColorPicker),
            new FrameworkPropertyMetadata(255,
            UpdateColorRgb,
            RgbCoerce));



        public static object RgbCoerce(DependencyObject d, object value)
        {
            var v = (int)value;
            if (v < 0) return 0;
            if (v > 255) return 255;
            return v;
        }

        /// <summary>
        /// Shared property changed callback to update the Color property
        /// </summary>

        public static void UpdateColorRgb(object o, DependencyPropertyChangedEventArgs e)
        {
            var c = (ColorPicker)o;
            var n = Color.FromArgb((byte)c.A, (byte)c.R, (byte)c.G, (byte)c.B);

            c._rgbSetInternally = true;
            
            c.Color = n;
            c.ColorBrush = new SolidColorBrush(n);

            c._rgbSetInternally = false;
        }

        #region ColorChanged Event

        public static readonly RoutedEvent ColorChangedEvent =
            EventManager.RegisterRoutedEvent("ColorChanged", RoutingStrategy.Bubble, typeof(EventHandler<ColorChangedEventArgs>), typeof(ColorPicker));

        public event EventHandler<ColorChangedEventArgs> ColorChanged
        { add => AddHandler(ColorChangedEvent, value); remove => RemoveHandler(ColorChangedEvent, value);
        }

        private void RaiseColorChangedEvent(Color color)
        {
            var newEventArgs = new ColorChangedEventArgs(ColorChangedEvent, color);
            RaiseEvent(newEventArgs);
        }

        #endregion

    }

    public class ColorChangedEventArgs : RoutedEventArgs
    {
        public ColorChangedEventArgs(RoutedEvent routedEvent, Color color)
        {
            RoutedEvent = routedEvent;
            Color = color;
        }

        private Color _color;
        public Color Color
        { get => _color; set => _color = value;
        }
    }


    public class ColorTextBox : TextBox
    {

        #region Properties

        /// <summary>
        /// ColorBrush Property
        /// </summary>

        public SolidColorBrush ColorBrush
        { get => (SolidColorBrush)GetValue(ColorBrushProperty); set => SetValue(ColorBrushProperty, value);
        }
        public static readonly DependencyProperty ColorBrushProperty =
            DependencyProperty.Register(nameof(ColorBrush), typeof(SolidColorBrush), typeof(ColorTextBox), new UIPropertyMetadata(Brushes.Black));

        /// <summary>
        /// Color Property
        /// </summary>
        private bool _colorSetInternally;

        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set
            {
                SetValue(ColorProperty, value);

                if (!_colorSetInternally)
                {
                    SetValue(TextProperty, value.ToString());
                }
            }
        }
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(nameof(Color), typeof(Color), typeof(ColorTextBox), new UIPropertyMetadata(Colors.Black));

        #endregion

        #region Overridden Methods

        /// <summary>
        /// Updates the Color property any time the text changes
        /// </summary>

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);

            _colorSetInternally = true;
            Color = ColorPickerUtil.ColorFromString(Text);
            ColorBrush = new SolidColorBrush(Color);
            _colorSetInternally = false;
        }

        /// <summary>
        /// Restricts input to chacters that are valid for defining a color
        /// </summary>

        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0)
            {
                var c = e.Text[0];

                var isValid = false;

                if (c is >= 'a' and <= 'f') isValid = true;
                if (c is >= 'A' and <= 'F') isValid = true;
                if (c is >= '0' and <= '9' && Keyboard.Modifiers != ModifierKeys.Shift) isValid = true;

                if (!isValid)
                {
                    e.Handled = true;
                }

                if (Text.Length >= 8)
                {
                    e.Handled = true;
                }
            }

            base.OnPreviewTextInput(e);
        }

        #endregion

    }

    public class HueChooser : FrameworkElement
    {
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var p = e.GetPosition(this);

                if (Orientation == Orientation.Vertical)
                {
                    Hue = 1 - p.Y / ActualHeight;
                }
                else
                {
                    Hue = 1 - p.X / ActualWidth;
                }
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var p = e.GetPosition(this);

                if (Orientation == Orientation.Vertical)
                {
                    Hue = 1 - p.Y / ActualHeight;
                }
                else
                {
                    Hue = 1 - p.X / ActualWidth;
                }
            }

            Mouse.Capture(this);
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();
            base.OnMouseUp(e);
        }


        public Orientation Orientation
        { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value);
        }

        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(HueChooser), new UIPropertyMetadata(Orientation.Vertical));


        public double Hue
        { get => (double)GetValue(HueProperty); set => SetValue(HueProperty, value);
        }

        public static readonly DependencyProperty HueProperty =
            DependencyProperty.Register(nameof(Hue), typeof(double), typeof(HueChooser),
            new FrameworkPropertyMetadata(0.0,
            HueChanged,
            HueCoerce));

        public static void HueChanged(object o, DependencyPropertyChangedEventArgs e)
        {
            var h = (HueChooser)o;
            h.UpdateHueOffset();
            h.UpdateColor();
        }

        public static object HueCoerce(DependencyObject d, object brightness)
        {
            var v = (double)brightness;
            if (v < 0) return 0.0;
            if (v > 1) return 1.0;
            return v;
        }

        public double HueOffset
        { get => (double)GetValue(HueOffsetProperty); private set => SetValue(HueOffsetProperty, value);
        }
        public static readonly DependencyProperty HueOffsetProperty =
            DependencyProperty.Register(nameof(HueOffset), typeof(double), typeof(HueChooser), new UIPropertyMetadata(0.0));

        private void UpdateHueOffset()
        {
            var length = ActualHeight;
            if (Orientation == Orientation.Horizontal) length = ActualWidth;

            HueOffset = length - length * Hue;
        }

        private void UpdateColor()
        {
            Color = ColorPickerUtil.ColorFromHsb(Hue, 1, 1);
            ColorBrush = new SolidColorBrush(Color);
        }

        public Color Color
        { get => (Color)GetValue(ColorProperty); private set => SetValue(ColorProperty, value);
        }
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(nameof(Color), typeof(Color), typeof(HueChooser), new UIPropertyMetadata(Colors.Red));

        public SolidColorBrush ColorBrush
        { get => (SolidColorBrush)GetValue(ColorBrushProperty); private set => SetValue(ColorBrushProperty, value);
        }
        public static readonly DependencyProperty ColorBrushProperty =
            DependencyProperty.Register(nameof(ColorBrush), typeof(SolidColorBrush), typeof(HueChooser), new UIPropertyMetadata(Brushes.Red));

        protected override void OnRender(DrawingContext dc)
        {
            var lb = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0)
            };

            if (Orientation == Orientation.Vertical)
                lb.EndPoint = new Point(0, 1);
            else
                lb.EndPoint = new Point(1, 0);

            lb.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0x00, 0x00), 1.00));
            lb.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0xFF, 0x00), 0.85));
            lb.GradientStops.Add(new GradientStop(Color.FromRgb(0x00, 0xFF, 0x00), 0.76));
            lb.GradientStops.Add(new GradientStop(Color.FromRgb(0x00, 0xFF, 0xFF), 0.50));
            lb.GradientStops.Add(new GradientStop(Color.FromRgb(0x00, 0x00, 0xFF), 0.33));
            lb.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0x00, 0xFF), 0.16));
            lb.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0x00, 0x00), 0.00));

            dc.DrawRectangle(lb, null, new Rect(0, 0, ActualWidth, ActualHeight));

            UpdateHueOffset();

        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            UpdateHueOffset();
            return base.ArrangeOverride(finalSize);
        }
    }

    public class AlphaChooser : FrameworkElement
    {
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var p = e.GetPosition(this);

                if (Orientation == Orientation.Vertical)
                {
                    Alpha = 1 - p.Y / ActualHeight;
                }
                else
                {
                    Alpha = 1 - p.X / ActualWidth;
                }
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var p = e.GetPosition(this);

                if (Orientation == Orientation.Vertical)
                {
                    Alpha = 1 - p.Y / ActualHeight;
                }
                else
                {
                    Alpha = 1 - p.X / ActualWidth;
                }
            }

            Mouse.Capture(this);
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();
            base.OnMouseUp(e);
        }

        public Orientation Orientation
        { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value);
        }

        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(AlphaChooser), new UIPropertyMetadata(Orientation.Vertical));


        public double Alpha
        { get => (double)GetValue(AlphaProperty); set => SetValue(AlphaProperty, value);
        }

        public static readonly DependencyProperty AlphaProperty =
            DependencyProperty.Register(nameof(Alpha), typeof(double), typeof(AlphaChooser),
            new FrameworkPropertyMetadata(1.0,
            AlphaChanged,
            AlphaCoerce));

        public static void AlphaChanged(object o, DependencyPropertyChangedEventArgs e)
        {
            var h = (AlphaChooser)o;
            h.UpdateAlphaOffset();
            h.UpdateColor();
        }

        public static object AlphaCoerce(DependencyObject d, object brightness)
        {
            var v = (double)brightness;
            if (v < 0) return 0.0;
            if (v > 1) return 1.0;
            return v;
        }

        public double AlphaOffset
        { get => (double)GetValue(AlphaOffsetProperty); private set => SetValue(AlphaOffsetProperty, value);
        }
        public static readonly DependencyProperty AlphaOffsetProperty =
            DependencyProperty.Register(nameof(AlphaOffset), typeof(double), typeof(AlphaChooser), new UIPropertyMetadata(0.0));

        private void UpdateAlphaOffset()
        {
            var length = ActualHeight;
            if (Orientation == Orientation.Horizontal) length = ActualWidth;

            AlphaOffset = length - length * Alpha;
        }

        private void UpdateColor()
        {
            Color = Color.FromArgb((byte)Math.Round(Alpha * 255), 0, 0, 0);
            ColorBrush = new SolidColorBrush(Color);
        }

        public Color Color
        { get => (Color)GetValue(ColorProperty); private set => SetValue(ColorProperty, value);
        }
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(nameof(Color), typeof(Color), typeof(AlphaChooser), new UIPropertyMetadata(Colors.Red));

        public SolidColorBrush ColorBrush
        { get => (SolidColorBrush)GetValue(ColorBrushProperty); private set => SetValue(ColorBrushProperty, value);
        }
        public static readonly DependencyProperty ColorBrushProperty =
            DependencyProperty.Register(nameof(ColorBrush), typeof(SolidColorBrush), typeof(AlphaChooser), new UIPropertyMetadata(Brushes.Red));

        protected override void OnRender(DrawingContext dc)
        {
            var lb = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0)
            };

            if (Orientation == Orientation.Vertical)
                lb.EndPoint = new Point(0, 1);
            else
                lb.EndPoint = new Point(1, 0);

            lb.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x00, 0x00, 0x00), 0.00));
            lb.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0x00, 0x00, 0x00), 1.00));

            dc.DrawRectangle(lb, null, new Rect(0, 0, ActualWidth, ActualHeight));

            UpdateAlphaOffset();

        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            UpdateAlphaOffset();
            return base.ArrangeOverride(finalSize);
        }

    }

    public class SaturationBrightnessChooser : FrameworkElement
    {
        public Thickness OffsetPadding
        { get => (Thickness)GetValue(OffsetPaddingProperty); set => SetValue(OffsetPaddingProperty, value);
        }
        public static readonly DependencyProperty OffsetPaddingProperty =
            DependencyProperty.Register(nameof(OffsetPadding), typeof(Thickness), typeof(SaturationBrightnessChooser), new UIPropertyMetadata(new Thickness(0.0)));

        public double Hue
        { private get => (double)GetValue(HueProperty); set => SetValue(HueProperty, value);
        }
        public static readonly DependencyProperty HueProperty =
            DependencyProperty.Register(nameof(Hue), typeof(double), typeof(SaturationBrightnessChooser), new
            FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, HueChanged));


        public static void HueChanged(object o, DependencyPropertyChangedEventArgs e)
        {
            var h = (SaturationBrightnessChooser)o;
            h.UpdateColor();
        }

        public double SaturationOffset
        { get => (double)GetValue(SaturationOffsetProperty); set => SetValue(SaturationOffsetProperty, value);
        }
        public static readonly DependencyProperty SaturationOffsetProperty =
            DependencyProperty.Register(nameof(SaturationOffset), typeof(double), typeof(SaturationBrightnessChooser), new UIPropertyMetadata(0.0));

        public double Saturation
        { get => (double)GetValue(SaturationProperty); set => SetValue(SaturationProperty, value);
        }

        public static readonly DependencyProperty SaturationProperty =
            DependencyProperty.Register(nameof(Saturation), typeof(double), typeof(SaturationBrightnessChooser),
            new FrameworkPropertyMetadata(0.0,
            SaturationChanged,
            SaturationCoerce));

        public static void SaturationChanged(object o, DependencyPropertyChangedEventArgs e)
        {
            var h = (SaturationBrightnessChooser)o;
            h.UpdateSaturationOffset();
        }

        public static object SaturationCoerce(DependencyObject d, object brightness)
        {
            var v = (double)brightness;
            if (v < 0) return 0.0;
            if (v > 1) return 1.0;
            return v;
        }




        public Color Color
        { get => (Color)GetValue(ColorProperty); private set => SetValue(ColorProperty, value);
        }
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(nameof(Color), typeof(Color), typeof(SaturationBrightnessChooser), new UIPropertyMetadata(Colors.Red));

        public SolidColorBrush ColorBrush
        { get => (SolidColorBrush)GetValue(ColorBrushProperty); private set => SetValue(ColorBrushProperty, value);
        }
        public static readonly DependencyProperty ColorBrushProperty =
            DependencyProperty.Register(nameof(ColorBrush), typeof(SolidColorBrush), typeof(SaturationBrightnessChooser), new UIPropertyMetadata(Brushes.Red));


        public double BrightnessOffset
        { get => (double)GetValue(BrightnessOffsetProperty); set => SetValue(BrightnessOffsetProperty, value);
        }
        public static readonly DependencyProperty BrightnessOffsetProperty =
            DependencyProperty.Register(nameof(BrightnessOffset), typeof(double), typeof(SaturationBrightnessChooser), new UIPropertyMetadata(0.0));

        public double Brightness
        { get => (double)GetValue(BrightnessProperty); set => SetValue(BrightnessProperty, value);
        }

        public static readonly DependencyProperty BrightnessProperty =
            DependencyProperty.Register(nameof(Brightness), typeof(double), typeof(SaturationBrightnessChooser),
            new FrameworkPropertyMetadata(0.0,
            BrightnessChanged,
            BrightnessCoerce));

        public static void BrightnessChanged(object o, DependencyPropertyChangedEventArgs e)
        {
            var h = (SaturationBrightnessChooser)o;
            h.UpdateBrightnessOffset();
        }

        public static object BrightnessCoerce(DependencyObject d, object brightness)
        {
            var v = (double)brightness;
            if (v < 0) return 0.0;
            if (v > 1) return 1.0;
            return v;
        }

        private void UpdateSaturationOffset()
        {
            SaturationOffset = OffsetPadding.Left + (ActualWidth - (OffsetPadding.Right + OffsetPadding.Left)) * Saturation;
        }

        private void UpdateBrightnessOffset()
        {
            BrightnessOffset = OffsetPadding.Top + (ActualHeight - (OffsetPadding.Bottom + OffsetPadding.Top) - (ActualHeight - (OffsetPadding.Bottom + OffsetPadding.Top)) * Brightness);
        }

        protected override void OnRender(DrawingContext dc)
        {
            var h = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0)
            };
            h.GradientStops.Add(new GradientStop(Colors.White, 0.00));
            h.GradientStops.Add(new GradientStop(ColorPickerUtil.ColorFromHsb(Hue, 1, 1), 1.0));
            dc.DrawRectangle(h, null, new Rect(0, 0, ActualWidth, ActualHeight));

            var v = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };
            v.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0, 0, 0), 1.00));
            v.GradientStops.Add(new GradientStop(Color.FromArgb(0x80, 0, 0, 0), 0.50));
            v.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0, 0, 0), 0.00));
            dc.DrawRectangle(v, null, new Rect(0, 0, ActualWidth, ActualHeight));

            UpdateSaturationOffset();
            UpdateBrightnessOffset();
        }

        public void UpdateColor()
        {
            Color = ColorPickerUtil.ColorFromHsb(Hue, Saturation, Brightness);
            ColorBrush = new SolidColorBrush(Color);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var p = e.GetPosition(this);
                Saturation = p.X / (ActualWidth - OffsetPadding.Right);
                Brightness = (ActualHeight - OffsetPadding.Bottom - p.Y) / (ActualHeight - OffsetPadding.Bottom);
                UpdateColor();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            var p = e.GetPosition(this);
            Saturation = p.X / (ActualWidth - OffsetPadding.Right);
            Brightness = (ActualHeight - OffsetPadding.Bottom - p.Y) / (ActualHeight - OffsetPadding.Bottom);
            UpdateColor();

            Mouse.Capture(this);
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();
            base.OnMouseUp(e);
        }
    }



    public static class ColorPickerUtil
    {

        public static string MakeValidColorString(string input)
        {
            var s = input;

            // remove invalid characters (this is a very forgiving function)
            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];

                if (!(c is >= 'a' and <= 'f') &&
                    !(c is >= 'A' and <= 'F') &&
                    !(c is >= '0' and <= '9'))
                {
                    s = s.Remove(i, 1);
                    i--;
                }
            }

            // trim if too long
            if (s.Length > 8) s = s.Substring(0, 8);

            // pad with zeroes until a valid length is found
            while (s.Length <= 8 && s.Length != 3 && s.Length != 4 && s.Length != 6 && s.Length != 8)
            {
                s = s + "0";
            }

            return s;
        }

        public static Color ColorFromString(string s)
        {
            //ColorConverter converter = new ColorConverter();
            var c = (Color) ColorConverter.ConvertFromString(s);

            return c;
            /*
            string s = MakeValidColorString(S);

            byte A = 255;
            byte R = 0;
            byte G = 0;
            byte B = 0;

            // interpret 3 characters as RRGGBB (where R, G, and B are each repeated)
            if (s.Length == 3)
            {
                R = byte.Parse(s.Substring(0, 1) + s.Substring(0, 1), System.Globalization.NumberStyles.HexNumber);
                G = byte.Parse(s.Substring(1, 1) + s.Substring(1, 1), System.Globalization.NumberStyles.HexNumber);
                B = byte.Parse(s.Substring(2, 1) + s.Substring(2, 1), System.Globalization.NumberStyles.HexNumber);
            }

            // interpret 4 characters as AARRGGBB (where A, R, G, and B are each repeated)
            if (s.Length == 4)
            {
                A = byte.Parse(s.Substring(0, 1) + s.Substring(0, 1), System.Globalization.NumberStyles.HexNumber);
                R = byte.Parse(s.Substring(1, 1) + s.Substring(1, 1), System.Globalization.NumberStyles.HexNumber);
                G = byte.Parse(s.Substring(2, 1) + s.Substring(2, 1), System.Globalization.NumberStyles.HexNumber);
                B = byte.Parse(s.Substring(3, 1) + s.Substring(3, 1), System.Globalization.NumberStyles.HexNumber);
            }

            // interpret 6 characters as RRGGBB
            if (s.Length == 6)
            {
                R = byte.Parse(s.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                G = byte.Parse(s.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
                B = byte.Parse(s.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            }

            // interpret 8 characters as AARRGGBB
            if (s.Length == 8)
            {
                A = byte.Parse(s.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                R = byte.Parse(s.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
                G = byte.Parse(s.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                B = byte.Parse(s.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
            }

            return Color.FromArgb(A, R, G, B);
             */ 
        }

        private static readonly char[] _hexDigits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        public static string StringFromColor(Color c)
        {
            var bytes = new byte[4];
            bytes[0] = c.A;
            bytes[1] = c.R;
            bytes[2] = c.G;
            bytes[3] = c.B;

            var chars = new char[bytes.Length * 2];

            for (var i = 0; i < bytes.Length; i++)
            {
                int b = bytes[i];
                chars[i * 2] = _hexDigits[b >> 4];
                chars[i * 2 + 1] = _hexDigits[b & 0xF];
            }

            return new string(chars);
        }

        public static void HsbFromColor(Color c, ref double h, ref double s, ref double b)
        {
            // standard algorithm from nearly any graphics textbook

            var red = c.R;
            var green = c.G;
            var blue = c.B;

            int imax = red, imin = red;

            if (green > imax) imax = green; else if (green < imin) imin = green;
            if (blue > imax) imax = blue; else if (blue < imin) imin = blue;
            double max = imax / 255.0, min = imin / 255.0;

            var value = max;
            var saturation = max > 0 ? (max - min) / max : 0.0;
            double hue = 0;

            if (imax > imin)
            {
                var f = 1.0 / ((max - min) * 255.0);
                hue = imax == red ? 0.0 + f * (green - blue)
                    : imax == green ? 2.0 + f * (blue - red)
                    : 4.0 + f * (red - green);
                hue = hue * 60.0;
                if (hue < 0.0)
                    hue += 360.0;
            }

            h = hue / 360;
            s = saturation;
            b = value;
        }

        public static Color ColorFromAhsb(double a, double h, double s, double b)
        {
            var r = ColorFromHsb(h, s, b);
            r.A = (byte)Math.Round(a * 255);
            return r;
        }

        public static Color ColorFromHsb(double H, double S, double b)
        {
            // standard algorithm from nearly any graphics textbook

            double red = 0.0, green = 0.0, blue = 0.0;

            if (S == 0.0)
            {
                red = green = blue = b;
            }
            else
            {
                var h = H * 360;
                while (h >= 360.0)
                    h -= 360.0;

                h = h / 60.0;
                var i = (int)h;

                var f = h - i;
                var r = b * (1.0 - S);
                var s = b * (1.0 - S * f);
                var t = b * (1.0 - S * (1.0 - f));

                switch (i)
                {
                    case 0: red = b; green = t; blue = r; break;
                    case 1: red = s; green = b; blue = r; break;
                    case 2: red = r; green = b; blue = t; break;
                    case 3: red = r; green = s; blue = b; break;
                    case 4: red = t; green = r; blue = b; break;
                    case 5: red = b; green = r; blue = s; break;
                }
            }

            byte iRed = (byte)(red * 255.0), iGreen = (byte)(green * 255.0), iBlue = (byte)(blue * 255.0);
            return Color.FromRgb(iRed, iGreen, iBlue);
        }
    }

}
