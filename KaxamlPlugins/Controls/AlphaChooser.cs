using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace KaxamlPlugins.Controls;

public class AlphaChooser : FrameworkElement
{
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation),
        typeof(Orientation),
        typeof(AlphaChooser),
        new UIPropertyMetadata(Orientation.Vertical));

    public static readonly DependencyProperty AlphaProperty = DependencyProperty.Register(
        nameof(Alpha),
        typeof(double),
        typeof(AlphaChooser),
        new FrameworkPropertyMetadata(1.0, AlphaChanged, AlphaCoerce));

    public static readonly DependencyProperty AlphaOffsetProperty = DependencyProperty.Register(
        nameof(AlphaOffset),
        typeof(double),
        typeof(AlphaChooser),
        new UIPropertyMetadata(0.0));

    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
        nameof(Color),
        typeof(Color),
        typeof(AlphaChooser),
        new UIPropertyMetadata(Colors.Red));

    public static readonly DependencyProperty ColorBrushProperty = DependencyProperty.Register(
        nameof(ColorBrush),
        typeof(SolidColorBrush),
        typeof(AlphaChooser),
        new UIPropertyMetadata(Brushes.Red));

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public double Alpha
    {
        get => (double)GetValue(AlphaProperty);
        set => SetValue(AlphaProperty, value);
    }

    public double AlphaOffset
    {
        get => (double)GetValue(AlphaOffsetProperty);
        private set => SetValue(AlphaOffsetProperty, value);
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
                Alpha = 1 - p.Y / ActualHeight;
            else
                Alpha = 1 - p.X / ActualWidth;
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            var p = e.GetPosition(this);

            if (Orientation == Orientation.Vertical)
                Alpha = 1 - p.Y / ActualHeight;
            else
                Alpha = 1 - p.X / ActualWidth;
        }

        Mouse.Capture(this);
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        ReleaseMouseCapture();
        base.OnMouseUp(e);
    }

    public static void AlphaChanged(object o, DependencyPropertyChangedEventArgs e)
    {
        var h = (AlphaChooser)o;
        h.UpdateAlphaOffset();
        h.UpdateColor();
    }

    public static object AlphaCoerce(DependencyObject d, object brightness)
    {
        var v = (double)brightness;
        return v switch
        {
            < 0 => 0.0,
            > 1 => 1.0,
            _ => v
        };
    }

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

    protected override void OnRender(DrawingContext dc)
    {
        var lb = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0)
        };

        lb.EndPoint = Orientation == Orientation.Vertical ? new Point(0, 1) : new Point(1, 0);
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