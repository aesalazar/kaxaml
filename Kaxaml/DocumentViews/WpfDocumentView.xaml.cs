using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Xaml;
using Kaxaml.CodeCompletion;
using Kaxaml.Controls;
using Kaxaml.Documents;
using Kaxaml.Properties;
using KaxamlPlugins;
using KaxamlPlugins.DependencyInjection;
using KaxamlPlugins.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TextChangedEventArgs = Kaxaml.Controls.TextChangedEventArgs;
using XamlParseException = System.Windows.Markup.XamlParseException;

namespace Kaxaml.DocumentViews;

public partial class WpfDocumentView : IXamlDocumentView
{
    #region Constructors

    public WpfDocumentView()
    {
        InitializeComponent();
        _logger = ApplicationDiServiceProvider.Services.GetRequiredService<ILogger<WpfDocumentView>>();
        _assemblyCacheManager = ApplicationDiServiceProvider.Services.GetRequiredService<AssemblyCacheManager>();

        KaxamlInfo.Frame = ContentArea;
        ContentArea.ContentRendered += ContentArea_ContentRendered;
        Dispatcher.UnhandledException += Dispatcher_UnhandledException;
        _assemblyCacheManager.CacheUpdated += AssemblyCacheManager_OnCacheUpdated;

        var schemaFile = Path.Combine(
            Path.GetDirectoryName(ApplicationDiServiceProvider.StartupPath + "\\")
            ?? throw new Exception("Could not determine Startup Path"),
            Settings.Default.WPFSchema);

        Dispatcher.InvokeAsync(() =>
        {
            var ex = XmlCompletionDataProvider.LoadSchema(schemaFile);
            if (ex is not null) _logger.LogError("Could not load Scheme File: {Ex}", ex);
        });

        _logger.LogInformation(
            "Constructed with call to load Schema path: {SchemaFile}",
            schemaFile);
    }

    #endregion Constructors

    #region Fields

    private static readonly IList<string> Colors = ["AliceBlue", "Aquamarine", "Azure", "Bisque", "BlanchedAlmond", "Burlywood", "CadetBlue", "Chartreuse", "Chocolate", "Coral", "CornflowerBlue", "Cornsilk", "DodgerBlue", "FloralWhite", "Gainsboro", "Ghostwhite", "Honeydew", "HotPink", "IndianRed", "LightSalmon", "Mintcream", "MistyRose", "Moccasin", "NavajoWhite", "Oldlace", "PapayaWhip", "PeachPuff", "Peru", "SaddleBrown", "Seashell", "Thistle", "Tomato", "WhiteSmoke"];

    private DispatcherTimer? _dispatcherTimer;

    private readonly ILogger _logger;

    private readonly AssemblyCacheManager _assemblyCacheManager;

    private bool _unhandledExceptionRaised;

    private readonly Brush _defaultBackgroundBrush = new SolidColorBrush(Color.FromRgb(0x40, 0x40, 0x40));

    private readonly Random _r = new();

    /// <summary>
    /// Indicates if is currently <see cref="Documents.XamlDocument.SourceText"/> is being parsed and reapplied.
    /// </summary>
    private bool _isSettingSourceText;

    /// <summary>
    /// Count of unprocessed UNDO events triggered by the user.
    /// </summary>
    private int _undoTriggerCount;

    /// <summary>
    /// Count of unprocessed REDO events triggered by the user.
    /// </summary>
    private int _redoTriggerCount;

    #endregion Fields

    #region Event Handlers

    private static void ContentArea_ContentRendered(object? _, EventArgs __)
    {
        KaxamlInfo.RaiseContentLoaded();
    }

    private void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // try to shut this down by killing the content and showing an
        // error page.  if it gets raised more than once between parses, then assume
        // it's fatal and shutdown the app

        if (!_unhandledExceptionRaised)
        {
            _unhandledExceptionRaised = true;
            ContentArea.Content = null;
            ReportError(e.Exception);
        }
        else
        {
            Application.Current.Shutdown();
        }

        e.Handled = true;
    }

    private void AssemblyCacheManager_OnCacheUpdated(object? sender, EventArgs __)
    {
        _logger.LogInformation("Starting parse attempt after assembly cache update...");
        DispatchParseAttempt();
    }

    #endregion Event Handlers

    #region IsValidXaml (DependencyProperty)

    /// <summary>
    /// description of IsValidXaml
    /// </summary>
    public bool IsValidXaml
    {
        get => (bool)GetValue(IsValidXamlProperty);
        set => SetValue(IsValidXamlProperty, value);
    }

    /// <summary>
    /// DependencyProperty for IsValidXaml
    /// </summary>
    public static readonly DependencyProperty IsValidXamlProperty = DependencyProperty.Register(
        nameof(IsValidXaml),
        typeof(bool),
        typeof(WpfDocumentView),
        new FrameworkPropertyMetadata(true, IsValidXamlChanged));

    /// <summary>
    /// PropertyChangedCallback for IsValidXaml
    /// </summary>
    private static void IsValidXamlChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
    {
        if (obj is WpfDocumentView owner)
        {
            if ((bool)args.NewValue)
                owner.HideErrorUi();
            else
                owner.ShowErrorUi();
        }
    }

    #endregion

    #region ErrorText (DependencyProperty)

    /// <summary>
    /// description of ErrorText
    /// </summary>
    public string? ErrorText
    {
        get => (string)GetValue(ErrorTextProperty);
        set => SetValue(ErrorTextProperty, value);
    }

    /// <summary>
    /// DependencyProperty for ErrorText
    /// </summary>
    public static readonly DependencyProperty ErrorTextProperty = DependencyProperty.Register(
        nameof(ErrorText),
        typeof(string),
        typeof(WpfDocumentView),
        new PropertyMetadata(default(string?)));

    #endregion

    #region ErrorLineNumber (DependencyProperty)

    /// <summary>
    /// description of ErrorLineNumber
    /// </summary>
    public int ErrorLineNumber
    {
        get => (int)GetValue(ErrorLineNumberProperty);
        set => SetValue(ErrorLineNumberProperty, value);
    }

    /// <summary>
    /// DependencyProperty for ErrorLineNumber
    /// </summary>
    public static readonly DependencyProperty ErrorLineNumberProperty = DependencyProperty.Register(
        nameof(ErrorLineNumber),
        typeof(int),
        typeof(WpfDocumentView),
        new PropertyMetadata(default(int)));

    #endregion

    #region ErrorLinePosition (DependencyProperty)

    /// <summary>
    /// description of ErrorLinePosition
    /// </summary>
    public int ErrorLinePosition
    {
        get => (int)GetValue(ErrorLinePositionProperty);
        set => SetValue(ErrorLinePositionProperty, value);
    }

    /// <summary>
    /// DependencyProperty for ErrorLinePosition
    /// </summary>
    public static readonly DependencyProperty ErrorLinePositionProperty = DependencyProperty.Register(
        nameof(ErrorLinePosition),
        typeof(int),
        typeof(WpfDocumentView),
        new FrameworkPropertyMetadata(default(int), ErrorLinePositionChanged));

    /// <summary>
    /// PropertyChangedCallback for ErrorLinePosition
    /// </summary>
    private static void ErrorLinePositionChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
    {
        if (obj is WpfDocumentView)
        {
            // handle changed event here
        }
    }

    #endregion

    #region PreviewImage (DependencyProperty)

    public ImageSource PreviewImage
    {
        get => (ImageSource)GetValue(PreviewImageProperty);
        private set => SetValue(PreviewImagePropertyKey, value);
    }

    private static readonly DependencyPropertyKey PreviewImagePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(PreviewImage),
        typeof(ImageSource),
        typeof(WpfDocumentView),
        new UIPropertyMetadata(default(ImageSource)));

    public static readonly DependencyProperty PreviewImageProperty = PreviewImagePropertyKey.DependencyProperty;

    #endregion

    #region Scale (DependencyProperty)

    public double Scale
    {
        get => (double)GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
        nameof(Scale),
        typeof(double),
        typeof(WpfDocumentView),
        new FrameworkPropertyMetadata(1.0));

    #endregion

    #region Event Handlers

    private void Editor_OnTextChanged(object _, TextChangedEventArgs e)
    {
        if (XamlDocument == null || _isSettingSourceText) return;

        _logger.LogDebug("Processing text with length {Length}...", e.Text.Length);
        ClearDispatcherTimer();
        AttemptTagMatchParse();
        DispatchParseAttempt();
    }

    private void Editor_OnUndoStarted(object? _, EventArgs __)
    {
        _undoTriggerCount++;
        _logger.LogDebug("Increment UNDO flag to: {Count}", _undoTriggerCount);
    }

    private void Editor_OnUndoCompleted(object? _, EventArgs __)
    {
        _undoTriggerCount--;
        _logger.LogDebug("Decrement UNDO flag to: {Count}", _undoTriggerCount);
    }

    private void Editor_OnRedoStarted(object? _, EventArgs __)
    {
        _redoTriggerCount++;
        _logger.LogDebug("Increment REDO flag to: {Count}", _redoTriggerCount);
    }

    private void Editor_OnRedoCompleted(object? _, EventArgs __)
    {
        _redoTriggerCount--;
        _logger.LogDebug("Decrement REDO flag to: {Count}", _redoTriggerCount);
    }

    private void ErrorOverlayAnimationCompleted(object? _, EventArgs __)
    {
        // once we're done fading into the "snapshot", we want to 
        // get rid of the existing content so that any really bad 
        // error (like one that is consuming memory) isn't persisted
        ContentArea.Content = null;
    }

    private void ContentAreaRendered(object? _, EventArgs __)
    {
        try
        {
            if (IsValidXaml && XamlDocument is not null) XamlDocument.PreviewImage = RenderHelper.VisualToBitmap(ContentArea, (int)ContentArea.ActualWidth, (int)ContentArea.ActualHeight, null);
        }
        catch (Exception ex)
        {
            _logger.LogError("Exception: {Ex}", ex);
            if (ex.IsCriticalException()) throw;
        }
    }

    private void LineNumberClick(object? _, RoutedEventArgs __)
    {
        Editor.SelectLine(ErrorLineNumber - 1);
        Editor.Focus();
        Editor.TextEditor.Focus();
    }

    #endregion

    #region Private Methods

    private void ClearDispatcherTimer()
    {
        if (_dispatcherTimer == null) return;
        _dispatcherTimer.Stop();
        _dispatcherTimer = null;
        _logger.LogDebug("Cleared Dispatcher Timer");
    }

    private void DispatchParseAttempt()
    {
        if (Settings.Default.EnableAutoParse is false) return;

        ClearDispatcherTimer();
        _dispatcherTimer = new DispatcherTimer(
            TimeSpan.FromSeconds(Settings.Default.AutoParseTimeout),
            DispatcherPriority.ApplicationIdle,
            ParseCallback,
            Dispatcher.CurrentDispatcher);

        _logger.LogInformation("Started new Dispatcher Timer");
    }

    private void ParseCallback(object? _, EventArgs __)
    {
        Parse(false);
    }

    private void Parse(bool isExplicit)
    {
        _logger.LogInformation("Parse attempt as explicit = {Explicit} started...", isExplicit);

        ClearDispatcherTimer();
        if (XamlDocument?.SourceText == null || CodeCompletionPopup.IsOpenSomewhere)
        {
            _logger.LogDebug("Aborting with is null source text = {IsNull} and is open = {IsOpen}...",
                XamlDocument?.SourceText == null,
                CodeCompletionPopup.IsOpenSomewhere);
            return;
        }

        // handle the in place preparsing (this actually updates the source in the editor)
        var index = TextEditor.CaretIndex;
        XamlDocument.SourceText = PreParse(XamlDocument.SourceText);
        TextEditor.CaretIndex = index;

        var str = XamlDocument.SourceText;

        try
        {
            //Load the XAML to memory
            using var memoryStream = new MemoryStream(str.Length);
            using var streamWriter = new StreamWriter(memoryStream);
            streamWriter.Write(str);
            streamWriter.Flush();
            memoryStream.Seek(0, SeekOrigin.Begin);

            ContentArea.JournalOwnership = JournalOwnership.UsesParentJournal;

            //Use the newer XamlXmlReader to better support dynamic assembly loading
            var assemblies = _assemblyCacheManager.SnapshotCurrentAssemblies();
            var context = new XamlSchemaContext(assemblies);
            using var reader = new XamlXmlReader(memoryStream, context);
            using var writer = new XamlObjectWriter(context);
            XamlServices.Transform(reader, writer);

            var content = writer.Result;
            if (content is Window window)
            {
                window.Owner = Application.Current.MainWindow;

                if (!isExplicit)
                {
                    var bd = new Border
                    {
                        Background = _defaultBackgroundBrush
                    };

                    var tb = new TextBlock
                    {
                        FontFamily = new FontFamily("Segoe, Segoe UI, Verdana"),
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 14,
                        Foreground = Brushes.White,
                        MaxWidth = 320,
                        Margin = new Thickness(50),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Text = "The root element of this content is of type Window.  Press F5 to show the content in a new window."
                    };

                    bd.Child = tb;
                    ContentArea.Content = bd;
                }
                else
                {
                    window.Show();
                }
            }
            else
            {
                ContentArea.Content = content;
            }

            IsValidXaml = true;
            ErrorText = null;
            ErrorLineNumber = 0;
            ErrorLinePosition = 0;
            _unhandledExceptionRaised = false;

            if (Settings.Default.EnableAutoBackup) XamlDocument?.SaveBackup();
        }

        catch (Exception ex)
        {
            if (ex.IsCriticalException()) throw;
            ReportError(ex);
        }

        _logger.LogInformation("Parse attempt as explicit = {Explicit} complete.", isExplicit);
    }

    private string PreParse(string str)
    {
        while (str.Contains("?COLOR")) str = ReplaceOnce(str, "?COLOR", GetRandomColor().ToString());
        while (str.Contains("?NAMEDCOLOR")) str = ReplaceOnce(str, "?NAMEDCOLOR", GetRandomColorName());
        return str;
    }

    /// <summary>
    /// See if a single unmatched XML pair is present and sync.
    /// </summary>
    /// <remarks>
    /// This attempts to perform an XML brace match as the user is modifying a Tag that is not
    /// self-closing.  To do this, it performs an audit on all XML tags and if there is one and
    /// only one unmatched XML Tag set, it will replace the name of the other tag based on the
    /// current cursor position.  Note that this method will abort if inside an undo/redo event
    /// after clearing the <see cref="_undoTriggerCount"/>/<see cref="_redoTriggerCount"/>.
    /// </remarks>
    private void AttemptTagMatchParse()
    {
        if (Settings.Default.EnableAutomaticTagNameMatching is false)
        {
            _logger.LogDebug("Aborting since tag matching is disabled.");
            return;
        }

        if (_undoTriggerCount is not 0)
        {
            _logger.LogInformation("Aborting with UNDO trigger count: {Count}", _undoTriggerCount);
            return;
        }

        if (_redoTriggerCount is not 0)
        {
            _logger.LogDebug("Aborting with REDO trigger count: {Count}", _redoTriggerCount);
            return;
        }

        _logger.LogDebug("Looking for mismatched tags...");

        //Only attempt to match is the user is editing a single tag
        var mismatches = XmlUtilities.AuditXmlTags(TextEditor.Text, 2);
        if (mismatches.Count is not 1)
        {
            _logger.LogDebug("Aborting with mismatched tag count of: {Count}", mismatches.Count);
            return;
        }

        var (openTag, closeTag) = mismatches.First();
        if (openTag is null || closeTag is null)
        {
            _logger.LogDebug("Aborting due to null tag in set: {Open}, {Close}", openTag, closeTag);
            return;
        }

        _logger.LogDebug("Processing single mismatched XAML tags: {Open}, {Close}", openTag, closeTag);

        //Determine tag to use based on current cursor position
        string replaceName;
        int replaceStartIndex;
        int replaceLength;
        var index = TextEditor.CaretIndex;
        var isCloseEdit = closeTag.NameStartIndex <= index && index <= closeTag.NameEndIndex;
        if (isCloseEdit)
        {
            //Within closed tag so replace open name
            replaceName = closeTag.Name;
            replaceStartIndex = openTag.NameStartIndex;
            replaceLength = openTag.NameEndIndex - openTag.NameStartIndex;
        }
        else if (openTag.NameStartIndex <= index && index <= openTag.NameEndIndex)
        {
            //Within open tag so replace closed name
            replaceName = openTag.Name;
            replaceStartIndex = closeTag.NameStartIndex;
            replaceLength = closeTag.NameEndIndex - closeTag.NameStartIndex;
        }
        else
        {
            //Likely a copy/paste so abort
            return;
        }

        //Make sure the index is not beyond the length after any replaces
        _isSettingSourceText = true;
        var old = TextEditor.ReplaceString(replaceStartIndex, replaceLength, replaceName);
        _isSettingSourceText = false;

        _logger.LogInformation("Replaced {TagEnd} XAML Tag '{Orig}' with '{New}' at index {Index} for {Length} chars",
            isCloseEdit ? "opened" : "closed",
            old, replaceName, replaceStartIndex, replaceLength);
    }

    private static string ReplaceOnce(string str, string oldValue, string newValue)
    {
        var index = str.IndexOf(oldValue, StringComparison.Ordinal);
        var s = str;

        s = s.Remove(index, oldValue.Length);
        s = s.Insert(index, newValue);

        return s;
    }

    private Color GetRandomColor() => Color.FromRgb((byte)_r.Next(0, 255), (byte)_r.Next(0, 255), (byte)_r.Next(0, 255));

    private string GetRandomColorName() => Colors[_r.Next(0, Colors.Count - 1)];

    private void ShowErrorUi()
    {
        if (XamlDocument is null) return;

        // update the error image
        ImageSource? src = RenderHelper.ElementToGrayscaleBitmap(ContentArea);
        XamlDocument.PreviewImage = src;

        var c = Color.FromArgb(255, 216, 216, 216);

        if (src is BitmapSource source)
        {
            var croppedSrc = new CroppedBitmap(source, new Int32Rect(0, 0, 1, 1));
            var pixels = new byte[4];
            croppedSrc.CopyPixels(pixels, 4, 0);
            c = Color.FromArgb(255, pixels[0], pixels[1], pixels[2]);
        }

        ErrorOverlayImage.Source = src;
        ErrorOverlay.Background = new SolidColorBrush(c);

        var d = (DoubleAnimation)FindResource("ShowErrorOverlay");
        d.Completed += ErrorOverlayAnimationCompleted;
        ErrorOverlay.BeginAnimation(OpacityProperty, d);
    }

    private void HideErrorUi()
    {
        var d = (DoubleAnimation)FindResource("HideErrorOverlay");
        if (d != null) ErrorOverlay.BeginAnimation(OpacityProperty, d);
    }

    #endregion

    #region IXamlDocumentView Members

    public void Parse()
    {
        Parse(true);
    }

    public void OnActivate()
    {
        KaxamlInfo.Frame = ContentArea;
        Parse();
    }

    public void ReportError(Exception e)
    {
        IsValidXaml = false;

        if (e is XamlParseException exception)
        {
            ErrorLineNumber = exception.LineNumber;
            ErrorLinePosition = exception.LinePosition;
        }
        else
        {
            ErrorLineNumber = 0;
            ErrorLinePosition = 0;
        }

        var inner = e;

        while (inner.InnerException != null) inner = inner.InnerException;

        var message = inner
            .Message
            .Replace("\r", "")
            .Replace("\n", "")
            .Replace("\t", "");

        // get rid of everything after "Line" if it is in the last characters 
        var pos = message.LastIndexOf("Line", StringComparison.InvariantCulture);
        ErrorText = pos > 0 && pos > message.Length - 50
            ? message.Substring(0, pos)
            : message;

        _logger.LogDebug("Parse error reported: {Message}", message);
    }

    /// <summary>
    /// WPF Document DataContext for this WPF Document View control.
    /// </summary>
    public XamlDocument? XamlDocument => DataContext as WpfDocument;

    /// <summary>
    /// Container for the XAML Text being edited by the user.
    /// </summary>
    public IKaxamlInfoTextEditor TextEditor => Editor;

    #endregion
}