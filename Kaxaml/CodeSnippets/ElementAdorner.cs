using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Kaxaml.CodeSnippets;

internal sealed class ElementAdorner : Adorner
{
    #region Constructors

    public ElementAdorner(UIElement owner, UIElement element, Point offset)
        : base(owner)
    {
        _element = element;

        AddVisualChild(element);
        Offset = offset;
    }

    #endregion Constructors

    #region Methods

    internal void Hide()
    {
        RemoveVisualChild(_element);
        _element = null;
    }

    #endregion Methods

    #region Fields

    private Point _offset;
    private UIElement? _element;

    #endregion Fields

    #region Properties

    protected override int VisualChildrenCount => 1;

    public Point Offset
    {
        get => _offset;
        set
        {
            _offset = value;
            InvalidateArrange();
        }
    }

    #endregion Properties

    #region Overridden Methods

    protected override Size ArrangeOverride(Size finalSize)
    {
        _element?.Arrange(new Rect(Offset, _element.DesiredSize));
        return finalSize;
    }

    protected override Visual? GetVisualChild(int index) => _element;

    #endregion Overridden Methods
}