using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace KaxamlPlugins.Controls;

public class HueChooser : FrameworkElement
{
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation),
        typeof(Orientation),
        typeof(HueChooser),
        new UIPropertyMetadata(Orientation.Vertical));

    public static readonly DependencyProperty HueProperty = DependencyProperty.Register(
        nameof(Hue),
        typeof(double),
        typeof(HueChooser),
        new FrameworkPropertyMetadata(0.0, HueChanged, HueCoerce));

    public static readonly DependencyProperty HueOffsetProperty = DependencyProperty.Register(
        nameof(HueOffset),
        typeof(double),
        typeof(HueChooser),
        new UIPropertyMetadata(0.0));

    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
        nameof(Color),
        typeof(Color),
        typeof(HueChooser),
        new UIPropertyMetadata(Colors.Red));

    public static readonly DependencyProperty ColorBrushProperty = DependencyProperty.Register(
        nameof(ColorBrush),
        typeof(SolidColorBrush),
        typeof(HueChooser),
        new UIPropertyMetadata(Brushes.Red));


    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public double Hue
    {
        get => (double)GetValue(HueProperty);
        set => SetValue(HueProperty, value);
    }

    public double HueOffset
    {
        get => (double)GetValue(HueOffsetProperty);
        private set => SetValue(HueOffsetProperty, value);
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

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            var p = e.GetPosition(this);

            if (Orientation == Orientation.Vertical)
                Hue = 1 - p.Y / ActualHeight;
            else
                Hue = 1 - p.X / ActualWidth;
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            var p = e.GetPosition(this);

            if (Orientation == Orientation.Vertical)
                Hue = 1 - p.Y / ActualHeight;
            else
                Hue = 1 - p.X / ActualWidth;
        }

        Mouse.Capture(this);
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        ReleaseMouseCapture();
        base.OnMouseUp(e);
    }

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