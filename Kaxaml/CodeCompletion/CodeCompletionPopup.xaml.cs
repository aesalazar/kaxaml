using System;
using System.Collections;
using System.Windows;
using System.Windows.Input;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using Kaxaml.Properties;

namespace Kaxaml.CodeCompletion;

public partial class CodeCompletionPopup
{
    private bool _overrideForceAccept;

    public CodeCompletionPopup()
    {
        InitializeComponent();
    }

    private void DoubleClick(object sender, MouseButtonEventArgs e)
    {
        Accept(true);
    }

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
    public static readonly DependencyProperty CompletionItemsProperty = DependencyProperty.Register(
        nameof(CompletionItems),
        typeof(ArrayList),
        typeof(CodeCompletionPopup),
        new FrameworkPropertyMetadata(null));

    #endregion

    #region Methods

    public void SelectNext()
    {
        CompletionListBox.SelectNext();
        _overrideForceAccept = true;
    }

    public void SelectPrevious()
    {
        CompletionListBox.SelectPrevious();
        _overrideForceAccept = true;
    }

    public void PageDown()
    {
        CompletionListBox.PageDown();
        _overrideForceAccept = true;
    }

    public void PageUp()
    {
        CompletionListBox.PageUp();
        _overrideForceAccept = true;
    }

    public void Cancel()
    {
        RaiseResultProvidedEvent(null, "", false, true);
        Hide();
    }

    public static bool IsOpenSomewhere
    {
        get
        {
            if (_popup != null) return _popup.IsOpen;

            return false;
        }
    }

    public ICompletionData? SelectedItem
    {
        get
        {
            if (CompletionListBox.SelectedIndex > -1 && CompletionListBox.SelectedItem is ICompletionData data) return data;

            return null;
        }
    }

    public void Accept(bool force)
    {
        if (CompletionListBox.SelectedItem is ICompletionData item)
        {
            RaiseResultProvidedEvent(item, item.Text, force || _overrideForceAccept, false);
            Hide();
        }
    }

    public void DoSearch(string prefix)
    {
        if (IsOpen)
        {
            var index = SearchForItem(prefix);
            if (index >= 0) CompletionListBox.SelectedIndex = index;
            //FrameworkElement fe = CompletionListBox.ItemContainerGenerator.ContainerFromIndex(index) as FrameworkElement;
            //if (fe != null) fe.BringIntoView();
        }
    }

    private string _cuePrefix = "";

    public void CueSearch(string prefix)
    {
        _cuePrefix = prefix;
        Opened += CodeCompletionPopup_Opened;
    }

    private void CodeCompletionPopup_Opened(object? sender, EventArgs e)
    {
        Opened -= CodeCompletionPopup_Opened;
        if (_cuePrefix.Length > 0) DoSearch(_cuePrefix);
        _cuePrefix = "";
    }

    private int SearchForItem(string prefix)
    {
        var indexOfItem = -1;
        for (var i = 0; i < CompletionItems.Count; i++)
            if (CompletionItems[i] is ICompletionData data && data.Text.Length >= prefix.Length)
                if (data.Text.Substring(0, prefix.Length).ToLower().Equals(prefix.ToLower()))
                {
                    indexOfItem = i;
                    break;
                }

        //if (indexOfItem == -1)
        //{
        //    for (int i = 0; i < _LastIndex; i++)
        //    {
        //        if ((CompletionItems[i] as XmlCompletionData).Text.Length >= prefix.Length)
        //        {
        //            if ((CompletionItems[i] as XmlCompletionData).Text.Substring(0, prefix.Length).ToLowerInvariant().Equals(prefix.ToLowerInvariant()))
        //            {
        //                indexOfItem = i;
        //                break;
        //            }
        //        }
        //    }
        //}

        return indexOfItem;
    }

    public void Show()
    {
        //CurrentPrefix = "";
        IsOpen = true;
    }

    public void Hide()
    {
        IsOpen = false;
    }

    #endregion

    #region Events

    #region ResultProvidedEvent

    public static readonly RoutedEvent ResultProvidedEvent = EventManager.RegisterRoutedEvent("ResultProvided", RoutingStrategy.Bubble, typeof(EventHandler<ResultProvidedEventArgs>), typeof(CodeCompletionPopup));

    public event EventHandler<ResultProvidedEventArgs> ResultProvided
    {
        add => AddHandler(ResultProvidedEvent, value);
        remove => RemoveHandler(ResultProvidedEvent, value);
    }

    private void RaiseResultProvidedEvent(ICompletionData? item, string text, bool forcedAccept, bool cancelled)
    {
        var newEventArgs = new ResultProvidedEventArgs(ResultProvidedEvent, item, text, forcedAccept, cancelled);
        RaiseEvent(newEventArgs);
    }

    #endregion

    #endregion

    #region Static Show Methods (and Support Types)

    private static CodeCompletionPopup? _popup;

    public static CodeCompletionPopup Show(ArrayList items, Point p)
    {
        if (_popup == null) _popup = new CodeCompletionPopup();

        //popup.VerticalOffset = p.Y;
        //popup.HorizontalOffset = p.X;

        double font = Settings.Default.EditorFontSize;

        _popup.PlacementRectangle = new Rect(p.X, p.Y - font, 1, font);

        _popup.CompletionItems = items;

        if (items == null || items.Count == 0) return _popup;

        _popup.CompletionListBox.SelectedIndex = 0;
        _popup._overrideForceAccept = false;
        _popup.Show();

        return _popup;
    }

    #endregion
}

public class ResultProvidedEventArgs : RoutedEventArgs
{
    public ResultProvidedEventArgs(RoutedEvent routedEvent, ICompletionData? item, string text, bool forcedAccept, bool canceled)
    {
        Item = item;
        RoutedEvent = routedEvent;
        ForcedAccept = forcedAccept;
        Text = text;
        Canceled = canceled;
    }

    public bool ForcedAccept { get; set; }

    public ICompletionData? Item { get; set; }

    public string Text { get; set; }

    public bool Canceled { get; set; }
}