using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;
using System.Collections;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using System.Windows.Media.Animation;

namespace Kaxaml.CodeCompletion
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>

    public partial class StringBox : UserControl
    {

		#region Constructors 

        public StringBox()
        {
            InitializeComponent();

            StringHostItems = new ObservableCollection<StringHost>();

            StringHostItems.Add(new StringHost("")); //1
            StringHostItems.Add(new StringHost("")); //2
            StringHostItems.Add(new StringHost("")); //3
            StringHostItems.Add(new StringHost("")); //4
            StringHostItems.Add(new StringHost("")); //5
            StringHostItems.Add(new StringHost("")); //6
            StringHostItems.Add(new StringHost("")); //7
            StringHostItems.Add(new StringHost("")); //8
            StringHostItems.Add(new StringHost("")); //9
            StringHostItems.Add(new StringHost("")); //10
        }

		#endregion Constructors 


        #region Private Fields

        private StringHost? _selectedItem;
        private int _selectedIndexInView = -1;
        private int _topOffset;

        #endregion

        #region SelectedIndex

        public int SelectedIndex
        {
            get => _topOffset + _selectedIndexInView;
            set
            {
                if (value >= 0 && value < CompletionItems.Count)
                {

                    // if index is greater than 10 or less than count - 10, then
                    // we can just set the top offset and select the top item

                    if (value > 10 - 1 && value < CompletionItems.Count - 10)
                    {
                        SetTopOffset(value);
                        SelectItemByIndex(0);
                    }
                    else
                    {
                        if (value <= 10 - 1)
                        {
                            SetTopOffset(0);
                            SelectItemByIndex(value);
                        }
                        else if (value > CompletionItems.Count - 10)
                        {
                            SetTopOffset(CompletionItems.Count - 10);
                            SelectItemByIndex(value - _topOffset);
                        }
                    }
                }
            }
        }

        #endregion

        #region SelectedItem

        public object? SelectedItem => CompletionItems[SelectedIndex];

        #endregion

        #region StringHostItems (DependencyProperty)

        /// <summary>
        /// description of the property
        /// </summary>
        public ObservableCollection<StringHost> StringHostItems
        { get => (ObservableCollection<StringHost>)GetValue(StringHostItemsProperty); set => SetValue(StringHostItemsProperty, value);
        }

        /// <summary>
        /// DependencyProperty for StringHostItems
        /// </summary>
        public static readonly DependencyProperty StringHostItemsProperty =
            DependencyProperty.Register(nameof(StringHostItems), typeof(ObservableCollection<StringHost>), typeof(StringBox),
            new FrameworkPropertyMetadata(default(ObservableCollection<StringHost>)));

        #endregion

        #region CompletionItems (DependencyProperty)

        /// <summary>
        /// description of the property
        /// </summary>
        public ArrayList CompletionItems
        {
            get => (ArrayList)GetValue(CompletionItemsProperty);
            set => SetValue(CompletionItemsProperty, value);
        }

        /// <summary>
        /// DependencyProperty for CompletionItems
        /// </summary>
        public static readonly DependencyProperty CompletionItemsProperty =
            DependencyProperty.Register(nameof(CompletionItems), typeof(ArrayList), typeof(StringBox), new FrameworkPropertyMetadata(default(ArrayList), CompletionItemsChanged));

        /// <summary>
        /// PropertyChangedCallback for CompletionItems
        /// </summary>
        private static void CompletionItemsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj is StringBox owner)
            {
                if (args.NewValue is ArrayList items)
                {
                    var max = Math.Min(items.Count, 10);

                    // add the new items
                    for (var i = 0; i < max; i++)
                    {
                        var item = (ICompletionData?)items[i] ?? throw new Exception("Could not extract completion data");
                        owner.StringHostItems[i].Value = item.Text;
                        owner.StringHostItems[i].Tooltip = item.Description;
                        owner.StringHostItems[i].IsSelectable = true;
                    }

                    // clear the remaining items
                    for (var i = items.Count; i < 10; i++)
                    {
                        owner.StringHostItems[i].Value = string.Empty;
                        owner.StringHostItems[i].Tooltip = string.Empty;
                        owner.StringHostItems[i].IsSelectable = false;
                    }

                    // show or hide the ScrollerSlider based on need and update its max and min values
                    if (items.Count > 10 - 1)
                    {
                        owner.ScrollerSlider.Visibility = Visibility.Visible;
                        owner.ScrollerSlider.Minimum = 0;
                        owner.ScrollerSlider.Maximum = items.Count - 10;
                    }
                    else
                    {
                        owner.ScrollerSlider.Visibility = Visibility.Collapsed;
                    }


                }
            }
        }

        #endregion

        #region Overrides

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            Mouse.Capture(this, CaptureMode.SubTree);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            ReleaseMouseCapture();

            if (_selectNextTimer != null)
            {
                _selectNextTimer.Stop();
            }

            if (_selectPreviousTimer != null)
            {
                _selectPreviousTimer.Stop();
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (IsMouseCaptured)
            {
                SelectNext();
            }

            base.OnMouseLeave(e);
        }

        private void SliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetTopOffset((int)e.NewValue, false);

            // the scrolltext feature has been disabled (because it looked goofy!), but all the
            // code is still available if we ever want to revisit it

            //if (ScrollTextTimer == null)
            //{
            //    ScrollTextTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(500), DispatcherPriority.Background, ScrollTextTimer_Tick, this.Dispatcher);
            //}

            //if (StringHostItems[0] != null && ! String.IsNullOrEmpty(StringHostItems[0].Value))
            //{
            //    ScrollTextTimer.Stop();
            //    ShowScrollText(StringHostItems[0].Value[0].ToString());
            //    ScrollTextTimer.Start();
            //}

        }

        private void ScrollTextTimer_Tick(object sender, EventArgs e)
        {
            HideScrollText();   
        }

        private void ShowScrollText(string s)
        {
            ScrollText.Text = s;

            if (FindResource("ShowScrollText") is Storyboard sb)
            {
                BeginStoryboard(sb);
            }

        }

        private void HideScrollText()
        {
            if (FindResource("HideScrollText") is Storyboard sb)
            {
                BeginStoryboard(sb);
            }
        }

        #endregion

        #region Private Methods

        private void SetTopOffset(int offset)
        {
            SetTopOffset(offset, true);
        }

        private void SetTopOffset(int offset, bool updateScrollBar)
        {
            // coerce the value of offset

            if (offset > CompletionItems.Count - 10) offset = CompletionItems.Count - 10;
            if (offset < 0) offset = 0;

            var max = Math.Min(CompletionItems.Count, 10);


            for (var i = 0; i < max; i++)
            {
                var item = (ICompletionData?)CompletionItems[i + offset] 
                           ?? throw new Exception("Could not extract completion data");
                StringHostItems[i].Value = item.Text;
                StringHostItems[i].Tooltip = item.Description;
            }
            
            if (updateScrollBar)
            {
                ScrollerSlider.Value = offset;
            }

            _topOffset = offset;
        }

        private void SelectItem(StringHost item)
        {
            if (item == null)
            {
                // clearn current selection
                if (_selectedItem != null) _selectedItem.IsSelected = false;
            }
            else if (item is { IsSelectable: true })
            {
                // clearn current selection
                if (_selectedItem != null) _selectedItem.IsSelected = false;

                // select the new item
                _selectedItem = item;
                item.IsSelected = true;

                // udpate the index
                _selectedIndexInView = StringHostItems.IndexOf(item);
            }
        }

        private int SelectItemByIndex(int index)
        {
            // coerce the index property
            if (index >= CompletionItems.Count) index = CompletionItems.Count - 1;
            if (index < 0) index = 0;

            var item = StringHostItems[index];
            SelectItem(item);

            return _selectedIndexInView;
        }

        #endregion

        #region Public Methods

        public void SelectNext()
        {
            if (_selectedIndexInView == 10 - 1)
            {
                // update the top offset
                SetTopOffset(_topOffset + 1);
            }
            else
            {
                SelectItemByIndex(_selectedIndexInView + 1);
            }
        }

        public void SelectPrevious()
        {
            if (_selectedIndexInView == 0)
            {
                // update the top offset
                SetTopOffset(_topOffset - 1);
            }
            else
            {
                SelectItemByIndex(_selectedIndexInView - 1);
            }
        }

        public void PageDown()
        {
            if (_selectedIndexInView == 10 - 1)
            {
                SetTopOffset(_topOffset + 10);
            }
            else
            {
                SelectItemByIndex(10 - 1);
            }
        }

        public void PageUp()
        {
            if (_selectedIndexInView == 0)
            {
                SetTopOffset(_topOffset - 10);
            }
            else
            {
                SelectItemByIndex(0);
            }
        }

        #endregion

        #region Event Handlers

        private void ItemMouseDown(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                var item = (StringHost)element.DataContext;
                SelectItem(item);
            }
        }

        private void ItemMouseUp(object sender, MouseEventArgs e)
        {
            if (_selectNextTimer != null)
            {
                _selectNextTimer.Stop();
            }

            if (_selectPreviousTimer != null)
            {
                _selectPreviousTimer.Stop();
            }
        }

        private void ItemMouseEnter(object sender, MouseEventArgs e)
        {
            if (_selectNextTimer != null)
            {
                _selectNextTimer.Stop();
            }

            if (_selectPreviousTimer != null)
            {
                _selectPreviousTimer.Stop();
            }

            if (IsMouseCaptured)
            {
                if (sender is FrameworkElement element)
                {
                    var item = (StringHost)element.DataContext;
                    SelectItem(item);
                }
            }
        }

        private DispatcherTimer? _selectNextTimer;
        private DispatcherTimer? _selectPreviousTimer;

        private void ItemMouseLeave(object sender, MouseEventArgs e)
        {
            if (IsMouseCaptured)
            {
                if (sender is FrameworkElement element)
                {
                    var item = (StringHost)element.DataContext;
                    var index = StringHostItems.IndexOf(item);

                    if (index == 9)
                    {
                        var p = e.GetPosition(element);

                        if (p.Y > 0)
                        {
                            if (_selectNextTimer == null)
                            {
                                _selectNextTimer = new DispatcherTimer();
                                _selectNextTimer.Tick += _SelectNextTimer_Tick;
                                _selectNextTimer.Interval = TimeSpan.FromMilliseconds(100);
                            }

                            _selectNextTimer.Start();
                        }
                    }

                    if (index == 0)
                    {
                        var p = e.GetPosition(element);

                        if (p.Y < 0)
                        {
                            if (_selectPreviousTimer == null)
                            {
                                _selectPreviousTimer = new DispatcherTimer();
                                _selectPreviousTimer.Tick += _SelectPreviousTimer_Tick;
                                _selectPreviousTimer.Interval = TimeSpan.FromMilliseconds(100);
                            }

                            _selectPreviousTimer.Start();
                        }
                    }

                }
            }
        }

        private void _SelectNextTimer_Tick(object? _, EventArgs __)
        {
            SelectNext();
        }

        private void _SelectPreviousTimer_Tick(object? _, EventArgs __)
        {
            SelectPrevious();
        }

        #endregion
    }

    public class StringHost : INotifyPropertyChanged
    {

		#region Fields 


        private string? _value;
        private string? _tooltip;

        internal bool IsSelectable = true;
        private bool _isSelected;

		#endregion Fields 

		#region Constructors 

        public StringHost(string? value)
        {
            Value = value;
        }

		#endregion Constructors 

		#region Properties 


        public string? Value
        {
            get => _value;
            set
            {
                if (value != _value)
                {
                    _value = value;
                    NotifyPropertyChanged("Value");
                }
            }
        }

        public string? Tooltip
        {
            get => _tooltip;
            set
            {
                if (value != _tooltip)
                {
                    _tooltip = value;
                    NotifyPropertyChanged("Tooltip");
                }
            }
        }


        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    NotifyPropertyChanged("IsSelected");
                }
            }
        }


        #endregion Properties 

        #region Overridden Methods 

        public override string? ToString()
        {
            return Value;
        }

		#endregion Overridden Methods 


        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

        #endregion
    }

    public class ContentItemsControl : ItemsControl
    {

		#region Overridden Methods 

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new ContentControl();
        }

		#endregion Overridden Methods 

    }


}