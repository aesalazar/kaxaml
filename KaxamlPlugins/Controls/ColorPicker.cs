using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using KaxamlPlugins.DependencyInjection;
using KaxamlPlugins.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KaxamlPlugins.Controls;

public class ColorPicker : Control
{
    public static readonly DependencyProperty ColorBrushProperty = DependencyProperty.Register(
        nameof(ColorBrush),
        typeof(SolidColorBrush),
        typeof(ColorPicker),
        new UIPropertyMetadata(Brushes.Black));

    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
        nameof(Color),
        typeof(Color),
        typeof(ColorPicker),
        new UIPropertyMetadata(Colors.Black, OnColorChanged));

    public static readonly DependencyProperty HueProperty = DependencyProperty.Register(
        nameof(Hue),
        typeof(double),
        typeof(ColorPicker),
        new FrameworkPropertyMetadata(0.0, UpdateColorHsb, HueCoerce));

    public static readonly DependencyProperty BrightnessProperty = DependencyProperty.Register(
        nameof(Brightness),
        typeof(double),
        typeof(ColorPicker),
        new FrameworkPropertyMetadata(0.0, UpdateColorHsb, BrightnessCoerce));

    public static readonly DependencyProperty SaturationProperty = DependencyProperty.Register(
        nameof(Saturation),
        typeof(double),
        typeof(ColorPicker),
        new FrameworkPropertyMetadata(0.0, UpdateColorHsb, SaturationCoerce));

    public static readonly DependencyProperty AlphaProperty = DependencyProperty.Register(
        nameof(Alpha),
        typeof(double),
        typeof(ColorPicker),
        new FrameworkPropertyMetadata(1.0, UpdateColorHsb, AlphaCoerce));

    public static readonly DependencyProperty RProperty = DependencyProperty.Register(
        nameof(R),
        typeof(int),
        typeof(ColorPicker),
        new FrameworkPropertyMetadata(default(int), UpdateColorRgb, RgbCoerce));

    public static readonly DependencyProperty GProperty = DependencyProperty.Register(
        nameof(G),
        typeof(int),
        typeof(ColorPicker),
        new FrameworkPropertyMetadata(default(int), UpdateColorRgb, RgbCoerce));

    public static readonly DependencyProperty BProperty = DependencyProperty.Register(
        nameof(B),
        typeof(int),
        typeof(ColorPicker),
        new FrameworkPropertyMetadata(default(int), UpdateColorRgb, RgbCoerce));

    public static readonly DependencyProperty AProperty = DependencyProperty.Register(
        nameof(A),
        typeof(int),
        typeof(ColorPicker),
        new FrameworkPropertyMetadata(255, UpdateColorRgb, RgbCoerce));

    private readonly ILogger<ColorPicker> _logger;

    /// <summary>
    /// Color Property
    /// </summary>
    private bool _hsbSetInternally;

    private bool _rgbSetInternally;

    static ColorPicker()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorPicker), new FrameworkPropertyMetadata(typeof(ColorPicker)));
    }

    public ColorPicker()
    {
        _logger = ApplicationDiServiceProvider.Services.GetRequiredService<ILogger<ColorPicker>>();
        _logger.LogInformation("Initialized Color Picker complete.");
    }

    /// <summary>
    /// ColorBrush Property
    /// </summary>
    public SolidColorBrush ColorBrush
    {
        get => (SolidColorBrush)GetValue(ColorBrushProperty);
        set => SetValue(ColorBrushProperty, value);
    }

    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <summary>
    /// Hue Property
    /// </summary>
    public double Hue
    {
        get => (double)GetValue(HueProperty);
        set => SetValue(HueProperty, value);
    }

    /// <summary>
    /// Brightness Property
    /// </summary>
    public double Brightness
    {
        get => (double)GetValue(BrightnessProperty);
        set => SetValue(BrightnessProperty, value);
    }

    /// <summary>
    /// Saturation Property
    /// </summary>

    public double Saturation
    {
        get => (double)GetValue(SaturationProperty);
        set => SetValue(SaturationProperty, value);
    }

    /// <summary>
    /// Alpha Property
    /// </summary>
    public double Alpha
    {
        get => (double)GetValue(AlphaProperty);
        set => SetValue(AlphaProperty, value);
    }

    /// <summary>
    /// R Property
    /// </summary>

    public int R
    {
        get => (int)GetValue(RProperty);
        set => SetValue(RProperty, value);
    }

    /// <summary>
    /// G Property
    /// </summary>
    public int G
    {
        get => (int)GetValue(GProperty);
        set => SetValue(GProperty, value);
    }

    /// <summary>
    /// B Property
    /// </summary>
    public int B
    {
        get => (int)GetValue(BProperty);
        set => SetValue(BProperty, value);
    }

    /// <summary>
    /// A Property
    /// </summary>
    public int A
    {
        get => (int)GetValue(AProperty);
        set => SetValue(AProperty, value);
    }

    public static void OnColorChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        var c = (ColorPicker)o;

        if (e.NewValue is not Color color) return;
        if (!c._hsbSetInternally)
        {
            // update HSB value based on new value of color

            double h = 0;
            double s = 0;
            double b = 0;
            ColorPickerUtil.HsbFromColor(color, ref h, ref s, ref b);

            c._hsbSetInternally = true;

            c.Alpha = color.A / 255d;
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

    public static object HueCoerce(DependencyObject d, object hue)
    {
        var v = (double)hue;
        return v switch
        {
            < 0 => 0.0,
            > 1 => 1.0,
            _ => v
        };
    }

    public static object BrightnessCoerce(DependencyObject d, object brightness)
    {
        var v = (double)brightness;
        return v switch
        {
            < 0 => 0.0,
            > 1 => 1.0,
            _ => v
        };
    }

    public static object SaturationCoerce(DependencyObject d, object saturation)
    {
        var v = (double)saturation;
        return v switch
        {
            < 0 => 0.0,
            > 1 => 1.0,
            _ => v
        };
    }

    public static object AlphaCoerce(DependencyObject d, object alpha)
    {
        var v = (double)alpha;
        return v switch
        {
            < 0 => 0.0,
            > 1 => 1.0,
            _ => v
        };
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
        c._logger.LogDebug("Updated color to: {Color}", c.Color);

        c._hsbSetInternally = false;
    }

    public static object RgbCoerce(DependencyObject d, object value)
    {
        var v = (int)value;
        return v switch
        {
            < 0 => 0,
            > 255 => 255,
            _ => v
        };
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
        c._logger.LogDebug("Updated color to: {Color}", c.Color);

        c._rgbSetInternally = false;
    }

    #region ColorChanged Event

    public static readonly RoutedEvent ColorChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ColorChanged),
        RoutingStrategy.Bubble,
        typeof(EventHandler<ColorChangedEventArgs>),
        typeof(ColorPicker));

    public event EventHandler<ColorChangedEventArgs> ColorChanged
    {
        add => AddHandler(ColorChangedEvent, value);
        remove => RemoveHandler(ColorChangedEvent, value);
    }

    private void RaiseColorChangedEvent(Color color)
    {
        var newEventArgs = new ColorChangedEventArgs(ColorChangedEvent, color);
        RaiseEvent(newEventArgs);
    }

    #endregion
}