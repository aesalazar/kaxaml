using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
using Kaxaml.Plugins.Default;
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
        _assemblyReferences = ApplicationDiServiceProvider.Services.GetRequiredService<AssemblyReferences>();
        _xamlDocumentManager = ApplicationDiServiceProvider.Services.GetRequiredService<XamlDocumentManager>();

        KaxamlInfo.Frame = ContentArea;
        Dispatcher.UnhandledException += Dispatcher_UnhandledException;
        _assemblyCacheManager.CacheUpdated += AssemblyCacheManager_OnCacheUpdated;

        var schemaFile = Path.Combine(
            Path.GetDirectoryName(ApplicationDiServiceProvider.StartupPath + "\\")
            ?? throw new Exception("Could not determine Startup Path"),
            Settings.Default.WPFSchema);

        Dispatcher.InvokeAsync(() =>
        {
            var ex = XmlCompletionDataProvider.LoadSchema(schemaFile);
            if (ex is not null) _logger.LogError("{File} Could not load Scheme File: {Ex}", XamlDocument?.Filename, ex);
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

    private readonly AssemblyReferences _assemblyReferences;

    private readonly XamlDocumentManager _xamlDocumentManager;

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

    private void Editor_OnTextChanged(object _, TextChangedEventArgs e)
    {
        if (XamlDocument == null || _isSettingSourceText) return;

        _logger.LogDebug("{File} Processing text with length {Length}...", XamlDocument?.Filename, e.Text.Length);
        ClearDispatcherTimer();
        AttemptTagMatchParse();
        DispatchParseAttempt();
    }

    private void Editor_OnUndoStarted(object? _, EventArgs __)
    {
        _undoTriggerCount++;
        _logger.LogDebug("{File} Increment UNDO flag to: {Count}", XamlDocument?.Filename, _undoTriggerCount);
    }

    private void Editor_OnUndoCompleted(object? _, EventArgs __)
    {
        _undoTriggerCount--;
        _logger.LogDebug("{File} Decrement UNDO flag to: {Count}", XamlDocument?.Filename, _undoTriggerCount);
    }

    private void Editor_OnRedoStarted(object? _, EventArgs __)
    {
        _redoTriggerCount++;
        _logger.LogDebug("{File} Increment REDO flag to: {Count}", XamlDocument?.Filename, _redoTriggerCount);
    }

    private void Editor_OnRedoCompleted(object? _, EventArgs __)
    {
        _redoTriggerCount--;
        _logger.LogDebug("{File} Decrement REDO flag to: {Count}", XamlDocument?.Filename, _redoTriggerCount);
    }

    private void ErrorOverlayAnimationCompleted(object? sender, EventArgs __)
    {
        // once we're done fading into the "snapshot", we want to 
        // get rid of the existing content so that any really bad 
        // error (like one that is consuming memory) isn't persisted
        if (sender is AnimationClock clock) clock.Completed -= ErrorOverlayAnimationCompleted;
        ContentArea.Content = null;
    }

    private void ContentArea_ContentRendered(object? _, EventArgs __)
    {
        try
        {
            if (IsValidXaml && XamlDocument is not null)
                XamlDocument.PreviewImage = RenderHelper.VisualToBitmap(
                    ContentArea,
                    (int)ContentArea.ActualWidth,
                    (int)ContentArea.ActualHeight, null);
        }
        catch (Exception ex)
        {
            _logger.LogError("{File} Exception: {Ex}", XamlDocument?.Filename, ex);
            if (ex.IsCriticalException()) throw;
        }

        KaxamlInfo.RaiseContentLoaded();
    }

    private void LineNumberClick(object? _, RoutedEventArgs __)
    {
        Editor.SelectLine(ErrorLineNumber - 1);
        Editor.Focus();
        Editor.TextEditor.Focus();
    }

    private void AssemblyCacheManager_OnCacheUpdated(object? sender, EventArgs __)
    {
        Dispatcher.Invoke(() =>
        {
            if (_xamlDocumentManager.SelectedXamlDocument == XamlDocument)
            {
                _logger.LogInformation("{File} Dispatching parse attempt after assembly cache update...", ReadFileName());
                DispatchParseAttempt();
            }
        });
    }

    #endregion

    #region Private Methods

    private void ClearDispatcherTimer()
    {
        var timer = Interlocked.Exchange(ref _dispatcherTimer, null);
        if (timer == null) return;
        timer.Stop();
        _logger.LogDebug("{File} Cleared Dispatcher Timer", ReadFileName());
    }

    private void DispatchParseAttempt()
    {
        if (Settings.Default.EnableAutoParse is false) return;

        ClearDispatcherTimer();
        var timer = new DispatcherTimer(
            TimeSpan.FromSeconds(Settings.Default.AutoParseTimeout),
            DispatcherPriority.ApplicationIdle,
            DispatcherTimeParseCallback,
            Dispatcher.CurrentDispatcher);

        Interlocked.Exchange(ref _dispatcherTimer, timer);
        _logger.LogInformation("{File} Started new Dispatcher Timer.", ReadFileName());
    }

    private void DispatcherTimeParseCallback(object? _, EventArgs __)
    {
        _logger.LogDebug("{File} Invoking Parse callback Attempt...", XamlDocument?.Filename);
        AttemptParse(false);
    }

    private void AttemptParse(bool isExplicit)
    {
        _logger.LogInformation("{File} Parse attempt as explicit = {Explicit} started...", XamlDocument?.Filename, isExplicit);

        ClearDispatcherTimer();
        if (XamlDocument?.SourceText == null || CodeCompletionPopup.IsOpenSomewhere)
        {
            _logger.LogDebug("{File} Aborting with is null source text = {IsNull} and is open = {IsOpen}...",
                XamlDocument?.Filename,
                XamlDocument?.SourceText == null,
                CodeCompletionPopup.IsOpenSomewhere);
            return;
        }

        // handle the in place preparsing (this actually updates the source in the editor)
        var index = TextEditor.CaretIndex;
        XamlDocument.SourceText = PreParse(XamlDocument.SourceText);
        TextEditor.CaretIndex = index;

        var str = XamlDocument.SourceText;

        //Check for any needed references
        var isAssemblyLoaded = LoadAssemblyReferences(str);
        if (isAssemblyLoaded is not false)
        {
            //something was loaded that will trigger event (or something went wrong)
            _logger.LogDebug(
                "{File} Aborting parse attempt after assembly load result: {Result}",
                XamlDocument?.Filename,
                isAssemblyLoaded is true ? "New Assembly Loaded" : "Assembly Load Failed");
            return;
        }

        //Parse the XML
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

        _logger.LogInformation(
            "{File} Parse attempt as explicit = {Explicit} complete."
            , XamlDocument?.Filename
            , isExplicit);
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
            _logger.LogDebug("{File} Aborting since tag matching is disabled.", XamlDocument?.Filename);
            return;
        }

        if (_undoTriggerCount is not 0)
        {
            _logger.LogInformation("{File} Aborting with UNDO trigger count: {Count}", XamlDocument?.Filename, _undoTriggerCount);
            return;
        }

        if (_redoTriggerCount is not 0)
        {
            _logger.LogDebug("{File} Aborting with REDO trigger count: {Count}", XamlDocument?.Filename, _redoTriggerCount);
            return;
        }

        _logger.LogDebug("{File} Looking for mismatched tags...", XamlDocument?.Filename);

        //Only attempt to match is the user is editing a single tag
        var mismatches = XmlUtilities.AuditXmlTags(TextEditor.Text, 2);
        if (mismatches.Count is not 1)
        {
            _logger.LogDebug("{File} Aborting with mismatched tag count of: {Count}", XamlDocument?.Filename, mismatches.Count);
            return;
        }

        var (openTag, closeTag) = mismatches.First();
        if (openTag is null || closeTag is null)
        {
            _logger.LogDebug("{File} Aborting due to null tag in set: {Open}, {Close}", XamlDocument?.Filename, openTag, closeTag);
            return;
        }

        _logger.LogDebug("{File} Processing single mismatched XAML tags: {Open}, {Close}", XamlDocument?.Filename, openTag, closeTag);

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

        _logger.LogInformation("{File} Replaced {TagEnd} XAML Tag '{Orig}' with '{New}' at index {Index} for {Length} chars",
            XamlDocument?.Filename,
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

    /// <summary>
    /// Scans the passed XAML for any external assembly references and loads them.
    /// </summary>
    /// <param name="xaml">Well-formed XML.</param>
    /// <returns>Indication of a NEW assembly being loaded into memory; indicates that something went wrong.</returns>
    private bool? LoadAssemblyReferences(string? xaml)
    {
        var references = XmlUtilities.FindCommentAssemblyReferences(xaml);
        if (references.Any() is false) return false;

        var missing = new List<string>();
        var loaded = new List<string>();

        foreach (var reference in references)
        {
            if (reference.Exists is false)
            {
                missing.Add(reference.FullName);
                _logger.LogDebug("{File} Missing Assembly: {Name}", XamlDocument?.Filename, reference.FullName);
            }
            else
            {
                loaded.Add(reference.FullName);
                _logger.LogDebug("{File} Loaded Assembly: {Name}", XamlDocument?.Filename, reference.FullName);
            }
        }

        if (missing.Any())
        {
            ReportError(new Exception($"{XamlDocument?.Filename} Could not load Assembly Reference: {missing.First()}"));
            return null;
        }

        return _assemblyReferences.LoadAssemblies(loaded);
    }

    /// <summary>
    /// Read file name in a thread safe way.
    /// </summary>
    private string ReadFileName()
    {
        var fileName = string.Empty;
        Dispatcher.Invoke(() => fileName = XamlDocument?.Filename);
        return fileName;
    }

    #endregion

    #region IXamlDocumentView Members

    public void Parse()
    {
        _logger.LogDebug("{File} Invoking explicit Parse Attempt...", XamlDocument?.Filename);
        AttemptParse(true);
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

        _logger.LogDebug("{File} Parse error reported: {Message}", XamlDocument?.Filename, message);
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