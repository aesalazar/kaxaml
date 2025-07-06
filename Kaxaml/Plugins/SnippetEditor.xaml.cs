using System;
using System.Windows;
using System.Windows.Input;
using Kaxaml.Plugins.Default;

namespace Kaxaml.Plugins;

/// <summary>
/// Interaction logic for SnippetEditor.xaml
/// </summary>
public partial class SnippetEditor : Window
{
    /// <summary>
    /// DependencyProperty for Snippet
    /// </summary>
    public static readonly DependencyProperty SnippetProperty =
        DependencyProperty.Register(nameof(Snippet), typeof(Snippet), typeof(SnippetEditor),
            new FrameworkPropertyMetadata(default(Snippet), SnippetChanged));

    private static SnippetEditor? _instance;

    public SnippetEditor()
    {
        InitializeComponent();
    }

    public static SnippetEditor Show(Snippet s, Window owner)
    {
        if (_instance == null) _instance = new SnippetEditor();

        _instance.Owner = owner;
        _instance.Snippet = s;
        _instance.Show();

        return _instance;
    }

    private void DoDone(object sender, RoutedEventArgs e)
    {
        FocusManager.SetFocusedElement(this, null);
        Hide();

        RaiseCommitValuesEvent(this);

        _instance = null;
    }

    private void DoCancel(object sender, RoutedEventArgs e)
    {
        Snippet.Name = _name;
        Snippet.Shortcut = _shortcut;
        Snippet.Text = _text;

        Hide();
        _instance = null;
    }

    protected override void OnClosed(EventArgs e)
    {
        _instance = null;
    }

    #region fields

    private string _name = "";
    private string _shortcut = "";
    private string _text = "";

    #endregion

    #region Snippet (DependencyProperty)

    /// <summary>
    /// The snippet being edited
    /// </summary>
    public Snippet Snippet
    {
        get => (Snippet)GetValue(SnippetProperty);
        set => SetValue(SnippetProperty, value);
    }


    /// <summary>
    /// PropertyChangedCallback for Snippet
    /// </summary>
    private static void SnippetChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
    {
        if (obj is SnippetEditor owner)
            // create a local backup of the editable properties (for the cancel operation)
            if (args.NewValue is Snippet s)
            {
                owner._name = s.Name;
                owner._shortcut = s.Shortcut;
                owner._text = s.Text;
            }
    }

    #endregion

    #region RoutedEvent Helper Methods

    /// <summary>
    /// A static helper method to raise a routed event on a target UIElement or ContentElement.
    /// </summary>
    /// <param name="target">UIElement or ContentElement on which to raise the event</param>
    /// <param name="args">RoutedEventArgs to use when raising the event</param>
    private static void RaiseEvent(DependencyObject target, RoutedEventArgs args)
    {
        if (target is UIElement element)
            element.RaiseEvent(args);
        else if (target is ContentElement contentElement) contentElement.RaiseEvent(args);
    }

    /// <summary>
    /// A static helper method that adds a handler for a routed event 
    /// to a target UIElement or ContentElement.
    /// </summary>
    /// <param name="element">UIElement or ContentElement that listens to the event</param>
    /// <param name="routedEvent">Event that will be handled</param>
    /// <param name="handler">Event handler to be added</param>
    private static void AddHandler(DependencyObject element, RoutedEvent routedEvent, Delegate handler)
    {
        if (element is UIElement uie)
        {
            uie.AddHandler(routedEvent, handler);
        }
        else
        {
            if (element is ContentElement ce) ce.AddHandler(routedEvent, handler);
        }
    }

    /// <summary>
    /// A static helper method that removes a handler for a routed event 
    /// from a target UIElement or ContentElement.
    /// </summary>
    /// <param name="element">UIElement or ContentElement that listens to the event</param>
    /// <param name="routedEvent">Event that will no longer be handled</param>
    /// <param name="handler">Event handler to be removed</param>
    private static void RemoveHandler(DependencyObject element, RoutedEvent routedEvent, Delegate handler)
    {
        if (element is UIElement uie)
        {
            uie.RemoveHandler(routedEvent, handler);
        }
        else
        {
            if (element is ContentElement ce) ce.RemoveHandler(routedEvent, handler);
        }
    }

    #endregion

    #region CommitValues

    /// <summary>
    /// CommitValues Routed Event
    /// </summary>
    public static readonly RoutedEvent CommitValuesEvent = EventManager.RegisterRoutedEvent("CommitValues",
        RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SnippetEditor));

    /// <summary>
    /// Occurs when ...
    /// </summary>
    public event RoutedEventHandler CommitValues
    {
        add => AddHandler(CommitValuesEvent, value);
        remove => RemoveHandler(CommitValuesEvent, value);
    }

    /// <summary>
    /// A helper method to raise the CommitValues event.
    /// </summary>
    protected RoutedEventArgs? RaiseCommitValuesEvent()
    {
        return RaiseCommitValuesEvent(this);
    }

    /// <summary>
    /// A static helper method to raise the CommitValues event on a target element.
    /// </summary>
    /// <param name="target">UIElement or ContentElement on which to raise the event</param>
    internal static RoutedEventArgs? RaiseCommitValuesEvent(UIElement? target)
    {
        if (target == null) return null;

        var args = new RoutedEventArgs
        {
            RoutedEvent = CommitValuesEvent
        };
        RaiseEvent(target, args);
        return args;
    }

    #endregion
}