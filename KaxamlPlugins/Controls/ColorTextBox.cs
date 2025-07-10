using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace KaxamlPlugins.Controls;

public class ColorTextBox : TextBox
{
    #region Properties

    /// <summary>
    /// ColorBrush Property
    /// </summary>
    public SolidColorBrush ColorBrush
    {
        get => (SolidColorBrush)GetValue(ColorBrushProperty);
        set => SetValue(ColorBrushProperty, value);
    }

    public static readonly DependencyProperty ColorBrushProperty = DependencyProperty.Register(
        nameof(ColorBrush),
        typeof(SolidColorBrush),
        typeof(ColorTextBox),
        new UIPropertyMetadata(Brushes.Black));

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

            if (!_colorSetInternally) SetValue(TextProperty, value.ToString());
        }
    }

    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
        nameof(Color),
        typeof(Color),
        typeof(ColorTextBox),
        new UIPropertyMetadata(Colors.Black));

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
    /// Restricts input to characters that are valid for defining a color
    /// </summary>
    protected override void OnPreviewTextInput(TextCompositionEventArgs e)
    {
        if (e.Text.Length > 0)
        {
            var c = e.Text[0];

            var isValid = c is >= 'a' and <= 'f'
                          || c is >= 'A' and <= 'F'
                          || (c is >= '0' and <= '9'
                              && Keyboard.Modifiers != ModifierKeys.Shift);

            if (!isValid) e.Handled = true;

            if (Text.Length >= 8) e.Handled = true;
        }

        base.OnPreviewTextInput(e);
    }

    #endregion
}