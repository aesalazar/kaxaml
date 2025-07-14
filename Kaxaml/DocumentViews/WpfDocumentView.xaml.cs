using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
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

namespace Kaxaml.DocumentViews;

public partial class WpfDocumentView : IXamlDocumentView
{
    #region Constructors

    public WpfDocumentView()
    {
        InitializeComponent();
        _logger = ApplicationDiServiceProvider.Services.GetRequiredService<ILogger<WpfDocumentView>>();

        KaxamlInfo.Frame = ContentArea;
        ContentArea.ContentRendered += ContentArea_ContentRendered;

        Dispatcher.UnhandledException += Dispatcher_UnhandledException;

        var schemaFile = Path.Combine(
            Path.GetDirectoryName(App.StartupPath + "\\")
            ?? throw new Exception("Could not determine Startup Path"),
            Settings.Default.WPFSchema);

        Dispatcher.InvokeAsync(() =>
        {
            var ex = XmlCompletionDataProvider.LoadSchema(schemaFile);
            if (ex is not null) _logger.LogError("Could not load Scheme File: {Ex}", ex);
        });

        _logger.LogInformation(
            "Initialized WPF Document View with call to load Schema path: {SchemaFile}",
            schemaFile);
    }

    #endregion Constructors

    #region Static Fields

    //-------------------------------------------------------------------
    //
    //  Private Fields
    //
    //-------------------------------------------------------------------

    private static DispatcherTimer? _dispatcherTimer;

    private readonly ILogger<WpfDocumentView> _logger;

    #endregion Static Fields

    #region Fields

    private bool _unhandledExceptionRaised;

    private readonly Brush _defaultBackgroundBrush = new SolidColorBrush(Color.FromRgb(0x40, 0x40, 0x40));

    #endregion Fields

    #region Event Handlers

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

    #endregion Event Handlers

    //-------------------------------------------------------------------
    //
    //  Properties
    //
    //-------------------------------------------------------------------


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

    //-------------------------------------------------------------------
    //
    //  Event Handlers
    //
    //-------------------------------------------------------------------

    #region Event Handlers

    private void EditorTextChanged(object sender, TextChangedEventArgs e)
    {
        if (XamlDocument == null) return;
        if (_isInitializing)
        {
            _isInitializing = false;
        }
        else
        {
            ClearDispatcherTimer();
            AttemptParse();
        }
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

    //-------------------------------------------------------------------
    //
    //  Private Methods
    //
    //-------------------------------------------------------------------

    #region Private Methods

    private static void ClearDispatcherTimer()
    {
        if (_dispatcherTimer != null)
        {
            _dispatcherTimer.Stop();
            _dispatcherTimer = null;
        }
    }

    private void AttemptParse()
    {
        if (Settings.Default.EnableAutoParse)
        {
            ClearDispatcherTimer();

            var timeout = new TimeSpan(0, 0, 0, Settings.Default.AutoParseTimeout);

            _dispatcherTimer =
                new DispatcherTimer(
                    timeout,
                    DispatcherPriority.ApplicationIdle,
                    ParseCallback,
                    Dispatcher.CurrentDispatcher);
        }
    }

    private void ParseCallback(object? _, EventArgs __)
    {
        Parse(false);
    }

    private void Parse(bool isExplicit)
    {
        ClearDispatcherTimer();

        if (XamlDocument != null && !CodeCompletionPopup.IsOpenSomewhere)
            if (XamlDocument.SourceText != null)
            {
                // handle the in place preparsing (this actually updates the source in the editor)
                if (TextEditor is not null)
                {
                    var index = TextEditor.CaretIndex;
                    XamlDocument.SourceText = PreParse(XamlDocument.SourceText);
                    TextEditor.CaretIndex = index;
                }

                var str = XamlDocument.SourceText;

                try
                {
                    object? content;
                    using (var ms = new MemoryStream(str.Length))
                    {
                        using (var sw = new StreamWriter(ms))
                        {
                            sw.Write(str);
                            sw.Flush();

                            ms.Seek(0, SeekOrigin.Begin);

                            var pc = new ParserContext
                            {
                                BaseUri = new Uri(XamlDocument?.Folder != null
                                    ? XamlDocument.Folder + "/"
                                    : Environment.CurrentDirectory + "/")
                            };

                            ContentArea.JournalOwnership = JournalOwnership.UsesParentJournal;
                            content = XamlReader.Load(ms, pc);
                        }
                    }

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
            }
    }

    private string PreParse(string str)
    {
        while (str.Contains("?COLOR")) str = ReplaceOnce(str, "?COLOR", GetRandomColor().ToString());
        while (str.Contains("?NAMEDCOLOR")) str = ReplaceOnce(str, "?NAMEDCOLOR", GetRandomColorName());
        return str;
    }

    private string ReplaceOnce(string str, string oldValue, string newValue)
    {
        var index = str.IndexOf(oldValue, StringComparison.Ordinal);
        var s = str;

        s = s.Remove(index, oldValue.Length);
        s = s.Insert(index, newValue);

        return s;
    }

    private readonly Random _r = new();

    private Color GetRandomColor()
    {
        return Color.FromRgb((byte)_r.Next(0, 255), (byte)_r.Next(0, 255), (byte)_r.Next(0, 255));
    }

    private string GetRandomColorName()
    {
        var colors = new[] { "AliceBlue", "Aquamarine", "Azure", "Bisque", "BlanchedAlmond", "Burlywood", "CadetBlue", "Chartreuse", "Chocolate", "Coral", "CornflowerBlue", "Cornsilk", "DodgerBlue", "FloralWhite", "Gainsboro", "Ghostwhite", "Honeydew", "HotPink", "IndianRed", "LightSalmon", "Mintcream", "MistyRose", "Moccasin", "NavajoWhite", "Oldlace", "PapayaWhip", "PeachPuff", "Peru", "SaddleBrown", "Seashell", "Thistle", "Tomato", "WhiteSmoke" };
        return colors[_r.Next(0, colors.Length - 1)];
    }

    private void ReportError(Exception e)
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

    private void ShowErrorUi()
    {
        if (XamlDocument is null) return;
        ImageSource? src;

        if (_isInitializing)
        {
            src = XamlDocument.PreviewImage;
        }
        else
        {
            // update the error image
            src = RenderHelper.ElementToGrayscaleBitmap(ContentArea);
            XamlDocument.PreviewImage = src;
        }

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
        ClearDispatcherTimer();
        Parse(true);
    }

    private bool _isInitializing;

    public void Initialize()
    {
        IsValidXaml = true;
        _isInitializing = true;
        ContentArea.Content = null;

        Parse();
    }

    public void OnActivate()
    {
        KaxamlInfo.Frame = ContentArea;
    }

    public XamlDocument? XamlDocument => DataContext as WpfDocument;

    public IKaxamlInfoTextEditor? TextEditor => Editor;

    #endregion
}