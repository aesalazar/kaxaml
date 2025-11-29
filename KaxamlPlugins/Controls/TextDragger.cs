using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using KaxamlPlugins.Utilities;

namespace KaxamlPlugins.Controls;

public class TextDragger : Decorator
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(TextDragger),
        new UIPropertyMetadata(string.Empty));

    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data),
        typeof(object),
        typeof(TextDragger),
        new UIPropertyMetadata(null));

    private bool _isDragging;

    static TextDragger()
    {
        CursorProperty.OverrideMetadata(typeof(TextDragger), new FrameworkPropertyMetadata(Cursors.Hand));
    }

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public object? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        if (e.ClickCount is 2 && Text is not null)
        {
            KaxamlInfo.Editor?.InsertStringAtCaret(Text);
        }

        base.OnMouseDown(e);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        // need this to ensure hit-testing
        drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));
        base.OnRender(drawingContext);
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
            StartDrag();
        else if (e.LeftButton == MouseButtonState.Released)
            _isDragging = false;

        base.OnPreviewMouseMove(e);
    }

    private void StartDrag()
    {
        var obj = new DataObject(DataFormats.Text, Text ?? string.Empty);
        if (Data != null) obj.SetData(Data.GetType(), Data);

        try
        {
            DragDrop.DoDragDrop(this, obj, DragDropEffects.Copy);
        }
        catch (Exception ex)
        {
            if (ex.IsCriticalException()) throw;
        }
    }
}