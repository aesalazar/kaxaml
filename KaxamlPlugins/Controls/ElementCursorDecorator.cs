using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KaxamlPlugins.Controls
{
    public class ElementCursorDecorator : Decorator
    {
        private CursorAdorner? _cursorAdorner;

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            // setup the adorner layer
            var adornerLayer = AdornerLayer.GetAdornerLayer(this);

            if (adornerLayer == null)
            {
                return;
            }

            if (_cursorAdorner == null)
            {
                _cursorAdorner = new CursorAdorner(this, CursorElement);
            }

            adornerLayer.Add(_cursorAdorner);

            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (_cursorAdorner != null)
            {
                var layer = VisualTreeHelper.GetParent(_cursorAdorner) as AdornerLayer;
                layer?.Remove(_cursorAdorner);
            }

            base.OnMouseLeave(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_cursorAdorner != null) _cursorAdorner.Offset = e.GetPosition(this);
            base.OnMouseMove(e);
        }

        static ElementCursorDecorator()
        {
            CursorProperty.OverrideMetadata(typeof(ElementCursorDecorator), new FrameworkPropertyMetadata(Cursors.None));
            ForceCursorProperty.OverrideMetadata(typeof(ElementCursorDecorator), new FrameworkPropertyMetadata(true));
        }

        public UIElement CursorElement
        { get => (UIElement)GetValue(CursorElementProperty); set => SetValue(CursorElementProperty, value);
        }
        public static readonly DependencyProperty CursorElementProperty =
            DependencyProperty.Register(nameof(CursorElement), typeof(UIElement), typeof(ElementCursorDecorator), new UIPropertyMetadata(null));

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
        }
    }

    internal sealed class CursorAdorner : Adorner
    {
        private readonly UIElement _cursor;

        private Point _offset;
        public Point Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                InvalidateArrange();
            }
        }

        public CursorAdorner(ElementCursorDecorator owner, UIElement cursor)
            : base(owner)
        {
            _cursor = cursor;
            AddVisualChild(_cursor);
        }

        protected override Visual GetVisualChild(int index)
        {
            return _cursor;
        }

        protected override int VisualChildrenCount => 1;

        protected override Size ArrangeOverride(Size finalSize)
        {
            _cursor.Arrange(new Rect(Offset, _cursor.DesiredSize));
            return finalSize;
        }

    }
}
