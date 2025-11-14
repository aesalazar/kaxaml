using System;
using System.Windows;
using System.Windows.Controls;
using Kaxaml.Documents;
using Kaxaml.DocumentViews;
using KaxamlPlugins;
using KaxamlPlugins.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Kaxaml.Views;

public partial class DocumentsView
{
    #region Constructors

    public DocumentsView()
    {
        KaxamlInfo.ParseRequested += KaxamlInfo_ParseRequested;
        XamlDocumentManager = ApplicationDiServiceProvider.Services.GetRequiredService<XamlDocumentManager>();
        XamlDocumentManager.SelectedXamlDocumentChanged += XamlDocumentManager_OnSelectedXamlDocumentChanged;
        InitializeComponent();
    }

    #endregion Constructors

    public XamlDocumentManager XamlDocumentManager { get; }

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

        if (XamlDocumentManager.SelectedXamlDocument == _view.XamlDocument)
        {
            SelectedView = _view;
            KaxamlInfo.Editor = SelectedView.TextEditor;
        }
    }

    private void KaxamlInfo_ParseRequested() => SelectedView?.Parse();

    private void XamlDocumentManager_OnSelectedXamlDocumentChanged(object? _, EventArgs __)
    {
        var listBoxItem = (ListBoxItem)ContentListBox
            .ItemContainerGenerator
            .ContainerFromItem(XamlDocumentManager.SelectedXamlDocument);

        var view = (IXamlDocumentView?)listBoxItem?
            .Template
            .FindName("PART_DocumentView", listBoxItem);

        if (view != null)
        {
            _view = view;
            SelectedView = _view;
            view.OnActivate();
            KaxamlInfo.Editor = SelectedView.TextEditor;
        }
    }

    #endregion
}