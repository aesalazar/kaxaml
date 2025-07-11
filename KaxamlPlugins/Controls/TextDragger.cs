using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using KaxamlPlugins.Utilities;

namespace KaxamlPlugins.Controls
{
    public class TextDragger : Decorator
    {
        static TextDragger()
        {
            CursorProperty.OverrideMetadata(typeof(TextDragger), new FrameworkPropertyMetadata(Cursors.Hand));
        }

        public string Text
        {
            get => (string)GetValue(TextProperty); 
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), 
            typeof(string), 
            typeof(TextDragger), 
            new UIPropertyMetadata(string.Empty));

        public object Data
        { 
            get => GetValue(DataProperty); 
            set => SetValue(DataProperty, value);
        }
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
            nameof(Data), 
            typeof(object), 
            typeof(TextDragger), 
            new UIPropertyMetadata(null));

        private bool _isClipboardSet;

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                if (!string.IsNullOrEmpty(Text))
                {
                    Clipboard.SetText(Text);
                    _isClipboardSet = true;
                }
                else if (Data != null)
                {
                    Clipboard.SetText(Data.ToString());
                    _isClipboardSet = true;
                }
            }
            else if (e.ClickCount == 2)
            {
                if (_isClipboardSet)
                {
                    KaxamlInfo.Editor?.InsertStringAtCaret(Text);
                    _isClipboardSet = false;
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            // need this to ensure hittesting
            drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));
            base.OnRender(drawingContext);
        }

        private bool _isDragging;

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
            {
                StartDrag();
            }
            else if (e.LeftButton == MouseButtonState.Released)
            {
                _isDragging = false;
            }

            base.OnPreviewMouseMove(e);
        }

        private void StartDrag()
        {
            var obj = new DataObject(DataFormats.Text, Text);

            if (obj != null)
            {
                if (Data != null) obj.SetData(Data.GetType(), Data);

                try
                {
                    DragDrop.DoDragDrop(this, obj, DragDropEffects.Copy);
                }
                catch (Exception ex)
                {
                    if (ex.IsCriticalException())
                    {
                        throw;
                    }
                }
            }
        }

    }
}