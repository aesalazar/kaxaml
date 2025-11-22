using System.Windows;
using System.Windows.Input;
using Kaxaml.CodeSnippets;

namespace Kaxaml.Plugins;

public partial class SnippetEditor
{
    /// <summary>
    /// DependencyProperty for Snippet
    /// </summary>
    public static readonly DependencyProperty SnippetProperty = DependencyProperty.Register(
        nameof(Snippet),
        typeof(Snippet),
        typeof(SnippetEditor),
        new FrameworkPropertyMetadata(default(Snippet), SnippetChanged));

    public SnippetEditor()
    {
        InitializeComponent();
    }

    public void Show(Snippet snippet)
    {
        Snippet = snippet;
        Show();
    }

    private void DoDone(object sender, RoutedEventArgs e)
    {
        FocusManager.SetFocusedElement(this, null);
        Hide();

        RaiseCommitValuesEvent(this);
    }

    private void DoCancel(object sender, RoutedEventArgs e)
    {
        Snippet.Name = _name;
        Snippet.Shortcut = _shortcut;
        Snippet.Text = _text;
        Hide();
    }

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

    #endregion

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

    #region CommitValues

    /// <inheritdoc cref="CommitValues" />
    public static readonly RoutedEvent CommitValuesEvent = EventManager.RegisterRoutedEvent(
        nameof(CommitValues),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(SnippetEditor));

    /// <summary>
    /// Fires when the value is saved.
    /// </summary>
    public event RoutedEventHandler CommitValues
    {
        add => AddHandler(CommitValuesEvent, value);
        remove => RemoveHandler(CommitValuesEvent, value);
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