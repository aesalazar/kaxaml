using System;
using System.Collections.ObjectModel;
using System.Windows;
using Kaxaml.Controls;

namespace Kaxaml.Documents;

/// <summary>
/// Manages a collection of XamlDocuments including a selection.
/// </summary>
/// <remarks>
/// This is essentially a ViewModel but do not want to introduce new MVVM dependencies until a
/// more concerted effort is made to change over everything at once.
/// </remarks>
public sealed class XamlDocumentManager : FrameworkElement
{
    /// <summary>
    /// Current collection of open Documents.
    /// </summary>
    public ObservableCollection<XamlDocument> XamlDocuments { get; } = [];

    #region SelectedXamlDocumentProperty

    /// <inheritdoc cref="SelectedXamlDocument"/>
    public static readonly DependencyProperty SelectedXamlDocumentProperty = DependencyProperty.Register(
        nameof(SelectedXamlDocument),
        typeof(XamlDocument),
        typeof(XamlDocumentManager),
        new PropertyMetadata(default(XamlDocument?), SelectedXamlDocumentChangedCallback));

    /// <summary>
    /// Currently selected Document, if any.
    /// </summary>
    public XamlDocument? SelectedXamlDocument
    {
        get => (XamlDocument?)GetValue(SelectedXamlDocumentProperty);
        set => SetValue(SelectedXamlDocumentProperty, value);
    }

    private static void SelectedXamlDocumentChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var manager = (XamlDocumentManager)d;
        manager.RaiseSelectedXamlDocumentChanged();
    }

    #endregion

    #region SelectedXamlDocumentChangedEvent

    /// <inheritdoc cref="SelectedXamlDocumentChanged"/>
    public static readonly RoutedEvent SelectedXamlDocumentChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(SelectedXamlDocumentChanged),
        RoutingStrategy.Bubble,
        typeof(EventHandler),
        typeof(KaxamlTextEditor));

    /// <summary>
    /// Fires when the selected document is changed.
    /// </summary>
    public event EventHandler SelectedXamlDocumentChanged
    {
        add => AddHandler(SelectedXamlDocumentChangedEvent, value);
        remove => RemoveHandler(SelectedXamlDocumentChangedEvent, value);
    }

    private void RaiseSelectedXamlDocumentChanged()
    {
        RaiseEvent(new RoutedEventArgs(SelectedXamlDocumentChangedEvent, this));
    }

    #endregion
}