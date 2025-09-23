using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Kaxaml.Documents;
using Kaxaml.Plugins.Default;
using Kaxaml.Properties;
using KaxamlPlugins;
using KaxamlPlugins.DependencyInjection;
using KaxamlPlugins.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Kaxaml;

public partial class MainWindow
{
    private readonly ILogger<MainWindow> _logger;

    #region Private Methods

    private BitmapSource RenderContent()
    {
        if (KaxamlInfo.Frame?.Content is not FrameworkElement element)
            element = KaxamlInfo.Frame
                      ?? throw new Exception("Expecting a FrameworkElement");

        var width = (int)element.ActualWidth;
        var height = (int)element.ActualHeight;
        var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(element);

        return rtb;
    }

    #endregion

    #region Overrides

    protected override void OnDrop(DragEventArgs e)
    {
        var filenames = (string[]?)e.Data.GetData("FileDrop", true);

        if (filenames is { Length: > 0 })
        {
            var first = (XamlDocument?)null;
            foreach (var f in filenames)
            {
                var ext = Path.GetExtension(f).ToLower();
                if (ext.Equals(".png") || ext.Equals(".jpg") || ext.Equals(".jpeg") || ext.Equals(".bmp") ||
                    ext.Equals(".gif"))
                {
                    // get a relative version of the file name
                    var docFolder = DocumentsView.SelectedDocument?.Folder ??
                                    throw new Exception("Expecting Selected Document");
                    var rfilename = f.Replace(docFolder + "\\", "");

                    // create and insert the xaml
                    var xaml = Settings.Default.PasteImageXaml;
                    xaml = xaml.Replace("$source$", rfilename);
                    DocumentsView.SelectedView?.TextEditor.InsertStringAtCaret(xaml);
                }
                else
                {
                    var doc = XamlDocument.FromFile(f);

                    if (doc != null)
                    {
                        DocumentsView.XamlDocuments.Add(doc);
                        first ??= doc;
                    }
                }
            }

            if (first != null) DocumentsView.SelectedDocument = first;
        }
    }

    #endregion

    #region Constructors

    public MainWindow(ILogger<MainWindow> logger)
    {
        _logger = logger;
        _logger.LogInformation("Initializing Main Window...");
        InitializeComponent();

        KaxamlInfo.MainWindow = this;

        // initialize commands

        var binding = new CommandBinding(ParseCommand);
        binding.Executed += Parse_Executed;
        binding.CanExecute += Parse_CanExecute;
        InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.F5)));
        CommandBindings.Add(binding);

        binding = new CommandBinding(NewWpfTabCommand);
        binding.Executed += NewWPFTab_Executed;
        binding.CanExecute += NewWPFTab_CanExecute;
        InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.T, ModifierKeys.Control, "Ctrl+T")));
        CommandBindings.Add(binding);

        binding = new CommandBinding(CloseTabCommand);
        binding.Executed += CloseTab_Executed;
        binding.CanExecute += CloseTab_CanExecute;
        InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.W, ModifierKeys.Control, "Ctrl+W")));
        CommandBindings.Add(binding);

        binding = new CommandBinding(CloseTabCommand);
        binding.Executed += CloseTab_Executed;
        binding.CanExecute += CloseTab_CanExecute;
        InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.F4, ModifierKeys.Control, "Ctrl+F4")));
        CommandBindings.Add(binding);

        binding = new CommandBinding(SaveCommand);
        binding.Executed += Save_Executed;
        binding.CanExecute += Save_CanExecute;
        InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.S, ModifierKeys.Control, "Ctrl+S")));
        CommandBindings.Add(binding);

        binding = new CommandBinding(SaveAsCommand);
        binding.Executed += SaveAs_Executed;
        binding.CanExecute += SaveAs_CanExecute;
        InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Alt, "Ctrl+Alt+S")));
        CommandBindings.Add(binding);

        binding = new CommandBinding(OpenCommand);
        binding.Executed += Open_Executed;
        binding.CanExecute += Open_CanExecute;
        InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.O, ModifierKeys.Control, "Ctrl+O")));
        CommandBindings.Add(binding);

        binding = new CommandBinding(SaveAsImageCommand);
        binding.Executed += SaveAsImage_Executed;
        binding.CanExecute += SaveAsImage_CanExecute;
        InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.I, ModifierKeys.Control, "Ctrl+I")));
        CommandBindings.Add(binding);

        binding = new CommandBinding(ExitCommand);
        binding.Executed += Exit_Executed;
        binding.CanExecute += Exit_CanExecute;
        //this.InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.O, ModifierKeys.Control, "Ctrl+O")));
        CommandBindings.Add(binding);

        // Zoom Commands

        binding = new CommandBinding(ZoomInCommand);
        binding.Executed += ZoomIn_Executed;
        binding.CanExecute += ZoomIn_CanExecute;
        InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.OemPlus, ModifierKeys.Control, "Ctrl++")));
        CommandBindings.Add(binding);

        binding = new CommandBinding(ZoomOutCommand);
        binding.Executed += ZoomOut_Executed;
        binding.CanExecute += ZoomOut_CanExecute;
        InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.OemMinus, ModifierKeys.Control, "Ctrl+-")));
        CommandBindings.Add(binding);

        binding = new CommandBinding(ActualSizeCommand);
        binding.Executed += ActualSize_Executed;
        binding.CanExecute += ActualSize_CanExecute;
        InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.D1, ModifierKeys.Control, "Ctrl+1")));
        CommandBindings.Add(binding);

        // Edit Commands 
        // We have an unusual situation here where we need to handle Copy/Paste/etc. from the menu
        // separately from the built in keyboard commands that you have in control itself.  This
        // is because of some difficulty in getting commands to go accross the WPF/WinForms barrier.
        // One artifact of this is the fact that we want to create the commands without InputGestures
        // because the WinForms controls will handle the keyboards stuff--so this is for Menu only.

        binding = new CommandBinding(CopyCommand);
        binding.Executed += Copy_Executed;
        binding.CanExecute += Copy_CanExecute;
        CommandBindings.Add(binding);

        binding = new CommandBinding(CutCommand);
        binding.Executed += Cut_Executed;
        binding.CanExecute += Cut_CanExecute;
        CommandBindings.Add(binding);

        binding = new CommandBinding(PasteCommand);
        binding.Executed += Paste_Executed;
        binding.CanExecute += Paste_CanExecute;
        CommandBindings.Add(binding);

        binding = new CommandBinding(PasteImageCommand);
        binding.Executed += PasteImage_Executed;
        binding.CanExecute += PasteImage_CanExecute;
        InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.V, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl+Shift+V")));
        CommandBindings.Add(binding);

        binding = new CommandBinding(DeleteCommand);
        binding.Executed += Delete_Executed;
        binding.CanExecute += Delete_CanExecute;
        CommandBindings.Add(binding);

        binding = new CommandBinding(UndoCommand);
        binding.Executed += Undo_Executed;
        binding.CanExecute += Undo_CanExecute;
        CommandBindings.Add(binding);

        binding = new CommandBinding(RedoCommand);
        binding.Executed += Redo_Executed;
        binding.CanExecute += Redo_CanExecute;
        CommandBindings.Add(binding);

        binding = new CommandBinding(FindCommand);
        binding.Executed += Find_Executed;
        binding.CanExecute += Find_CanExecute;
        CommandBindings.Add(binding);

        binding = new CommandBinding(FindNextCommand);
        binding.Executed += FindNext_Executed;
        binding.CanExecute += FindNext_CanExecute;
        InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.F3, ModifierKeys.None, "F3")));
        CommandBindings.Add(binding);

        binding = new CommandBinding(ReplaceCommand);
        binding.Executed += Replace_Executed;
        binding.CanExecute += Replace_CanExecute;
        InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.H, ModifierKeys.Control, "F3")));
        CommandBindings.Add(binding);

        PreviewKeyDown += MainWindow_PreviewKeyDown;

        // load or create startup documents

        if (App.StartupArgs.Length > 0)
            foreach (var s in App.StartupArgs)
            {
                _logger.LogInformation("Apply startup arg: {arg}", s);
                if (File.Exists(s))
                {
                    var doc = XamlDocument.FromFile(s);
                    if (doc is not null) XamlDocuments.Add(doc);
                }
                else
                {
                    _logger.LogInformation("File does not exist: {arg}", s);
                }
            }

        if (XamlDocuments.Count == 0)
        {
            var doc = new WpfDocument(ApplicationDiServiceProvider.TempDirectory);
            XamlDocuments.Add(doc);
            _logger.LogInformation("Created new WPF document: {Path}", doc.FullPath);
        }

        _logger.LogInformation("Initializing Main Window complete.");
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        PluginView.OpenPlugin(e.Key, Keyboard.Modifiers);
    }

    #endregion

    #region XamlDocuments (DependencyProperty)

    /// <summary>
    /// The collection of XamlDocuments that are currently actively being edited.
    /// </summary>
    public ObservableCollection<XamlDocument> XamlDocuments
    {
        get => (ObservableCollection<XamlDocument>)GetValue(XamlDocumentsProperty);
        set => SetValue(XamlDocumentsProperty, value);
    }

    /// <summary>
    /// DependencyProperty for XamlDocuments
    /// </summary>
    public static readonly DependencyProperty XamlDocumentsProperty = DependencyProperty.Register(
        nameof(XamlDocuments),
        typeof(ObservableCollection<XamlDocument>),
        typeof(MainWindow),
        new FrameworkPropertyMetadata(new ObservableCollection<XamlDocument>(), XamlDocumentsChanged));

    /// <summary>
    /// PropertyChangedCallback for XamlDocuments
    /// </summary>
    private static void XamlDocumentsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
    {
        if (obj is MainWindow _)
        {
            // handle changed event here
        }
    }

    #endregion

    #region ParseCommand

    public static readonly RoutedUICommand ParseCommand = new("_Parse", "ParseCommand", typeof(MainWindow));

    private void Parse_Executed(object sender, ExecutedRoutedEventArgs args)
    {
        if (Equals(sender, this)) DocumentsView.SelectedView?.Parse();
    }

    private void Parse_CanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        if (Equals(sender, this)) args.CanExecute = DocumentsView.SelectedView != null;
    }

    #endregion

    #region NewWPFTabCommand

    public static readonly RoutedUICommand NewWpfTabCommand = new("New WPF Tab", "NewWPFTabCommand", typeof(MainWindow));

    private void NewWPFTab_Executed(object sender, ExecutedRoutedEventArgs args)
    {
        if (Equals(sender, this))
        {
            var doc = new WpfDocument(ApplicationDiServiceProvider.TempDirectory);
            XamlDocuments.Add(doc);
            DocumentsView.SelectedDocument = doc;
        }
    }

    private void NewWPFTab_CanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        if (Equals(sender, this)) args.CanExecute = true;
    }

    #endregion

    #region CloseTabCommand

    public static readonly RoutedUICommand CloseTabCommand = new("Close Tab", "CloseTabCommand", typeof(MainWindow));

    private void CloseTab_Executed(object sender, ExecutedRoutedEventArgs args)
    {
        if (Equals(sender, this))
        {
            XamlDocument? document = null;

            if (args.Parameter != null)
                document = args.Parameter as XamlDocument;
            else if (DocumentsView.SelectedView != null)
                document = DocumentsView.SelectedView.XamlDocument;

            if (document != null)
            {
                if (document.NeedsSave)
                {
                    var result = MessageBox.Show(
                        "The document " + document.Filename +
                        " has not been saved. Would you like to save it before closing?", "Save Document",
                        MessageBoxButton.YesNoCancel);

                    if (result == MessageBoxResult.Yes) document.Save();
                    if (result == MessageBoxResult.Cancel) return;
                }

                DocumentsView.XamlDocuments.Remove(document);
            }
        }
    }

    private void CloseTab_CanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        if (Equals(sender, this)) args.CanExecute = DocumentsView.XamlDocuments.Count > 1;
    }

    #endregion

    #region SaveCommand

    public static readonly RoutedUICommand SaveCommand = new("_Save", "SaveCommand", typeof(MainWindow));

    private void Save_Executed(object sender, ExecutedRoutedEventArgs args)
    {
        if (Equals(sender, this))
            if (DocumentsView.SelectedView?.XamlDocument != null)
            {
                var document = DocumentsView.SelectedView.XamlDocument;
                Save(document);
            }
    }

    private void Save_CanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        if (Equals(sender, this)) args.CanExecute = DocumentsView.SelectedView != null;
    }

    #endregion

    #region SaveAsCommand

    public static readonly RoutedUICommand SaveAsCommand = new("Save As... ", "SaveAsCommand", typeof(MainWindow));

    private void SaveAs_Executed(object sender, ExecutedRoutedEventArgs args)
    {
        if (Equals(sender, this))
            if (DocumentsView.SelectedView?.XamlDocument != null)
            {
                var document = DocumentsView.SelectedView.XamlDocument;
                SaveAs(document);
            }
    }

    private void SaveAs_CanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        if (Equals(sender, this)) args.CanExecute = DocumentsView.SelectedView != null;
    }

    #endregion

    #region SaveAsImageCommand

    public static readonly RoutedUICommand SaveAsImageCommand = new("_SaveAsImage", "SaveAsImageCommand", typeof(MainWindow));

    private void SaveAsImage_Executed(object sender, ExecutedRoutedEventArgs args)
    {
        if (Equals(sender, this))
        {
            var sfd = new SaveFileDialog
            {
                Filter = "Image files (*.png)|*.png|All files (*.*)|*.*"
            };

            if (sfd.ShowDialog(KaxamlInfo.MainWindow) == true)
            {
                using var fs = new FileStream(sfd.FileName, FileMode.Create);
                var rtb = RenderContent();
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                encoder.Save(fs);
            }
        }
    }

    private void SaveAsImage_CanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        if (Equals(sender, this)) args.CanExecute = true;
    }

    #endregion

    #region OpenCommand

    public static readonly RoutedUICommand OpenCommand = new("_Open", "OpenCommand", typeof(MainWindow));

    private void Open_Executed(object sender, ExecutedRoutedEventArgs args)
    {
        if (Equals(sender, this)) Open();
    }

    private void Open_CanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        if (Equals(sender, this)) args.CanExecute = true;
    }

    #endregion

    #region ExitCommand

    public static readonly RoutedUICommand ExitCommand = new("_Exit", "ExitCommand", typeof(MainWindow));

    private void Exit_Executed(object sender, ExecutedRoutedEventArgs args)
    {
        if (Equals(sender, this)) Application.Current.Shutdown();
    }

    private void Exit_CanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        if (Equals(sender, this)) args.CanExecute = true;
    }

    #endregion

    #region ZoomInCommand

    public static readonly RoutedUICommand ZoomInCommand = new("Zoom In", "ZoomInCommand", typeof(MainWindow));

    private void ZoomIn_Executed(object sender, ExecutedRoutedEventArgs args)
    {
        if (Equals(sender, this)) StatusView.ZoomIn();
    }

    private void ZoomIn_CanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        if (Equals(sender, this)) args.CanExecute = true;
    }

    #endregion

    #region ZoomOutCommand

    public static readonly RoutedUICommand ZoomOutCommand = new("Zoom Out", "ZoomOutCommand", typeof(MainWindow));

    private void ZoomOut_Executed(object sender, ExecutedRoutedEventArgs args)
    {
        if (Equals(sender, this)) StatusView.ZoomOut();
    }

    private void ZoomOut_CanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        if (Equals(sender, this)) args.CanExecute = true;
    }

    #endregion

    #region ActualSizeCommand

    public static readonly RoutedUICommand ActualSizeCommand = new("Zoom to Actual Size", "ActualSizeCommand", typeof(MainWindow));

    private void ActualSize_Executed(object sender, ExecutedRoutedEventArgs args)
    {
        if (Equals(sender, this)) StatusView.ActualSize();
    }

    private void ActualSize_CanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        if (Equals(sender, this)) args.CanExecute = true;
    }

    #endregion

    #region PasteCommand

    public static readonly RoutedUICommand PasteCommand = new("Paste", "PasteCommand", typeof(MainWindow));

    private void Paste_Executed(object sender, ExecutedRoutedEventArgs args)
    {
        if (Equals(sender, this)) DocumentsView.SelectedView?.TextEditor.InsertStringAtCaret(Clipboard.GetText());
    }

    private void Paste_CanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        if (Equals(sender, this)) args.CanExecute = Clipboard.ContainsText();
    }

    #endregion

    #region PasteImageCommand

    public static readonly RoutedUICommand PasteImageCommand = new("Paste _Image", "PasteImageCommand", typeof(MainWindow));

    private void PasteImage_Executed(object sender, ExecutedRoutedEventArgs args)
    {
        if (Equals(sender, this))
            if (Clipboard.ContainsImage())
            {
                var src = Clipboard.GetImage();

                if (src != null)
                    try
                    {
                        // find and/or create the folder
                        var folder = Settings.Default.PasteImageFolder;
                        string absfolder;

                        if (!folder.Contains(':'))
                        {
                            var selectedDocFolder = DocumentsView.SelectedDocument?.Folder
                                                    ?? throw new Exception("Expecting Selected Document");
                            absfolder = Path.Combine(selectedDocFolder, folder);
                        }
                        else
                        {
                            absfolder = folder;
                        }

                        // create the folder if it doesn't exist
                        if (!Directory.Exists(absfolder)) Directory.CreateDirectory(absfolder);

                        // create a unique filename
                        var filename = Settings.Default.PasteImageFile;
                        var tempfile = Path.Combine(absfolder, filename);
                        var number = 1;

                        var absfilename = tempfile.Replace("$number$", number.ToString());
                        while (File.Exists(absfilename))
                        {
                            number++;
                            absfilename = tempfile.Replace("$number$", number.ToString());
                        }

                        // save the image from the clipboard
                        using (var fs = new FileStream(absfilename, FileMode.Create))
                        {
                            var encoder = new JpegBitmapEncoder
                            {
                                QualityLevel = 100
                            };

                            //PngBitmapEncoder encoder = new PngBitmapEncoder();
                            //encoder.Interlace = PngInterlaceOption.Off;
                            encoder.Frames.Add(BitmapFrame.Create(src));
                            encoder.Save(fs);
                        }

                        // get a relative version of the file name
                        var docFolder = DocumentsView.SelectedDocument?.Folder
                                        ?? throw new Exception("Expecting Selected Document");
                        var rfilename = absfilename.Replace(docFolder + "\\", "");

                        // create and insert the xaml
                        var xaml = Settings.Default.PasteImageXaml;
                        xaml = xaml.Replace("$source$", rfilename);
                        DocumentsView.SelectedView?.TextEditor.InsertStringAtCaret(xaml);
                    }
                    catch (Exception ex)
                    {
                        if (ex.IsCriticalException()) throw;
                    }
            }
    }

    private void PasteImage_CanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        if (Equals(sender, this)) args.CanExecute = Clipboard.ContainsImage();
    }

    #endregion

    #region CopyCommand

    public static readonly RoutedUICommand CopyCommand = new("_Copy", "CopyCommand", typeof(MainWindow));

    private void Copy_Executed(object sender, ExecutedRoutedEventArgs args)
    {
        if (Equals(sender, this))
        {
            var text = DocumentsView?.SelectedView?.TextEditor.SelectedText;
            if (text is not null) Clipboard.SetText(text);
        }
    }

    private void Copy_CanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        if (Equals(sender, this))
            args.CanExecute = !string.IsNullOrEmpty(DocumentsView.SelectedView?.TextEditor.SelectedText);
    }

    #endregion

    #region CutCommand

    public static readonly RoutedUICommand CutCommand = new("Cu_t", "CutCommand", typeof(MainWindow));

    private void Cut_Executed(object sender, ExecutedRoutedEventArgs args)
    {
        if (Equals(sender, this))
        {
            var text = DocumentsView?.SelectedView?.TextEditor.SelectedText;
            if (text is not null) Clipboard.SetText(text);
            DocumentsView?.SelectedView?.TextEditor.ReplaceSelectedText("");
        }
    }

    private void Cut_CanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        if (Equals(sender, this))
        {
            var text = DocumentsView?.SelectedView?.TextEditor.SelectedText;
            args.CanExecute = !string.IsNullOrEmpty(text);
        }
    }

    #endregion

    #region DeleteCommand

    public static readonly RoutedUICommand DeleteCommand = new("_Delete", "DeleteCommand", typeof(MainWindow));

    private void Delete_Executed(object sender, ExecutedRoutedEventArgs args)
    {
        if (Equals(sender, this)) DocumentsView?.SelectedView?.TextEditor.ReplaceSelectedText("");
    }

    private void Delete_CanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        if (Equals(sender, this))
            args.CanExecute = !string.IsNullOrEmpty(DocumentsView?.SelectedView?.TextEditor.SelectedText);
    }

    #endregion

    #region RedoCommand

    public static readonly RoutedUICommand RedoCommand = new("_Redo", "RedoCommand", typeof(MainWindow));

    private void Redo_Executed(object sender, ExecutedRoutedEventArgs args)
    {
        if (Equals(sender, this)) DocumentsView?.SelectedView?.TextEditor.Redo();
    }

    private void Redo_CanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        if (Equals(sender, this)) args.CanExecute = true;
    }

    #endregion

    #region UndoCommand

    public static readonly RoutedUICommand UndoCommand = new("_Undo", "UndoCommand", typeof(MainWindow));

    private void Undo_Executed(object sender, ExecutedRoutedEventArgs args)
    {
        if (Equals(sender, this)) DocumentsView?.SelectedView?.TextEditor.Undo();
    }

    private void Undo_CanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        if (Equals(sender, this)) args.CanExecute = true;
    }

    #endregion

    #region FindCommand

    public static readonly RoutedUICommand FindCommand = new("_Find", "FindCommand", typeof(MainWindow));

    private void Find_Executed(object sender, ExecutedRoutedEventArgs args)
    {
        if (Equals(sender, this)) PluginView.SelectedPlugin = PluginView.GetFindPlugin();
    }

    private void Find_CanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        if (Equals(sender, this)) args.CanExecute = true;
    }

    #endregion

    #region FindNextCommand

    public static readonly RoutedUICommand FindNextCommand = new("_FindNext", "FindNextCommand", typeof(MainWindow));

    private void FindNext_Executed(object sender, ExecutedRoutedEventArgs args)
    {
        if (Equals(sender, this))
        {
            var findPlugin = PluginView.GetFindPlugin();
            if (findPlugin?.Root is Find find)
            {
                var findText = find.FindText.Text;
                KaxamlInfo.Editor?.Find(findText);
            }
        }
    }

    private void FindNext_CanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        if (Equals(sender, this)) args.CanExecute = true;
    }

    #endregion

    #region ReplaceCommand

    public static readonly RoutedUICommand ReplaceCommand = new("_Replace", "ReplaceCommand", typeof(MainWindow));

    private void Replace_Executed(object sender, ExecutedRoutedEventArgs args)
    {
        if (Equals(sender, this)) PluginView.SelectedPlugin = PluginView.GetFindPlugin();
    }

    private void Replace_CanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        if (Equals(sender, this)) args.CanExecute = true;
    }

    #endregion

    #region Public Methods

    private OpenFileDialog? _openDialog;

    public bool Open()
    {
        if (_openDialog == null)
            _openDialog = new OpenFileDialog
            {
                AddExtension = true,
                DefaultExt = ".xaml",
                Filter = "XAML files|*.xaml|Backup files|*.backup|All files|*.*",
                Multiselect = true,
                CheckFileExists = true,
                CheckPathExists = true,
                RestoreDirectory = true
            };

        if (_openDialog.ShowDialog() is true)
        {
            XamlDocument? first = null;

            foreach (var s in _openDialog.FileNames)
            {
                var doc = XamlDocument.FromFile(s);

                if (doc != null)
                {
                    DocumentsView.XamlDocuments.Add(doc);
                    if (first == null) first = doc;
                }
            }

            DocumentsView.SelectedDocument = first;

            return true;
        }

        return false;
    }

    private SaveFileDialog? _saveDialog;

    public bool Save(XamlDocument document)
    {
        var saved = document.UsingTemporaryFilename
            ? SaveAs(document)
            : document.Save();

        _logger.LogInformation(
            "Saved '{Path}': {Result}",
            document.FullPath,
            saved);

        return saved;
    }

    public bool SaveAs(XamlDocument document)
    {
        _saveDialog ??= new SaveFileDialog
        {
            AddExtension = true,
            DefaultExt = ".xaml",
            Filter = "XAML file|*.xaml|All files|*.*"
        };

        _saveDialog.FileName = document.Filename;

        if (_saveDialog.ShowDialog() is not true) return false;
        if (document.SaveAs(_saveDialog.FileName))
        {
            _logger.LogInformation("Saved '{Path}'.", document.FullPath);
            return true;
        }

        MessageBox.Show("The file could not be saved as " + _saveDialog.FileName + ".");
        _logger.LogInformation("Could not save '{Path}'.", document.FullPath);
        return false;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        var dirty = XamlDocuments
            .Where(d => d.NeedsSave)
            .ToList();

        if (!dirty.Any())
        {
            _logger.LogInformation("No dirty documents so processing with close...");
            return;
        }

        var sb = new StringBuilder();
        sb.Append(dirty.Count > 1
            ? $"There are {dirty.Count} unsaved documents."
            : $"Document '{dirty.First().Filename}' is unsaved.");
        sb.Append("  Are you sure you wish to exit and lose all changes?");

        var result = MessageBox.Show(
            sb.ToString(),
            "Confirm Closing",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);

        if (result is MessageBoxResult.Cancel)
        {
            _logger.LogInformation("Close aborted at users request due to unsaved documents.");
            e.Cancel = true;
        }
        else
        {
            _logger.LogInformation(
                "User confirmed abandoning {Count} unsaved document(s) so processing with close...",
                dirty.Count);
        }
    }

    #endregion
}