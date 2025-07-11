using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace KaxamlPlugins.Controls;

public class SaturationBrightnessChooser : FrameworkElement
{
    public static readonly DependencyProperty OffsetPaddingProperty = DependencyProperty.Register(
        nameof(OffsetPadding),
        typeof(Thickness),
        typeof(SaturationBrightnessChooser),
        new UIPropertyMetadata(new Thickness(0.0)));

    public static readonly DependencyProperty HueProperty = DependencyProperty.Register(
        nameof(Hue),
        typeof(double),
        typeof(SaturationBrightnessChooser),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, HueChanged));

    public static readonly DependencyProperty SaturationOffsetProperty = DependencyProperty.Register(
        nameof(SaturationOffset),
        typeof(double),
        typeof(SaturationBrightnessChooser),
        new UIPropertyMetadata(0.0));

    public static readonly DependencyProperty SaturationProperty = DependencyProperty.Register(
        nameof(Saturation),
        typeof(double),
        typeof(SaturationBrightnessChooser),
        new FrameworkPropertyMetadata(0.0, SaturationChanged, SaturationCoerce));

    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
        nameof(Color),
        typeof(Color),
        typeof(SaturationBrightnessChooser),
        new UIPropertyMetadata(Colors.Red));

    public static readonly DependencyProperty ColorBrushProperty = DependencyProperty.Register(
        nameof(ColorBrush),
        typeof(SolidColorBrush),
        typeof(SaturationBrightnessChooser),
        new UIPropertyMetadata(Brushes.Red));

    public static readonly DependencyProperty BrightnessOffsetProperty = DependencyProperty.Register(
        nameof(BrightnessOffset),
        typeof(double),
        typeof(SaturationBrightnessChooser),
        new UIPropertyMetadata(0.0));

    public static readonly DependencyProperty BrightnessProperty = DependencyProperty.Register(
        nameof(Brightness),
        typeof(double),
        typeof(SaturationBrightnessChooser),
        new FrameworkPropertyMetadata(0.0, BrightnessChanged, BrightnessCoerce));

    public Thickness OffsetPadding
    {
        get => (Thickness)GetValue(OffsetPaddingProperty);
        set => SetValue(OffsetPaddingProperty, value);
    }

    public double Hue
    {
        private get => (double)GetValue(HueProperty);
        set => SetValue(HueProperty, value);
    }

    public double SaturationOffset
    {
        get => (double)GetValue(SaturationOffsetProperty);
        set => SetValue(SaturationOffsetProperty, value);
    }

    public double Saturation
    {
        get => (double)GetValue(SaturationProperty);
        set => SetValue(SaturationProperty, value);
    }

    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        private set => SetValue(ColorProperty, value);
    }

    public SolidColorBrush ColorBrush
    {
        get => (SolidColorBrush)GetValue(ColorBrushProperty);
        private set => SetValue(ColorBrushProperty, value);
    }

    public double BrightnessOffset
    {
        get => (double)GetValue(BrightnessOffsetProperty);
        set => SetValue(BrightnessOffsetProperty, value);
    }

    public double Brightness
    {
        get => (double)GetValue(BrightnessProperty);
        set => SetValue(BrightnessProperty, value);
    }

    public static void HueChanged(object o, DependencyPropertyChangedEventArgs e)
    {
        var h = (SaturationBrightnessChooser)o;
        h.UpdateColor();
    }

    public static void SaturationChanged(object o, DependencyPropertyChangedEventArgs e)
    {
        var h = (SaturationBrightnessChooser)o;
        h.UpdateSaturationOffset();
    }

    public static object SaturationCoerce(DependencyObject d, object brightness)
    {
        var v = (double)brightness;
        return v switch
        {
            < 0 => 0.0,
            > 1 => 1.0,
            _ => v
        };
    }

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