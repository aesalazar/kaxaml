using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Kaxaml.Controls;

public class ZoomFrame : Frame
{
    #region Private Fields

    private Thumb? _partThumb;

    #endregion

    static ZoomFrame()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ZoomFrame), new FrameworkPropertyMetadata(typeof(ZoomFrame)));
    }

    #region IsDraggable (DependencyProperty)

    public bool IsDraggable
    {
        get => (bool)GetValue(IsDraggableProperty);
        set => SetValue(IsDraggableProperty, value);
    }

    public static readonly DependencyProperty IsDraggableProperty =
        DependencyProperty.Register(nameof(IsDraggable), typeof(bool), typeof(ZoomFrame), new FrameworkPropertyMetadata(default(bool)));

    #endregion

    #region IsDragUIVisible (DependencyProperty)

    public bool IsDragUiVisible
    {
        get => (bool)GetValue(IsDragUiVisibleProperty);
        set => SetValue(IsDragUiVisibleProperty, value);
    }

    public static readonly DependencyProperty IsDragUiVisibleProperty =
        DependencyProperty.Register(nameof(IsDragUiVisible), typeof(bool), typeof(ZoomFrame), new FrameworkPropertyMetadata(false));

    #endregion


    #region Scale (DependencyProperty)

    public double Scale
    {
        get => (double)GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public static readonly DependencyProperty ScaleProperty =
        DependencyProperty.Register(nameof(Scale), typeof(double), typeof(ZoomFrame), new FrameworkPropertyMetadata(1.0, ScaleChanged));

    private static void ScaleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
    {
        if (obj is ZoomFrame owner)
        {
            if (owner.Scale > 1.001)
            {
                owner.IsDragUiVisible = true;
            }
            else
            {
                owner.IsDragUiVisible = false;
                owner.IsDraggable = false;
                //owner.ScaleOrigin = new Point(0.5, 0.5);
            }
        }
    }

    #endregion


    #region ScaleOrigin (DependencyProperty)

    public Point ScaleOrigin
    {
        get => (Point)GetValue(ScaleOriginProperty);
        set => SetValue(ScaleOriginProperty, value);
    }

    public static readonly DependencyProperty ScaleOriginProperty =
        DependencyProperty.Register(nameof(ScaleOrigin), typeof(Point), typeof(ZoomFrame), new FrameworkPropertyMetadata(new Point(0.5, 0.5)));

    #endregion

    #region overrides

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (e.IsDown && Keyboard.Modifiers == ModifierKeys.Alt)
            if (Scale > 1)
                IsDraggable = true;
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        IsDraggable = false;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _partThumb = Template.FindName("PART_Thumb", this) as Thumb;

        if (_partThumb != null)
        {
            _partThumb.DragStarted += PART_Thumb_DragStarted;
            _partThumb.DragDelta += PART_Thumb_DragDelta;
            _partThumb.DragCompleted += PART_Thumb_DragCompleted;
        }
    }

    private bool _isDragging;

    private void PART_Thumb_DragStarted(object sender, DragStartedEventArgs e)
    {
        _isDragging = true;
    }

    private void PART_Thumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (_isDragging)
            if (Scale > 1.0)
            {
                var x = ScaleOrigin.X - e.HorizontalChange / (ActualWidth * Scale);
                if (x > 1) x = 1;
                if (x < 0) x = 0;

                var y = ScaleOrigin.Y - e.VerticalChange / (ActualHeight * Scale);
                if (y > 1) y = 1;
                if (y < 0) y = 0;

                ScaleOrigin = new Point(x, y);
            }
    }

    private void PART_Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        _isDragging = false;
    }

    #endregion
}