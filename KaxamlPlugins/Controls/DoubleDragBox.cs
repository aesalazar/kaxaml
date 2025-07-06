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

    public class DoubleDragBox : Control
    {

        static DoubleDragBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DoubleDragBox), new FrameworkPropertyMetadata(typeof(DoubleDragBox)));
        }


        #region Dependency Properties


        /// <summary>
        /// Indicates the number of digits that are displayed (beyond 0).
        /// </summary>
        public static readonly DependencyProperty PrecisionProperty =
            DependencyProperty.Register(nameof(Precision), typeof(int), typeof(DoubleDragBox), new UIPropertyMetadata(2));

        public int Precision
        { get => (int)GetValue(PrecisionProperty); set => SetValue(PrecisionProperty, value);
        }


        /// <summary>
        /// A string corresponding to the value of the Current property.
        /// </summary>
        public static readonly DependencyProperty CurrentTextProperty =
            DependencyProperty.Register(nameof(CurrentText), typeof(string), typeof(DoubleDragBox), new UIPropertyMetadata(""));

        public string CurrentText
        { get => (string)GetValue(CurrentTextProperty); set => SetValue(CurrentTextProperty, value);
        }


        /// <summary>
        /// The current value.
        /// </summary>
        public static readonly DependencyProperty CurrentProperty =
            DependencyProperty.Register(nameof(Current), typeof(double), typeof(DoubleDragBox),
            new FrameworkPropertyMetadata(double.MinValue, CurrentChanged, CurrentCoerce));

        public double Current
        { get => (double)GetValue(CurrentProperty); set => SetValue(CurrentProperty, value);
        }

        public static void CurrentChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var ddb = (DoubleDragBox)o;
            var d = (double)e.NewValue;

            ddb.CurrentText = Math.Round(d, ddb.Precision).ToString();
        }

        public static object CurrentCoerce(DependencyObject o, object value)
        {
            var ddb = (DoubleDragBox)o;
            var v = (double)value;

            if (ddb != null)
            {
                if (v > ddb.Maximum) return ddb.Maximum;
                if (v < ddb.Minimum) return ddb.Minimum;
            }

            return v;
        }


        public static readonly DependencyProperty IntervalProperty =
            DependencyProperty.Register(nameof(Interval), typeof(double), typeof(DoubleDragBox), new UIPropertyMetadata(0.05));

        public double Interval
        { get => (double)GetValue(IntervalProperty); set => SetValue(IntervalProperty, value);
        }



        public static readonly DependencyProperty SensitivityProperty =
            DependencyProperty.Register(nameof(Sensitivity), typeof(double), typeof(DoubleDragBox), new UIPropertyMetadata(20.0));

        public double Sensitivity
        { get => (double)GetValue(SensitivityProperty); set => SetValue(SensitivityProperty, value);
        }




        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(DoubleDragBox), new UIPropertyMetadata(0.0));

        public double Minimum
        { get => (double)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value);
        }


        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(DoubleDragBox), new UIPropertyMetadata(1.0));

        public double Maximum
        { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value);
        }





        #endregion


        private Point _beginPoint;
        private bool _isPointValid;

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            CaptureMouse();
            _beginPoint = e.GetPosition(this);
            _isPointValid = true;
            base.OnPreviewMouseDown(e);
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            _isPointValid = false;
            ReleaseMouseCapture();
            base.OnPreviewMouseUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (_isPointValid)
                {

                    if (e.GetPosition(this).Y - _beginPoint.Y > Sensitivity)
                    {
                        Current -= Interval;
                        _beginPoint.Y = e.GetPosition(this).Y;
                    }
                    else if (e.GetPosition(this).Y - _beginPoint.Y < -1 * Sensitivity)
                    {
                        Current += Interval;
                        _beginPoint.Y = e.GetPosition(this).Y;
                    }
                }
            }

            base.OnMouseMove(e);
        }
    }
}
