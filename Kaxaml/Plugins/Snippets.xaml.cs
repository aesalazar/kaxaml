using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using Kaxaml.CodeSnippets;
using KaxamlPlugins.DependencyInjection;
using KaxamlPlugins.Utilities;

namespace Kaxaml.Plugins;

/// <summary>
/// Manages access to the XML Snippet file.
/// </summary>
public partial class Snippets
{
    #region Constructors

    public Snippets(SnippetEditor snippetEditor)
    {
        _snippetEditor = snippetEditor;

        // load the snippets file from the disk
        SnippetCategories = [];
        ReadValues();
        InitializeComponent();
    }

    #endregion Constructors

    #region Fields

    private const string SnippetsFile = "KaxamlSnippets.xml";

    private readonly SnippetEditor _snippetEditor;

    #endregion Fields

    #region Event Handlers

    private void SnippetEditor_OnCommitValues(object sender, RoutedEventArgs e)
    {
        WriteValues();
    }

    private void SnippetEditor_OnClosed(object? sender, EventArgs e)
    {
        _snippetEditor.Closed -= SnippetEditor_OnClosed;
        _snippetEditor.CommitValues -= SnippetEditor_OnCommitValues;
    }

    #endregion Event Handlers

    #region Fields

    private TextBoxOverlay? _tbo;
    private EventHandler<TextBoxOverlayHideEventArgs>? _snippetHidden;
    private EventHandler<TextBoxOverlayHideEventArgs>? _categoryHidden;

    #endregion Fields

    #region Properties

    public ObservableCollection<SnippetCategory> SnippetCategories { get; }

    private static string SnippetsFullPath => Path.Combine(
        ApplicationDiServiceProvider.SnippetDirectory,
        SnippetsFile);

    private TextBoxOverlay TextBoxOverlay
    {
        get
        {
            if (_tbo != null) return _tbo;
            _tbo = new TextBoxOverlay();
            Style? style = null;
            try
            {
                style = (Style)FindResource("TextBoxOverlayStyle");
            }
            catch (Exception ex)
            {
                if (ex.IsCriticalException()) throw;
            }

            _tbo.Style = style;
            return _tbo;
        }
    }

    #endregion Properties

    #region Private Methods

    private void MoveSnippetDown(object o, EventArgs e)
    {
        var cm = (ContextMenu)ItemsControl.ItemsControlFromItemContainer(o as MenuItem);
        var lbi = (ListBoxItem)cm.PlacementTarget;
        var s = (Snippet)lbi.DataContext;
        var c = s.Category;

        var index = c.Snippets.IndexOf(s);
        if (index < c.Snippets.Count - 1) c.Snippets.Move(index, index + 1);

        WriteValues();
    }

    private void MoveSnippetUp(object o, EventArgs e)
    {
        var cm = (ContextMenu)ItemsControl.ItemsControlFromItemContainer(o as MenuItem);
        var lbi = (ListBoxItem)cm.PlacementTarget;
        var s = (Snippet)lbi.DataContext;
        var c = s.Category;

        var index = c.Snippets.IndexOf(s);
        if (index > 0) c.Snippets.Move(index, index - 1);

        WriteValues();
    }

    private static void ValidateSnippetXmlFile()
    {
        if (File.Exists(SnippetsFullPath)) return;

        var sourcePath = Path.Combine(
            ApplicationDiServiceProvider.StartupPath,
            SnippetsFile);

        Directory.CreateDirectory(ApplicationDiServiceProvider.SnippetDirectory);
        File.Copy(sourcePath, SnippetsFullPath, true);
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
            var s = (Snippet?)e.Data.GetData(typeof(Snippet));

            // make sure we're not dragging into the same category
            if (s is null || s.Category == sc) return;

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
            var text = (string?)e.Data.GetData(DataFormats.Text);
            if (text is null) return;

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

            var text = (string?)e.Data.GetData(DataFormats.Text);
            if (text is null) return;

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
            _snippetEditor.Show(s);
            _snippetEditor.CommitValues += SnippetEditor_OnCommitValues;
            _snippetEditor.Closed += SnippetEditor_OnClosed;
        }
    }

    public ArrayList GetSnippetCompletionItems()
    {
        var items = new ArrayList();
        items.Sort();

        foreach (var c in SnippetCategories)
        foreach (var s in c.Snippets)
            if (!string.IsNullOrEmpty(s.Shortcut))
            {
                var item = new SnippetCompletionData(s.Text, s.Shortcut, s);
                items.Add(item);
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
        if (index < SnippetCategories.Count - 1) SnippetCategories.Move(index, index + 1);
    }

    public void MoveCategoryUp(object o, EventArgs e)
    {
        var cm = (ContextMenu)ItemsControl.ItemsControlFromItemContainer(o as MenuItem);
        var t = (TabItem)cm.PlacementTarget;
        var s = (SnippetCategory)t.DataContext;

        var index = SnippetCategories.IndexOf(s);
        if (index > 0) SnippetCategories.Move(index, index - 1);
    }

    public void NewCategory(object o, EventArgs e)
    {
        var s = new SnippetCategory
        {
            Name = "New Category"
        };
        SnippetCategories.Add(s);
    }

    public void ReadValues()
    {
        ValidateSnippetXmlFile();
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
        if (root is null) return;

        foreach (XmlNode categoryNode in root.ChildNodes)
            if (categoryNode.Name == "Category")
            {
                // look for a matching category
                var c = (SnippetCategory?)null;

                foreach (var sc in SnippetCategories)
                    if (string.Compare(
                            sc.Name,
                            categoryNode.Attributes?["Name"]?.Value,
                            StringComparison.Ordinal) == 0)
                        c = sc;

                if (c == null)
                {
                    c = new SnippetCategory
                    {
                        Name = categoryNode.Attributes?["Name"]?.Value ?? string.Empty
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
        var xmlWriter = new XmlTextWriter(SnippetsFullPath, Encoding.UTF8)
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

        foreach (var snippetCategory in SnippetCategories)
        {
            var categoryNode = xml.CreateElement("Category");
            categoryNode.SetAttribute("Name", snippetCategory.Name);

            foreach (var s in snippetCategory.Snippets)
            {
                var snippetNode = xml.CreateElement("Snippet");
                snippetNode.SetAttribute("Name", s.Name);
                snippetNode.SetAttribute("Shortcut", s.Shortcut);
                categoryNode.AppendChild(snippetNode);

                var cdata = xml.CreateCDataSection(s.Text);
                snippetNode.AppendChild(cdata);
            }

            root.AppendChild(categoryNode);
        }

        xml.Save(SnippetsFullPath);
    }

    #endregion Public Methods
}