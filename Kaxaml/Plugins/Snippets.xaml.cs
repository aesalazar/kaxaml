using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using KaxamlPlugins;

namespace Kaxaml.Plugins.Default
{
    /// <summary>
    /// Interaction logic for Snippets.xaml
    /// </summary>

    public partial class Snippets : UserControl
    {

        #region Const Fields

        private const string SnippetsFile = "KaxamlSnippets.xml";

        #endregion Const Fields

        #region Fields

        private TextBoxOverlay? _tbo;
        private EventHandler<TextBoxOverlayHideEventArgs>? _snippetHidden;
        private EventHandler<TextBoxOverlayHideEventArgs>? _categoryHidden;

        #endregion Fields

        #region Constructors

        public Snippets()
        {
            // load the snippets file from the disk
            SnippetCategories = new();
            ReadValues();
            InitializeComponent();
        }

        #endregion Constructors

        #region Properties


        public ObservableCollection<SnippetCategory> SnippetCategories
        {
            get;
        }


        private string SnippetsFullPath
        {
            get
            {
                var fi = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var dir = fi.DirectoryName;
                return dir + "\\" + SnippetsFile;
            }
        }


        private TextBoxOverlay TextBoxOverlay
        {
            get
            {
                if (_tbo == null)
                {
                    _tbo = new TextBoxOverlay();
                    var style = (Style?)null;
                    try
                    {
                        style = (Style)FindResource("TextBoxOverlayStyle");
                    }
                    catch (Exception ex)
                    {
                        if (ex.IsCriticalException())
                        {
                            throw;
                        }
                    }
                    _tbo.Style = style;
                }
                return _tbo;
            }
        }


        #endregion Properties

        #region Event Handlers

        private void editor_CommitValues(object sender, RoutedEventArgs e)
        {
            WriteValues();
        }

        #endregion Event Handlers

        #region Private Methods

        private void MoveSnippetDown(object o, EventArgs e)
        {
            var cm = (ContextMenu)ItemsControl.ItemsControlFromItemContainer(o as MenuItem);
            var lbi = (ListBoxItem)cm.PlacementTarget;
            var s = (Snippet)lbi.DataContext;
            var c = s.Category;

            var index = c.Snippets.IndexOf(s);
            if (index < c.Snippets.Count - 1)
            {
                c.Snippets.Move(index, index + 1);
            }

            WriteValues();
        }

        private void MoveSnippetUp(object o, EventArgs e)
        {
            var cm = (ContextMenu)ItemsControl.ItemsControlFromItemContainer(o as MenuItem);
            var lbi = (ListBoxItem)cm.PlacementTarget;
            var s = (Snippet)lbi.DataContext;
            var c = s.Category;

            var index = c.Snippets.IndexOf(s);
            if (index > 0)
            {
                c.Snippets.Move(index, index - 1);
            }

            WriteValues();
        }

        #endregion Private Methods

        #region Public Methods

        public void DeleteCategory(object o, EventArgs e)
        {
            var cm = (ContextMenu)ItemsControl.ItemsControlFromItemContainer(o as MenuItem);
            var t = (TabItem)cm.PlacementTarget;
            var s = (SnippetCategory)t.DataContext;

            if (MessageBox.Show("Are you sure you want to delete the category " + s.Name + " and all associated snippets?", "Delete Category?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                SnippetCategories.Remove(s);
                WriteValues();
            }
        }

        public void DeleteSnippet(object o, EventArgs e)
        {
            var cm = (ContextMenu)ItemsControl.ItemsControlFromItemContainer(o as MenuItem);
            var lbi = (ListBoxItem)cm.PlacementTarget;
            var s = (Snippet)lbi.DataContext;
            var c = s.Category;
            c.Snippets.Remove(s);
            WriteValues();
        }

        public void DoCategoryHidden(object? o, TextBoxOverlayHideEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(o);
            var ti = (TabItem)o;
            var c = (SnippetCategory)ti.DataContext;

            if (e.Result == TextBoxOverlayResult.Accept)
            {
                c.Name = e.ResultText;
                WriteValues();
            }

            TextBoxOverlay.Hidden -= _categoryHidden;
        }

        public void DoDrop(object o, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Snippet)))
            {
                var sc = (SnippetCategory)((FrameworkElement)o).DataContext;
                var s = (Snippet)e.Data.GetData(typeof(Snippet));

                // make sure we're not dragging into the same category
                if (s.Category == sc) return;

                // otherwise, consider this a move so remove from the previous category
                // and add to the new one

                // remove from old category
                s.Category.Snippets.Remove(s);

                // add to the new one
                sc.Snippets.Add(s);

                // update the category
                s.Category = sc;

                // save the changes
                WriteValues();
            }
            else if (e.Data.GetDataPresent(DataFormats.Text))
            {
                var text = (string)e.Data.GetData(DataFormats.Text);
                var sc = (SnippetCategory)((FrameworkElement)o).DataContext;
                sc.AddSnippet("New Snippet", "", text);

                // write the xaml file
                WriteValues();
            }

            // if the drop target is a TabItem, then expand it 
            if (o.GetType() == typeof(TabItem))
            {
                var ti = (TabItem)o;
                ti.IsSelected = true;
            }
        }

        public void DoGridDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text))
            {
                // create a new category
                var sc = new SnippetCategory
                {
                    Name = "New Category"
                };
                SnippetCategories.Add(sc);

                var text = (string)e.Data.GetData(DataFormats.Text);
                sc.AddSnippet("New Snippet", "", text);

                var ti = (TabItem)SnippetCategoriesTabControl.ItemContainerGenerator.ContainerFromItem(sc);
                if (ti != null) ti.IsSelected = true;

                // write the xaml file
                WriteValues();

                // don't allow drops here anymore
                MainGrid.AllowDrop = false;
                MainGrid.Drop -= DoGridDrop;
            }

        }

        public void DoSnippetHidden(object? o, TextBoxOverlayHideEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(o);
            TextBoxOverlay.Hidden -= _snippetHidden;

            var lbi = (ListBoxItem)o;
            var s = (Snippet)lbi.DataContext;

            if (e.Result == TextBoxOverlayResult.Accept)
            {
                s.Name = e.ResultText;
                WriteValues();
            }
        }

        public void EditSnippet(object o, EventArgs e)
        {
            var cm = (ContextMenu)ItemsControl.ItemsControlFromItemContainer(o as MenuItem);
            var lbi = (ListBoxItem)cm.PlacementTarget;

            if (lbi.DataContext is Snippet s)
            {
                var editor = SnippetEditor.Show(s, Application.Current.MainWindow);
                editor.CommitValues += editor_CommitValues;
            }
        }

        public ArrayList GetSnippetCompletionItems()
        {
            var items = new ArrayList();
            items.Sort();

            foreach (var c in SnippetCategories)
            {
                foreach (var s in c.Snippets)
                {
                    if (!string.IsNullOrEmpty(s.Shortcut))
                    {
                        var item = new SnippetCompletionData(s.Text, s.Shortcut, s);
                        items.Add(item);
                    }
                }
            }

            return items;
        }

        public void GridLoaded(object sender, RoutedEventArgs e)
        {
            if (SnippetCategories.Count == 0)
            {
                MainGrid.AllowDrop = true;
                MainGrid.Drop += DoGridDrop;
            }
        }

        public void MoveCategoryDown(object o, EventArgs e)
        {
            var cm = (ContextMenu)ItemsControl.ItemsControlFromItemContainer(o as MenuItem);
            var t = (TabItem)cm.PlacementTarget;
            var s = (SnippetCategory)t.DataContext;

            var index = SnippetCategories.IndexOf(s);
            if (index < SnippetCategories.Count - 1)
            {
                SnippetCategories.Move(index, index + 1);
            }
        }

        public void MoveCategoryUp(object o, EventArgs e)
        {
            var cm = (ContextMenu)ItemsControl.ItemsControlFromItemContainer(o as MenuItem);
            var t = (TabItem)cm.PlacementTarget;
            var s = (SnippetCategory)t.DataContext;

            var index = SnippetCategories.IndexOf(s);
            if (index > 0)
            {
                SnippetCategories.Move(index, index - 1);
            }
        }

        public void NewCategory(object o, EventArgs e)
        {
            var s = new SnippetCategory
            {
                Name = "New Category"
            };
            SnippetCategories.Add(s);
        }

        public void NewSnippet(object o, EventArgs e)
        {
            var cm = (ContextMenu)ItemsControl.ItemsControlFromItemContainer(o as MenuItem);
            var lbi = (ListBoxItem)cm.PlacementTarget;
            var s = (Snippet)lbi.DataContext;
            WriteValues();

        }

        public void ReadValues()
        {
            SnippetCategories.Clear();

            var xml = new XmlDocument();

            try
            {
                xml.Load(SnippetsFullPath);
            }
            catch (FileNotFoundException)
            {
                return;
            }

            var root = xml.DocumentElement;
            if(root is null ) return;

            foreach (XmlNode categoryNode in root.ChildNodes)
            {
                if (categoryNode.Name == "Category")
                {
                    // look for a matching categor
                    var c = (SnippetCategory?)null;

                    foreach (var sc in SnippetCategories)
                    {
                        if (sc.Name?.CompareTo(categoryNode.Attributes?["Name"]?.Value) == 0) c = sc;
                    }

                    if (c == null)
                    {
                        c = new SnippetCategory
                        {
                            Name = categoryNode.Attributes?["Name"]?.Value ?? string.Empty,
                        };
                        SnippetCategories.Add(c);
                    }

                    foreach (XmlNode snippetNode in categoryNode.ChildNodes)
                    {
                        var name = snippetNode.Attributes?["Name"]?.Value ?? string.Empty;
                        var shortcut = snippetNode.Attributes?["Shortcut"]?.Value ?? string.Empty;
                        c.AddSnippet(name, shortcut, snippetNode.InnerText);
                    }
                }
            }
        }

        public void RenameCategory(object o, EventArgs e)
        {
            var cm = (ContextMenu)ItemsControl.ItemsControlFromItemContainer(o as MenuItem);
            var ti = (TabItem)cm.PlacementTarget;
            var c = (SnippetCategory)ti.DataContext;

            _categoryHidden ??= DoCategoryHidden;
            TextBoxOverlay.Hidden += _categoryHidden;

            TextBoxOverlay.Hidden += DoCategoryHidden;
            TextBoxOverlay.Show(ti, new Rect(new Point(14, 2), new Size(ti.ActualWidth - 20, 20)), c.Name);

            WriteValues();

        }

        public void RenameSnippet(object o, EventArgs e)
        {
            var cm = (ContextMenu)ItemsControl.ItemsControlFromItemContainer(o as MenuItem);
            var lbi = (ListBoxItem)cm.PlacementTarget;
            var s = (Snippet)lbi.DataContext;

            _snippetHidden ??= DoSnippetHidden;
            TextBoxOverlay.Hidden += _snippetHidden;

            // HACK: i'm handling the offset here rather than in the style
            TextBoxOverlay.Show(lbi, new Rect(new Point(14, 0), new Size(lbi.ActualWidth - 14, lbi.ActualHeight)), s.Name);

            WriteValues();

        }

        public void WriteValues()
        {
            var xmlWriter = new XmlTextWriter(SnippetsFullPath, System.Text.Encoding.UTF8)
            {
                Formatting = Formatting.Indented
            };
            xmlWriter.WriteProcessingInstruction("xml", "version='1.0' encoding='UTF-8'");
            xmlWriter.WriteStartElement("Snippets");
            xmlWriter.Close();

            var xml = new XmlDocument();
            xml.Load(SnippetsFullPath);

            var root = xml.DocumentElement 
                ?? throw new Exception($"Missing root element on snippet:{SnippetsFullPath}");

            foreach (var c in SnippetCategories)
            {
                var cnode = xml.CreateElement("Category");
                cnode.SetAttribute("Name", c.Name);

                if (c.Snippets != null)
                {

                    foreach (var s in c.Snippets)
                    {
                        var snode = xml.CreateElement("Snippet");
                        snode.SetAttribute("Name", s.Name);
                        snode.SetAttribute("Shortcut", s.Shortcut);
                        cnode.AppendChild(snode);

                        var cdata = xml.CreateCDataSection(s.Text);
                        snode.AppendChild(cdata);
                    }

                    root.AppendChild(cnode);
                }
            }

            xml.Save(SnippetsFullPath);

        }

        #endregion Public Methods

    }

    public class SnippetCompletionData : ICompletionData
    {

        #region Constructors

        public SnippetCompletionData(string description, string text, Snippet snippet)
        {
            Description = description;
            Text = text;
            Snippet = snippet;
        }

        #endregion Constructors


        #region ICompletionData Members

        public string Description { get; }

        public int ImageIndex => 0;

        public bool InsertAction(ICSharpCode.TextEditor.TextArea textArea, char ch)
        {
            return true;
        }

        public double Priority => 0;

        public string Text { get; set; }

        public Snippet Snippet { get; set; }


        #endregion

        #region IComparable Members

        public int CompareTo(object obj)
        {
            var s = (SnippetCompletionData)obj;
            return s.Text.CompareTo(Text);
        }

        #endregion
    }

    public class Snippet : INotifyPropertyChanged
    {

        #region Fields


        private string _name;
        private string _shortcut;
        private string _text;

        private SnippetCategory _category;

        #endregion Fields

        #region Constructors

        public Snippet(string name, string shortcut, string text, SnippetCategory category)
        {
            _name = name;
            _shortcut = shortcut;
            _text = text;
            _category = category;
        }

        #endregion Constructors

        #region Properties


        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public string Shortcut
        {
            get => _shortcut;
            set
            {
                if (_shortcut != value)
                {
                    _shortcut = value;
                    OnPropertyChanged("Shortcut");
                }
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged("Text");
                }
            }
        }


        public SnippetCategory Category
        {
            get => _category;
            set
            {
                if (_category != value)
                {
                    _category = value;
                    OnPropertyChanged("Category");
                }
            }
        }


        #endregion Properties

        #region Events

        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion Events

        #region Overridden Methods

        public override string ToString()
        {
            return Text;
        }

        #endregion Overridden Methods

        #region Private Methods

        private void OnPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

        #endregion Private Methods

        #region Public Methods

        public string IndentedText(int count, bool skipFirstLine)
        {
            var t = Text.Replace("\r\n", "\n");

            if (t.CompareTo(Text) != 0)
            {
                // separate Text into lines
                var lines = t.Split('\n');

                // generate the "indent" string
                var indent = "";
                for (var i = 0; i < count; i++)
                {
                    indent = indent + " ";
                }

                // append indent to the beginning of each string and
                // generate the result string (with newly inserted line ends)

                var result = "";
                for (var i = 0; i < lines.Length; i++)
                {
                    if (skipFirstLine && i == 0)
                    {
                        result = result + lines[i] + "\r\n";
                    }
                    else if (i == lines.Length - 1)
                    {
                        lines[i] = lines[i].Replace("\n", "");
                        result = result + indent + lines[i];
                    }
                    else
                    {
                        lines[i] = lines[i].Replace("\n", "");
                        result = result + indent + lines[i] + "\r\n";
                    }
                }

                return result;
            }

            return Text;
        }

        #endregion Public Methods

    }

    public class SnippetCategory : INotifyPropertyChanged
    {

        #region Fields

        private string _name = string.Empty;

        #endregion Fields

        #region Properties

        public ObservableCollection<Snippet> Snippets { get; } = new();

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged("Name");
                }
            }
        }


        #endregion Properties

        #region Events

        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion Events

        #region Private Methods

        private void OnPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

        #endregion Private Methods

        #region Public Methods

        public void AddSnippet(string name, string shortcut, string text)
        {
            var s = new Snippet(name, shortcut, text, this);
            Snippets.Add(s);
        }

        #endregion Public Methods

    }


    public enum TextBoxOverlayResult { None, Accept, Cancel };

    public class TextBoxOverlayHideEventArgs : EventArgs
    {

        #region Fields


        public readonly string ResultText;

        public readonly TextBoxOverlayResult Result;

        #endregion Fields

        #region Constructors

        public TextBoxOverlayHideEventArgs(TextBoxOverlayResult result, string resultText)
        {
            Result = result;
            ResultText = resultText;
        }

        #endregion Constructors

    }

    public class TextBoxOverlay : TextBox
    {

        #region Fields


        private bool _isOpen;

        private AdornerLayer? _adornerLayer;
        private ElementAdorner? _elementAdorner;
        private UIElement? _element;

        #endregion Fields

        #region Constructors

        public TextBoxOverlay()
        {

        }

        #endregion Constructors

        #region Events

        public event EventHandler<TextBoxOverlayHideEventArgs>? Hidden;

        #endregion Events

        #region Overridden Methods

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (_isOpen) Hide(TextBoxOverlayResult.Accept);
            }

            if (e.Key == Key.Escape)
            {
                if (_isOpen) Hide(TextBoxOverlayResult.Cancel);
            }

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
                {
                    if (VisualTreeHelper.GetParent(_elementAdorner) is AdornerLayer layer)
                    {
                        _elementAdorner.Hide();
                        layer.Remove(_elementAdorner);
                    }
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

            if (_adornerLayer == null)
            {
                return;
            }

            _elementAdorner = new ElementAdorner(element, this, offset);
            _adornerLayer.Add(_elementAdorner);
            Focus();

            _isOpen = true;
        }

        #endregion Public Methods

    }

    internal sealed class ElementAdorner : Adorner
    {

        #region Fields


        private Point _offset;
        private UIElement? _element;

        #endregion Fields

        #region Constructors

        public ElementAdorner(UIElement owner, UIElement element, Point offset)
            : base(owner)
        {
            _element = element;

            AddVisualChild(element);
            Offset = offset;
        }

        #endregion Constructors

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

        protected override Visual? GetVisualChild(int index)
        {
            return _element;
        }

        #endregion Overridden Methods

        #region Methods

        internal void Hide()
        {
            RemoveVisualChild(_element);
            _element = null;
        }

        #endregion Methods

    }

}