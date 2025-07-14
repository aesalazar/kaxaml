using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Kaxaml.Documents;
using Kaxaml.DocumentViews;
using KaxamlPlugins;

namespace Kaxaml.Views;

public partial class DocumentsView
{
    #region Constructors

    public DocumentsView()
    {
        InitializeComponent();
        KaxamlInfo.ParseRequested += KaxamlInfo_ParseRequested;
    }

    #endregion Constructors


    #region XamlDocuments (DependencyProperty)

    /// <summary>
    /// description of XamlDocuments
    /// </summary>
    public ObservableCollection<XamlDocument> XamlDocuments
    {
        get => (ObservableCollection<XamlDocument>)GetValue(XamlDocumentsProperty);
        set => SetValue(XamlDocumentsProperty, value);
    }

    /// <summary>
    /// DependencyProperty for XamlDocuments
    /// </summary>
    public static readonly DependencyProperty XamlDocumentsProperty =
        DependencyProperty.Register(nameof(XamlDocuments), typeof(ObservableCollection<XamlDocument>), typeof(DocumentsView), new FrameworkPropertyMetadata(new ObservableCollection<XamlDocument>()));

    #endregion

    #region SelectedDocument (DependencyProperty)

    /// <summary>
    /// The currently selected XamlDocument.
    /// </summary>
    public XamlDocument? SelectedDocument
    {
        get => (XamlDocument?)GetValue(SelectedDocumentProperty);
        set => SetValue(SelectedDocumentProperty, value);
    }

    /// <summary>
    /// DependencyProperty for SelectedDocument
    /// </summary>
    public static readonly DependencyProperty SelectedDocumentProperty =
        DependencyProperty.Register(nameof(SelectedDocument), typeof(XamlDocument), typeof(DocumentsView),
            new FrameworkPropertyMetadata(default(XamlDocument), SelectedDocumentChanged));

    /// <summary>
    /// PropertyChangedCallback for SelectedDocument
    /// </summary>
    private static void SelectedDocumentChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
    {
        if (obj is DocumentsView owner)
        {
            // handle changed event here

            var document = (XamlDocument)args.NewValue;
            var listBoxItem = (ListBoxItem)owner.ContentListBox.ItemContainerGenerator.ContainerFromItem(document);

            if (listBoxItem != null)
            {
                var v = (IXamlDocumentView)listBoxItem.Template.FindName("PART_DocumentView", listBoxItem);
                if (v != null)
                {
                    owner._view = v; // (IXamlDocumentView)listBoxItem.Template.FindName("PART_DocumentView", listBoxItem);
                    owner.SelectedView = owner._view;
                    v.OnActivate();
                    KaxamlInfo.Editor = owner.SelectedView.TextEditor;
                }
            }
        }
    }

    #endregion

    #region SelectedView (DependencyProperty)

    /// <summary>
    /// The view associated with the currently selected document.
    /// </summary>
    public IXamlDocumentView? SelectedView
    {
        get => (IXamlDocumentView)GetValue(SelectedViewProperty);
        set => SetValue(SelectedViewProperty, value);
    }

    /// <summary>
    /// DependencyProperty for SelectedView
    /// </summary>
    public static readonly DependencyProperty SelectedViewProperty = DependencyProperty.Register(
        nameof(SelectedView),
        typeof(IXamlDocumentView),
        typeof(DocumentsView),
        new FrameworkPropertyMetadata(default(IXamlDocumentView?)));

    #endregion

    #region Event Handlers

    private IXamlDocumentView? _view;

    private void DocumentViewLoaded(object sender, RoutedEventArgs e)
    {
        _view = (IXamlDocumentView)sender;

        if (SelectedDocument == _view.XamlDocument)
        {
            SelectedView = _view;
            KaxamlInfo.Editor = SelectedView.TextEditor;
        }
    }

    private void KaxamlInfo_ParseRequested()
    {
        if (SelectedView != null) SelectedView.Parse();
    }

    #endregion
}