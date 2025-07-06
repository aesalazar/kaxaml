using System;
using System.Collections;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using Kaxaml.CodeCompletion;
using Kaxaml.Plugins.Default;
using KaxamlPlugins;

namespace Kaxaml.Controls
{
    /// <summary>
    /// Interaction logic for KaxamlTextEditor.xaml
    /// </summary>

    public partial class KaxamlTextEditor : IKaxamlInfoTextEditor
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        public static int Counter;

        public KaxamlTextEditor()
        {
            Counter++;
            Debug.WriteLine("counter: " + Counter);

            // enable the system theme for WinForms controls
            System.Windows.Forms.Application.EnableVisualStyles();

            InitializeComponent();

            // capture text changed events from the editor
            TextEditor.Document.DocumentChanged += TextEditorDocumentChanged;

            // create a key handler that we will use to activate code completion
            TextEditor.ActiveTextAreaControl.TextArea.KeyEventHandler += ProcessText;

            // register to process keys for our code completion dialog
            TextEditor.ActiveTextAreaControl.TextArea.DoProcessDialogKey += ProcessKeys;

            // register to get an event when selection changed
            TextEditor.ActiveTextAreaControl.TextArea.SelectionManager.SelectionChanged += SelectionManager_SelectionChanged;

            // register to get an event when the caret position changed
            TextEditor.ActiveTextAreaControl.Caret.PositionChanged += Caret_PositionChanged;

            // register for an event when the WinFormsHost gets deactivated
            //FormsHost.MessageHook += new System.Windows.Interop.HwndSourceHook(FormsHost_MessageHook);

            // register the ShowSnippets command
            var binding = new CommandBinding(ShowSnippetsCommand);
            binding.Executed += ShowSnippets_Executed;
            binding.CanExecute += ShowSnippets_CanExecute;
            InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.Down, ModifierKeys.Alt)));
            CommandBindings.Add(binding);
        }

        private void Caret_PositionChanged(object? _, EventArgs __)
        {
            LineNumber = TextEditor.ActiveTextAreaControl.Caret.Position.Y;
            LinePosition = TextEditor.ActiveTextAreaControl.Caret.Position.X;
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Dependency Properties
        //
        //-------------------------------------------------------------------

        #region LineNumber (DependencyProperty)

        /// <summary>
        /// The current line number of the caret.
        /// </summary>
        public int LineNumber
        {
            get => (int)GetValue(LineNumberProperty); 
            set => SetValue(LineNumberProperty, value);
        }

        /// <summary>
        /// DependencyProperty for LineNumber
        /// </summary>
        public static readonly DependencyProperty LineNumberProperty = DependencyProperty.Register(
            nameof(LineNumber), 
            typeof(int), 
            typeof(KaxamlTextEditor), 
            new FrameworkPropertyMetadata(default(int), LineNumberChanged));

        /// <summary>
        /// PropertyChangedCallback for LineNumber
        /// </summary>
        private static void LineNumberChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj is KaxamlTextEditor owner)
            {
                owner.TextEditor.ActiveTextAreaControl.Caret.Line = (int)args.NewValue;
            }
        }
        #endregion

        #region LinePosition (DependencyProperty)

        /// <summary>
        /// The current line position of the caret.
        /// </summary>
        public int LinePosition
        {
            get => (int)GetValue(LinePositionProperty); 
            set => SetValue(LinePositionProperty, value);
        }

        /// <summary>
        /// DependencyProperty for LinePosition
        /// </summary>
        public static readonly DependencyProperty LinePositionProperty = DependencyProperty.Register(
            nameof(LinePosition), 
            typeof(int), 
            typeof(KaxamlTextEditor), 
            new FrameworkPropertyMetadata(default(int), LinePositionChanged));

        /// <summary>
        /// PropertyChangedCallback for LinePosition
        /// </summary>
        private static void LinePositionChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj is KaxamlTextEditor owner)
            {
                owner.TextEditor.ActiveTextAreaControl.Caret.Position = new TextLocation((int)args.NewValue, owner.TextEditor.ActiveTextAreaControl.Caret.Position.Y);
            }
        }

        #endregion

        #region ShowLineNumbers (DependencyProperty)

        /// <summary>
        /// description of ShowLineNumbers
        /// </summary>
        public bool ShowLineNumbers
        {
            get => (bool)GetValue(ShowLineNumbersProperty);
            set => SetValue(ShowLineNumbersProperty, value);
        }

        /// <summary>
        /// DependencyProperty for ShowLineNumbers
        /// </summary>
        public static readonly DependencyProperty ShowLineNumbersProperty = DependencyProperty.Register(
            nameof(ShowLineNumbers), 
            typeof(bool), 
            typeof(KaxamlTextEditor), 
            new FrameworkPropertyMetadata(default(bool), ShowLineNumbersChanged));

        /// <summary>
        /// PropertyChangedCallback for ShowLineNumbers
        /// </summary>
        private static void ShowLineNumbersChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj is KaxamlTextEditor owner)
            {
                owner.TextEditor.ShowLineNumbers = (bool)args.NewValue;
            }
        }

        #endregion

        #region FontFamily (DependencyProperty)

        /// <summary>
        /// The FontFamily associated with text in the TextEditor.
        /// </summary>
        public string FontFamilyName
        {
            get => (string)GetValue(FontFamilyPropertyName);
            set => SetValue(FontFamilyPropertyName, value);
        }

        /// <summary>
        /// DependencyProperty for FontFamily
        /// </summary>
        public static readonly DependencyProperty FontFamilyPropertyName = DependencyProperty.Register(
            nameof(FontFamilyName),
            typeof(string),
            typeof(KaxamlTextEditor),
            new FrameworkPropertyMetadata("Courier New", FontFamilyChanged));

        /// <summary>
        /// PropertyChangedCallback for FontFamily
        /// </summary>
        private static void FontFamilyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj is KaxamlTextEditor owner)
            {
                owner.TextEditor.Font = new System.Drawing.Font((string)args.NewValue, owner.FontSize);
            }
        }

        #endregion

        #region FontSize (DependencyProperty)

        /// <summary>
        /// The size of the text in the TextEditor.
        /// </summary>
        public new float FontSize
        {
            get => (float)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        /// <summary>
        /// DependencyProperty for FontSize
        /// </summary>
        public new static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
            nameof(FontSize), 
            typeof(float), 
            typeof(KaxamlTextEditor), 
            new FrameworkPropertyMetadata((float)1, FontSizeChanged));

        /// <summary>
        /// PropertyChangedCallback for FontSize
        /// </summary>
        private static void FontSizeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj is KaxamlTextEditor owner)
            {
                owner.TextEditor.Font = new System.Drawing.Font(owner.FontFamilyName, (float)args.NewValue);
            }
        }

        #endregion

        #region Text (DependencyProperty)

        /// <summary>
        /// description of Text
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty); 
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// DependencyProperty for Text
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), 
            typeof(string), 
            typeof(KaxamlTextEditor), 
            new FrameworkPropertyMetadata(default(string), TextPropertyChanged));

        /// <summary>
        /// PropertyChangedCallback for Text
        /// </summary>
        private static void TextPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj is KaxamlTextEditor owner)
            {
                var newValue = " ";
                if (!string.IsNullOrEmpty((string)args.NewValue)) newValue = (string)args.NewValue;

                if (!owner._setTextInternal)
                {
                    owner._resetTextInternal = true;
                    owner.TextEditor.ResetText();
                    owner.TextEditor.Refresh();
                    owner.TextEditor.Text = newValue;
                    owner._resetTextInternal = false;
                }

                if (!owner._resetTextInternal)
                {
                    owner.RaiseTextChangedEvent(newValue);
                }
            }
        }

        #endregion

        #region IsCodeCompletionEnabled (DependencyProperty)

        /// <summary>
        /// description of the property
        /// </summary>
        public bool IsCodeCompletionEnabled
        {
            get => (bool)GetValue(IsCodeCompletionEnabledProperty);
            set => SetValue(IsCodeCompletionEnabledProperty, value);
        }

        /// <summary>
        /// DependencyProperty for IsCodeCompletionEnabled
        /// </summary>
        public static readonly DependencyProperty IsCodeCompletionEnabledProperty = DependencyProperty.Register(
            nameof(IsCodeCompletionEnabled), 
            typeof(bool),
            typeof(KaxamlTextEditor), 
            new FrameworkPropertyMetadata(true));

        #endregion

        #region ConvertTabs (DependencyProperty)

        /// <summary>
        /// If true then tabs will be converted to spaces.
        /// </summary>
        public bool ConvertTabs
        {
            get => (bool)GetValue(ConvertTabsProperty); 
            set => SetValue(ConvertTabsProperty, value);
        }

        /// <summary>
        /// DependencyProperty for ConvertTabs
        /// </summary>
        public static readonly DependencyProperty ConvertTabsProperty = DependencyProperty.Register(
            nameof(ConvertTabs), 
            typeof(bool), 
            typeof(KaxamlTextEditor), 
            new FrameworkPropertyMetadata(default(bool), ConvertTabsChanged));

        /// <summary>
        /// PropertyChangedCallback for ConvertTabs
        /// </summary>
        private static void ConvertTabsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj is KaxamlTextEditor owner)
            {
                owner.TextEditor.ConvertTabsToSpaces = (bool)args.NewValue;
            }
        }

        #endregion

        #region EnableXmlFolding (DependencyProperty)

        private DispatcherTimer? _foldingTimer;

        /// <summary>
        /// Enabled XML nodes to be collapsed when true
        /// </summary>
        public bool EnableXmlFolding
        {
            get => (bool)GetValue(EnableXmlFoldingProperty); 
            set => SetValue(EnableXmlFoldingProperty, value);
        }

        /// <summary>
        /// DependencyProperty for EnableXmlFolding
        /// </summary>
        public static readonly DependencyProperty EnableXmlFoldingProperty = DependencyProperty.Register(
            nameof(EnableXmlFolding), 
            typeof(bool), 
            typeof(KaxamlTextEditor), 
            new FrameworkPropertyMetadata(default(bool), EnableXmlFoldingChanged));

        /// <summary>
        /// PropertyChangedCallback for EnableXmlFolding
        /// </summary>
        private static void EnableXmlFoldingChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj is KaxamlTextEditor owner)
            {
                if ((bool)args.NewValue)
                {
                    // set the folding strategy
                    if (owner.TextEditor.Document.FoldingManager.FoldingStrategy == null)
                    {
                        owner.TextEditor.Document.FoldingManager.FoldingStrategy = new XmlFoldingStrategy();
                    }

                    // create a timer to update the folding every second
                    if (owner._foldingTimer == null)
                    {
                        owner._foldingTimer = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(1000)
                        };
                        owner._foldingTimer.Tick += owner.FoldingTimerTick;
                    }

                    // enable folding and start the timer
                    owner.TextEditor.EnableFolding = true;
                    owner._foldingTimer.Start();
                }
                else
                {
                    // disable folding and start the timer
                    owner.TextEditor.EnableFolding = false;
                    owner._foldingTimer?.Stop();
                }
            }
        }

        #endregion

        #region EnableSyntaxHighlighting (DependencyProperty)

        /// <summary>
        /// Enables syntax highlighting when true.
        /// </summary>
        public bool EnableSyntaxHighlighting
        {
            get => (bool)GetValue(EnableSyntaxHighlightingProperty);
            set => SetValue(EnableSyntaxHighlightingProperty, value);
        }

        /// <summary>
        /// DependencyProperty for EnableSyntaxHighlighting
        /// </summary>
        public static readonly DependencyProperty EnableSyntaxHighlightingProperty = DependencyProperty.Register(
            nameof(EnableSyntaxHighlighting), 
            typeof(bool), 
            typeof(KaxamlTextEditor), 
            new FrameworkPropertyMetadata(default(bool), EnableSyntaxHighlightingChanged));

        /// <summary>
        /// PropertyChangedCallback for EnableSyntaxHighlighting
        /// </summary>
        private static void EnableSyntaxHighlightingChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj is KaxamlTextEditor owner)
            {
                if ((bool)args.NewValue)
                {
                    // set the highlighting strategy
                    owner.TextEditor.Document.HighlightingStrategy = HighlightingManager.Manager.FindHighlighter("XML");
                }
                else
                {
                    owner.TextEditor.Document.HighlightingStrategy = HighlightingManager.Manager.FindHighlighter("None");
                }
            }
        }

        #endregion

        #region ConvertTabsCount (DependencyProperty)

        /// <summary>
        /// The width of a tab in spaces.
        /// </summary>
        public int ConvertTabsCount
        {
            get => (int)GetValue(ConvertTabsCountProperty);
            set => SetValue(ConvertTabsCountProperty, value);
        }

        /// <summary>
        /// DependencyProperty for ConvertTabsCount
        /// </summary>
        public static readonly DependencyProperty ConvertTabsCountProperty = DependencyProperty.Register(
            nameof(ConvertTabsCount), 
            typeof(int), 
            typeof(KaxamlTextEditor), 
            new FrameworkPropertyMetadata(default(int), ConvertTabsCountChanged));

        /// <summary>
        /// PropertyChangedCallback for ConvertTabsCount
        /// </summary>
        private static void ConvertTabsCountChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj is KaxamlTextEditor owner)
            {
                owner.TextEditor.TabIndent = (int)args.NewValue;
                owner.TextEditor.TextEditorProperties.IndentationSize = (int)args.NewValue;
            }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Commands
        //
        //-------------------------------------------------------------------

        #region Commands

        #region ShowSnippetsCommand

        public static readonly RoutedUICommand ShowSnippetsCommand = new("Show Snippets", "ShowSnippetsCommand", typeof(KaxamlTextEditor));

        private void ShowSnippets_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (Equals(sender, this))
            {
                ShowCompletionWindow(char.MaxValue);
            }
        }

        private void ShowSnippets_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = Equals(sender, this);
        }

        #endregion

        #endregion

        //-------------------------------------------------------------------
        //
        //  Properties
        //
        //-------------------------------------------------------------------

        #region Properties

        public int CaretIndex
        {
            get => TextEditor.ActiveTextAreaControl.TextArea.Caret.Offset; 
            set => TextEditor.ActiveTextAreaControl.Caret.Position = TextEditor.Document.OffsetToPosition(value);
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Events
        //
        //-------------------------------------------------------------------

        #region TextChangedEvent

        public static readonly RoutedEvent TextChangedEvent = EventManager.RegisterRoutedEvent(
            nameof(TextChanged), 
            RoutingStrategy.Bubble, 
            typeof(EventHandler<TextChangedEventArgs>),
            typeof(KaxamlTextEditor));

        public event EventHandler<TextChangedEventArgs> TextChanged
        {
            add => AddHandler(TextChangedEvent, value); 
            remove => RemoveHandler(TextChangedEvent, value);
        }

        private void RaiseTextChangedEvent(string text)
        {
            var newEventArgs = new TextChangedEventArgs(TextChangedEvent, text);
            RaiseEvent(newEventArgs);
        }

        #endregion

        #region TextSelectionChangedEvent

        private void SelectionManager_SelectionChanged(object? _, EventArgs __)
        {
            RaiseTextSelectionChangedEvent();
        }

        public static readonly RoutedEvent TextSelectionChangedEvent = EventManager.RegisterRoutedEvent(
            nameof(TextSelectionChanged), 
            RoutingStrategy.Bubble, 
            typeof(RoutedEventHandler), 
            typeof(KaxamlTextEditor));

        public event RoutedEventHandler TextSelectionChanged
        {
            add => AddHandler(TextSelectionChangedEvent, value); 
            remove => RemoveHandler(TextSelectionChangedEvent, value);
        }

        private void RaiseTextSelectionChangedEvent()
        {
            var newEventArgs = new RoutedEventArgs(TextSelectionChangedEvent);
            RaiseEvent(newEventArgs);
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Event Handlers
        //
        //-------------------------------------------------------------------

        #region EventHandlers

        private bool _setTextInternal;
        private bool _resetTextInternal;

        private void TextEditorDocumentChanged(object sender, DocumentEventArgs e)
        {
            _setTextInternal = true;
            Text = e.Document.TextContent;
            _setTextInternal = false;
        }

        private void FoldingTimerTick(object? _, EventArgs __)
        {
            UpdateFolding();
        }


        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        private void UpdateFolding()
        {
            TextEditor.Document.FoldingManager.UpdateFoldings(string.Empty, new object());
            var area = TextEditor.ActiveTextAreaControl.TextArea;
            area.Refresh(area.FoldMargin);
        }

        /// <summary>
        /// Checks whether the caret is inside a set of quotes (" or ').
        /// </summary>
        private bool IsInsideQuotes(TextArea textArea)
        {
            var inside = false;

            var line = textArea.Document.GetLineSegment(textArea.Document.GetLineNumberForOffset(textArea.Caret.Offset));
            if (line != null)
            {
                if (line.Offset + line.Length > textArea.Caret.Offset &&
                    line.Offset < textArea.Caret.Offset)
                {

                    var charAfter = textArea.Document.GetCharAt(textArea.Caret.Offset);
                    var charBefore = textArea.Document.GetCharAt(textArea.Caret.Offset - 1);

                    if ((charBefore == '\'' && charAfter == '\'') ||
                        (charBefore == '\"' && charAfter == '\"'))
                    {
                        inside = true;
                    }
                }
            }

            return inside;
        }


        public void InsertCharacter(char ch)
        {
            TextEditor.ActiveTextAreaControl.TextArea.MotherTextEditorControl.BeginUpdate();

            switch (TextEditor.ActiveTextAreaControl.TextArea.Caret.CaretMode)
            {
                case CaretMode.InsertMode:
                    TextEditor.ActiveTextAreaControl.TextArea.InsertChar(ch);
                    break;
                case CaretMode.OverwriteMode:
                    TextEditor.ActiveTextAreaControl.TextArea.ReplaceChar(ch);
                    break;
            }
            var currentLineNr = TextEditor.ActiveTextAreaControl.TextArea.Caret.Line;
            TextEditor.Document.FormattingStrategy.FormatLine(TextEditor.ActiveTextAreaControl.TextArea, currentLineNr, TextEditor.Document.PositionToOffset(TextEditor.ActiveTextAreaControl.TextArea.Caret.Position), ch);

            TextEditor.ActiveTextAreaControl.TextArea.MotherTextEditorControl.EndUpdate();
        }

        public void InsertStringAtCaret(string s)
        {
            TextEditor.ActiveTextAreaControl.TextArea.MotherTextEditorControl.BeginUpdate();

            switch (TextEditor.ActiveTextAreaControl.TextArea.Caret.CaretMode)
            {
                case CaretMode.InsertMode:
                    TextEditor.ActiveTextAreaControl.TextArea.InsertString(s);
                    break;
                case CaretMode.OverwriteMode:
                    TextEditor.ActiveTextAreaControl.TextArea.InsertString(s);
                    break;
            }

            TextEditor.ActiveTextAreaControl.TextArea.MotherTextEditorControl.EndUpdate();
        }

        public void InsertString(string s, int offset)
        {
            TextEditor.ActiveTextAreaControl.TextArea.MotherTextEditorControl.BeginUpdate();

            TextEditor.ActiveTextAreaControl.TextArea.Document.Insert(offset, s);

            TextEditor.ActiveTextAreaControl.TextArea.MotherTextEditorControl.EndUpdate();
        }

        public void RemoveString(int beginIndex, int count)
        {
            TextEditor.ActiveTextAreaControl.TextArea.MotherTextEditorControl.BeginUpdate();

            TextEditor.ActiveTextAreaControl.TextArea.Document.Remove(beginIndex, count);

            if (CaretIndex > beginIndex)
            {
                TextEditor.ActiveTextAreaControl.TextArea.Caret.Column -= count;
            }

            TextEditor.ActiveTextAreaControl.TextArea.MotherTextEditorControl.EndUpdate();
        }

        #endregion

        #region Code Completion

        private static CodeCompletionPopup? _popup;

        private void CompleteTag()
        {
            var caret = TextEditor.ActiveTextAreaControl.Caret.Offset - 1;
            var begin = XmlParser.GetActiveElementStartIndex(Text, caret);
            var end = begin + 1;

            if (Text[caret - 1] == '/')
            {
                return;
            }

            var start = XmlParser.GetActiveElementStartIndex(Text, caret);

            // bail if we are either in a comment or if we are completing a "closing" tag
            if (Text[start + 1] == '/' || Text[start + 1] == '!') return;


            begin++;
            while (end < Text.Length && !char.IsWhiteSpace(Text[end]) && end < caret) end++;

            var column = TextEditor.ActiveTextAreaControl.Caret.Column;
            InsertStringAtCaret("</" + Text.Substring(begin, end - begin) + ">");

            TextEditor.ActiveTextAreaControl.Caret.Column = column;
            TextEditor.ActiveTextAreaControl.Caret.UpdateCaretPosition();
        }

        private bool _spaceIsValid = true;

        private bool ProcessText(char ch)
        {
            if (!IsCodeCompletionEnabled) return false;

            var currCaretIndex = TextEditor.ActiveTextAreaControl.Caret.Offset;

            if (XmlCompletionDataProvider.IsSchemaLoaded)
            {
                if (CodeCompletionPopup.IsOpenSomewhere)
                {
                    if (char.IsLetterOrDigit(ch))
                    {
                        //popup.AppendChar(ch);
                        InsertCharacter(ch);
                        currCaretIndex = TextEditor.ActiveTextAreaControl.Caret.Offset;

                        if (currCaretIndex > _beginCaretIndex)
                        {
                            var prefix = Text.Substring(_beginCaretIndex, currCaretIndex - _beginCaretIndex);
                            _popup?.DoSearch(prefix);
                        }

                        return true;
                    }

                    switch (ch)
                    {
                        case '>':
                            _popup?.Accept(false);
                            InsertCharacter(ch);
                            CompleteTag();
                            return true;

                        case ' ':
                            if (_spaceIsValid)
                            {
                                _popup?.Accept(false);
                                InsertCharacter(ch);
                                ShowCompletionWindow(ch);
                                return true;
                            }
                            return false;

                        case '=':
                            _popup?.Accept(false);
                            InsertCharacter(ch);
                            var column = TextEditor.ActiveTextAreaControl.TextArea.Caret.Column + 1;
                            InsertStringAtCaret("\"\"");
                            TextEditor.ActiveTextAreaControl.TextArea.Caret.Column = column;
                            return true;

                        case '/':
                            _popup?.Cancel();
                            InsertCharacter(ch);
                            return true;

                    }
                }
                else
                {

                    switch (ch)
                    {
                        case '<':
                            InsertCharacter(ch);
                            ShowCompletionWindow(ch);
                            return true;

                        case ' ':
                            InsertCharacter(ch);
                            ShowCompletionWindow(ch);
                            return true;

                        case '>':
                            InsertCharacter(ch);
                            CompleteTag();
                            return true;

                        case '.':
                            if (XmlParser.IsInsideXmlTag(Text, currCaretIndex))
                            {
                                if (_popup != null)
                                {
                                    var startColumn = TextEditor.ActiveTextAreaControl.TextArea.Caret.Column;
                                    var restoreColumn = startColumn + 1;
                                    var lineOffset = TextEditor.ActiveTextAreaControl.TextArea.Caret.Offset - startColumn;

                                    while (true)
                                    {
                                        startColumn--;

                                        if (Text[lineOffset + startColumn] == '<') break;
                                        if (startColumn <= 0) return false;
                                        if (!char.IsLetterOrDigit(Text[lineOffset + startColumn])) return false;
                                    }

                                    InsertCharacter(ch);
                                    currCaretIndex++;

                                    TextEditor.ActiveTextAreaControl.TextArea.Caret.Column = startColumn + 1;
                                    ShowCompletionWindow('<', XmlParser.GetActiveElementStartIndex(Text, currCaretIndex) + 1);

                                    TextEditor.ActiveTextAreaControl.TextArea.Caret.Column = restoreColumn;

                                    var prefix = Text.Substring(_beginCaretIndex, currCaretIndex - _beginCaretIndex);
                                    _popup.CueSearch(prefix);

                                    return true;
                                }

                            }
                            break;

                        case '=':
                            if (!XmlParser.IsInsideAttributeValue(Text, currCaretIndex))
                            {
                                InsertCharacter(ch);
                                var column = TextEditor.ActiveTextAreaControl.TextArea.Caret.Column + 1;
                                InsertStringAtCaret("\"\"");
                                TextEditor.ActiveTextAreaControl.TextArea.Caret.Column = column;
                                ShowCompletionWindow('\"');
                                return true;
                            }
                            break;


                        default:
                            if (XmlParser.IsAttributeValueChar(ch))
                            {
                                if (IsInsideQuotes(TextEditor.ActiveTextAreaControl.TextArea))
                                {
                                    // Have to insert the character ourselves since
                                    // it is not actually inserted yet.  If it is not
                                    // inserted now the code completion will not work
                                    // since the completion data provider attempts to
                                    // include the key typed as the pre-selected text.
                                    InsertCharacter(ch);
                                    ShowCompletionWindow(ch);
                                    return true;
                                }
                            }
                            break;
                    }
                }
            }
            return false;
        }

        private bool ProcessKeys(Keys keyData)
        {
            if (!IsCodeCompletionEnabled) return false;
            // return true to suppress the keystroke before the TextArea can handle it

            if (CodeCompletionPopup.IsOpenSomewhere)
            {
                if (keyData != Keys.Space) _spaceIsValid = true;

                if (keyData == Keys.Down)
                {
                    _popup?.SelectNext();
                    return true;
                }
                if (keyData == Keys.PageDown)
                {
                    _popup?.PageDown();
                    return true;
                }
                if (keyData == Keys.Up)
                {
                    _popup?.SelectPrevious();
                    return true;
                }
                if (keyData == Keys.PageUp)
                {
                    _popup?.PageUp();
                    return true;
                }
                if (keyData is Keys.Return or Keys.Tab)
                {
                    // if the selected item is an attribute, then we want to automatically insert the equals sign and quotes
                    //if (popup.SelectedItem.

                    _popup?.Accept(false);

                    return true;
                }
                if (keyData is Keys.Escape or Keys.Left or Keys.Delete)
                {
                    _popup?.Cancel();
                    return true;
                }
                if (keyData == Keys.Back)
                {

                    var currCaretIndex = TextEditor.ActiveTextAreaControl.Caret.Offset;

                    if (currCaretIndex > _beginCaretIndex)
                    {
                        var prefix = Text.Substring(_beginCaretIndex, currCaretIndex - _beginCaretIndex);
                        _popup?.DoSearch(prefix);
                    }
                    else
                    {
                        _popup?.Cancel();
                    }
                }
            }

            return false;
        }

        private int _beginCaretIndex;

        private delegate void OneArgDelegate(object arg);
        public void ShowCompletionWindowUi(object param)
        {
            _spaceIsValid = false;
            if (param is ArrayList items)
            {
                var mainWindow = System.Windows.Application.Current.MainWindow;

                double borderX = 0; // mainWindow.ActualWidth - (mainWindow.Content as FrameworkElement).ActualWidth;
                var borderY = mainWindow.ActualHeight - ((FrameworkElement)mainWindow.Content).ActualHeight;

                var editorPoint = TextEditor.PointToScreen(new System.Drawing.Point(0, 0));
                var caretPoint = TextEditor.ActiveTextAreaControl.Caret.ScreenPosition;

                _popup = CodeCompletionPopup.Show(items, new Point(editorPoint.X + caretPoint.X + borderX, editorPoint.Y + caretPoint.Y + FontSize * 1.3 + 3));
                _popup.ResultProvided += w_ResultProvided;
            }
        }

        private void ShowCompletionWindow(object param)
        {
            ShowCompletionWindow(param, TextEditor.ActiveTextAreaControl.Caret.Offset);
        }

        private void ShowCompletionWindow(object param, int beginCaretIndex)
        {
            if (!Properties.Settings.Default.EnableCodeCompletion) return;

            if (!CodeCompletionPopup.IsOpenSomewhere)
            {
                _beginCaretIndex = beginCaretIndex;

                if (param is char ch)
                {
                    if (IsCodeCompletionEnabled)
                    {
                        if (ch == char.MaxValue)
                        {
                            if (((App)System.Windows.Application.Current).Snippets != null)
                            {
                                var s = ((App)System.Windows.Application.Current).Snippets?.GetSnippetCompletionItems();

                                if (s?.Count > 0)
                                {
                                    Dispatcher.BeginInvoke(
                                        DispatcherPriority.ApplicationIdle,
                                        new OneArgDelegate(ShowCompletionWindowUi),
                                        s);
                                }
                            }
                        }
                        else
                        {
                            var completionDataProvider = new XmlCompletionDataProvider();

                            ICollection c = completionDataProvider.GenerateCompletionData("", TextEditor.ActiveTextAreaControl.TextArea, ch);

                            var items = new ArrayList(c);
                            items.Sort();

                            Dispatcher.BeginInvoke(
                                DispatcherPriority.ApplicationIdle,
                                new OneArgDelegate(ShowCompletionWindowUi),
                                items);
                        }
                    }
                }
            }
        }

        private void w_ResultProvided(object? _, ResultProvidedEventArgs e)
        {
            // remove the event handler
            _popup!.ResultProvided -= w_ResultProvided;

            if (!e.Canceled)
            {
                var currCaretIndex = TextEditor.ActiveTextAreaControl.Caret.Offset;
                var inputLength = currCaretIndex - _beginCaretIndex;
                if (inputLength < 0) inputLength = 0;

                var indentedText = "";

                if (e.Item is SnippetCompletionData snippet)
                {
                    TextEditor.ActiveTextAreaControl.Caret.ValidateCaretPos();
                    indentedText = snippet.Snippet.IndentedText(TextEditor.ActiveTextAreaControl.Caret.Position.X - inputLength, true);
                }

                if (e.Item is XmlCompletionData xml)
                {
                    indentedText = xml.Text;
                }

                if (currCaretIndex > _beginCaretIndex)
                {

                    var prefix = Text.Substring(_beginCaretIndex, currCaretIndex - _beginCaretIndex);
                    if (e.ForcedAccept || e.Text.ToLowerInvariant().StartsWith(prefix.ToLowerInvariant()))
                    {
                        // clear the user entered text
                        RemoveString(_beginCaretIndex, currCaretIndex - _beginCaretIndex);

                        // insert the selected string
                        InsertString(indentedText, _beginCaretIndex);

                        // place the caret at the end of the inserted text
                        TextEditor.ActiveTextAreaControl.TextArea.Caret.Column += indentedText.Length;
                    }
                }
                else if (currCaretIndex - _beginCaretIndex == 0)
                {
                    InsertStringAtCaret(indentedText);
                }
            }
        }

        #endregion

        #region Find and Replace

        public void ReplaceSelectedText(string s)
        {
            {
                var offset = TextEditor.ActiveTextAreaControl.SelectionManager.SelectionCollection[0].Offset;
                var length = TextEditor.ActiveTextAreaControl.SelectionManager.SelectionCollection[0].Length;

                RemoveString(offset, length);
                InsertString(s, offset);

                SetSelection(offset, offset + s.Length, true);
            }
        }

        public string SelectedText => TextEditor.ActiveTextAreaControl.SelectionManager.SelectedText;

        public void SetSelection(int fromOffset, int toOffset, bool suppressSelectionChangedEvent)
        {
            try
            {
                var from = TextEditor.Document.OffsetToPosition(fromOffset);
                var to = TextEditor.Document.OffsetToPosition(toOffset);

                if (suppressSelectionChangedEvent)
                {
                    TextEditor.ActiveTextAreaControl.TextArea.SelectionManager.SelectionChanged -= SelectionManager_SelectionChanged;
                    TextEditor.ActiveTextAreaControl.SelectionManager.SetSelection(from, to);
                    TextEditor.ActiveTextAreaControl.TextArea.SelectionManager.SelectionChanged += SelectionManager_SelectionChanged;
                }
                else
                {
                    TextEditor.ActiveTextAreaControl.SelectionManager.SetSelection(from, to);
                }
            }
            catch (Exception ex)
            {
                if (ex.IsCriticalException())
                {
                    throw;
                }
            }
        }

        public void SelectLine(int lineNumber)
        {

            try
            {
                var startPoint = new TextLocation(0, lineNumber);
                var endPoint = new TextLocation(0, lineNumber + 1);

                TextEditor.ActiveTextAreaControl.SelectionManager.SetSelection(startPoint, endPoint);
                TextEditor.ActiveTextAreaControl.Caret.Position = new TextLocation(0, lineNumber);
            }
            catch (Exception ex)
            {
                if (ex.IsCriticalException())
                {
                    throw;
                }
            }
        }

        private string _findText = "";
        private int _findIndex;

        public void Find(string s)
        {
            _findText = s;
            _findIndex = CaretIndex;

            if (string.Equals(SelectedText, _findText, StringComparison.InvariantCultureIgnoreCase))
            {
                _findIndex = CaretIndex + 1;
            }

            FindNext();
        }

        public void FindNext()
        {
            FindNext(true);
        }

        public void FindNext(bool allowStartFromTop)
        {
            var index = Text.ToUpperInvariant().IndexOf(_findText.ToUpperInvariant(), _findIndex, StringComparison.Ordinal);
            if (index > 0)
            {
                SetSelection(index, index + _findText.Length, false);
                _findIndex = index + 1;
                CaretIndex = index;

                Focus();
                TextEditor.Select();
            }
            else if (allowStartFromTop)
            {
                _findIndex = 0;
                FindNext(false);
            }
            else
            {
                System.Windows.MessageBox.Show("The text \"" + _findText + "\" could not be found.");
                _findIndex = 0;
            }
        }

        public void Replace(string s, string replacement, bool selectedonly)
        {
            if (selectedonly)
            {
                var c = CaretIndex;
                var sub = SelectedText;
                var r = ReplaceEx(sub, s, replacement);

                ReplaceSelectedText(r);
                
                //Make sure the text is not shorter as a result of the replacement
                CaretIndex = Math.Min(c, TextEditor.Document.TextLength);
            }
            else
            {
                var c = CaretIndex;
                var r = ReplaceEx(Text, s, replacement);
                Text = r;
                CaretIndex = c;
            }
        }

        private string ReplaceEx(string original, string pattern, string replacement)
        {
            var count = 0;
            var position0 = 0;
            int position1;

            var upperString = original.ToUpper();
            var upperPattern = pattern.ToUpper();

            var inc = original.Length / pattern.Length * (replacement.Length - pattern.Length);
            var chars = new char[original.Length + Math.Max(0, inc)];

            while ((position1 = upperString.IndexOf(upperPattern, position0, StringComparison.Ordinal)) != -1)
            {
                for (var i = position0; i < position1; ++i)
                    chars[count++] = original[i];
                for (var i = 0; i < replacement.Length; ++i)
                    chars[count++] = replacement[i];
                position0 = position1 + pattern.Length;
            }

            if (position0 == 0) return original;

            for (var i = position0; i < original.Length; ++i) chars[count++] = original[i];

            return new string(chars, 0, count);
        }


        public nint EditorHandle => TextEditor.Handle;

        public TextEditorControl HostedEditorControl => TextEditor;

        #endregion

        #region Undo/Redo

        public void Undo()
        {
            TextEditor.Undo();
        }

        public void Redo()
        {
            TextEditor.Redo();
        }

        #endregion
    }

    public class TextChangedEventArgs : RoutedEventArgs
    {
        public TextChangedEventArgs(RoutedEvent routedEvent, string text)
        {
            RoutedEvent = routedEvent;
            Text = text;
        }

        public string Text { get; set; }
    }
}