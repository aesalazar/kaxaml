using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Kaxaml.CodeSnippets;

public sealed class TextBoxOverlay : TextBox
{
    #region Events

    public event EventHandler<TextBoxOverlayHideEventArgs>? Hidden;

    #endregion Events

    #region Fields

    private bool _isOpen;

    private AdornerLayer? _adornerLayer;
    private ElementAdorner? _elementAdorner;
    private UIElement? _element;

    #endregion Fields

    #region Overridden Methods

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            if (_isOpen)
                Hide(TextBoxOverlayResult.Accept);

        if (e.Key == Key.Escape)
            if (_isOpen)
                Hide(TextBoxOverlayResult.Cancel);

        base.OnKeyDown(e);
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        if (_isOpen) Hide(TextBoxOverlayResult.Accept);
        base.OnLostFocus(e);
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        if (_isOpen) Hide(TextBoxOverlayResult.Accept);
        base.OnLostKeyboardFocus(e);
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        if (_isOpen) Hide(TextBoxOverlayResult.Accept);
        base.OnLostMouseCapture(e);
    }

    #endregion Overridden Methods

    #region Public Methods

    public void Hide(TextBoxOverlayResult result)
    {
        if (_isOpen) // only hide once
        {
            if (_elementAdorner != null)
                if (VisualTreeHelper.GetParent(_elementAdorner) is AdornerLayer layer)
                {
                    _elementAdorner.Hide();
                    layer.Remove(_elementAdorner);
                }

            var e = new TextBoxOverlayHideEventArgs(result, Text);
            OnHidden(e);

            _isOpen = false;
        }
    }

    public void OnHidden(TextBoxOverlayHideEventArgs e)
    {
        Hidden?.Invoke(_element, e);
    }

    public void Show(UIElement element, Rect rect, string initialValue)
    {
        var size = rect.Size;
        var offset = rect.Location;

        Height = size.Height;
        Width = size.Width;

        Text = initialValue;
        SelectAll();

        _element = element;

        _adornerLayer = AdornerLayer.GetAdornerLayer(element);

        if (_adornerLayer == null) return;

        _elementAdorner = new ElementAdorner(element, this, offset);
        _adornerLayer.Add(_elementAdorner);
        Focus();

        _isOpen = true;
    }

    #endregion Public Methods
}