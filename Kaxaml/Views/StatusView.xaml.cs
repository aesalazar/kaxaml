using System.Windows;

namespace Kaxaml.Views;

public partial class StatusView
{
    public StatusView()
    {
        InitializeComponent();
    }

    #region CurrentLineNumber (DependencyProperty)

    public int CurrentLineNumber
    {
        get => (int)GetValue(CurrentLineNumberProperty);
        set => SetValue(CurrentLineNumberProperty, value);
    }

    public static readonly DependencyProperty CurrentLineNumberProperty =
        DependencyProperty.Register(nameof(CurrentLineNumber), typeof(int), typeof(StatusView), new FrameworkPropertyMetadata(1));

    #endregion

    #region CurrentLinePosition (DependencyProperty)

    public int CurrentLinePosition
    {
        get => (int)GetValue(CurrentLinePositionProperty);
        set => SetValue(CurrentLinePositionProperty, value);
    }

    public static readonly DependencyProperty CurrentLinePositionProperty =
        DependencyProperty.Register(nameof(CurrentLinePosition), typeof(int), typeof(StatusView), new FrameworkPropertyMetadata(1));

    #endregion


    #region Zoom (DependencyProperty)

    /// <summary>
    /// A description of the property.
    /// </summary>
    public int Zoom
    {
        get => (int)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public static readonly DependencyProperty ZoomProperty =
        DependencyProperty.Register(nameof(Zoom), typeof(int), typeof(StatusView), new FrameworkPropertyMetadata(100, ZoomChanged));

    private static void ZoomChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
    {
        if (obj is StatusView owner) owner.Scale = double.Parse(args.NewValue.ToString() ?? string.Empty) / 100.0;
    }

    #endregion

    #region Scale (DependencyProperty)

    public double Scale
    {
        get => (double)GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public static readonly DependencyProperty ScaleProperty =
        DependencyProperty.Register(nameof(Scale), typeof(double), typeof(StatusView), new FrameworkPropertyMetadata(1.0, ScaleChanged));

    private static void ScaleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
    {
        if (obj is StatusView owner) owner.Zoom = (int)((double)args.NewValue * 100.0);
    }

    #endregion


    #region Public Methods

    public void ZoomIn()
    {
        // find the index closest to the current zoom

        var index = -1;

        for (var i = 0; i < ZoomSlider.Ticks.Count; i++)
        {
            var v = ZoomSlider.Ticks[i];

            if (v <= ZoomSlider.Value) index = ZoomSlider.Ticks.IndexOf(v);
        }

        if (index >= 0 && index < ZoomSlider.Ticks.Count - 1) ZoomSlider.Value = ZoomSlider.Ticks[index + 1];
    }

    public void ZoomOut()
    {
        // find the index closest to the current zoom

        var index = -1;

        for (var i = ZoomSlider.Ticks.Count - 1; i > 0; i--)
        {
            var v = ZoomSlider.Ticks[i];

            if (v >= ZoomSlider.Value) index = ZoomSlider.Ticks.IndexOf(v);
        }

        if (index > 0 && index <= ZoomSlider.Ticks.Count - 1) ZoomSlider.Value = ZoomSlider.Ticks[index - 1];
    }

    public void ActualSize()
    {
        ZoomSlider.Value = 100;
    }

    #endregion
}