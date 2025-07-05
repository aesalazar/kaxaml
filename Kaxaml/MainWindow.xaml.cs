using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Kaxaml.Documents;
using Kaxaml.Plugins.Default;
using KaxamlPlugins;
using Microsoft.Win32;

namespace Kaxaml
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------


        #region Constructors

        public MainWindow()
        {
            InitializeComponent();

            KaxamlInfo.MainWindow = this;

            // initialize commands

            CommandBinding binding = new CommandBinding(ParseCommand);
            binding.Executed += new ExecutedRoutedEventHandler(Parse_Executed);
            binding.CanExecute += new CanExecuteRoutedEventHandler(Parse_CanExecute);
            InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.F5)));
            CommandBindings.Add(binding);

            binding = new CommandBinding(NewWPFTabCommand);
            binding.Executed += new ExecutedRoutedEventHandler(NewWPFTab_Executed);
            binding.CanExecute += new CanExecuteRoutedEventHandler(NewWPFTab_CanExecute);
            InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.T, ModifierKeys.Control, "Ctrl+T")));
            CommandBindings.Add(binding);

            //binding = new CommandBinding(NewAgTabCommand);
            //binding.Executed += new ExecutedRoutedEventHandler(this.NewAgTab_Executed);
            //binding.CanExecute += new CanExecuteRoutedEventHandler(this.NewAgTab_CanExecute);
            //this.InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.L, ModifierKeys.Control, "Ctrl+L")));
            //this.CommandBindings.Add(binding);

            binding = new CommandBinding(CloseTabCommand);
            binding.Executed += new ExecutedRoutedEventHandler(CloseTab_Executed);
            binding.CanExecute += new CanExecuteRoutedEventHandler(CloseTab_CanExecute);
            InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.W, ModifierKeys.Control, "Ctrl+W")));
            CommandBindings.Add(binding);

            binding = new CommandBinding(SaveCommand);
            binding.Executed += new ExecutedRoutedEventHandler(Save_Executed);
            binding.CanExecute += new CanExecuteRoutedEventHandler(Save_CanExecute);
            InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.S, ModifierKeys.Control, "Ctrl+S")));
            CommandBindings.Add(binding);

            binding = new CommandBinding(SaveAsCommand);
            binding.Executed += new ExecutedRoutedEventHandler(SaveAs_Executed);
            binding.CanExecute += new CanExecuteRoutedEventHandler(SaveAs_CanExecute);
            InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Alt, "Ctrl+Alt+S")));
            CommandBindings.Add(binding);

            binding = new CommandBinding(OpenCommand);
            binding.Executed += new ExecutedRoutedEventHandler(Open_Executed);
            binding.CanExecute += new CanExecuteRoutedEventHandler(Open_CanExecute);
            InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.O, ModifierKeys.Control, "Ctrl+O")));
            CommandBindings.Add(binding);

            binding = new CommandBinding(SaveAsImageCommand);
            binding.Executed += new ExecutedRoutedEventHandler(SaveAsImage_Executed);
            binding.CanExecute += new CanExecuteRoutedEventHandler(SaveAsImage_CanExecute);
            InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.I, ModifierKeys.Control, "Ctrl+I")));
            CommandBindings.Add(binding);

            binding = new CommandBinding(ExitCommand);
            binding.Executed += new ExecutedRoutedEventHandler(Exit_Executed);
            binding.CanExecute += new CanExecuteRoutedEventHandler(Exit_CanExecute);
            //this.InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.O, ModifierKeys.Control, "Ctrl+O")));
            CommandBindings.Add(binding);

            // Zoom Commands

            binding = new CommandBinding(ZoomInCommand);
            binding.Executed += new ExecutedRoutedEventHandler(ZoomIn_Executed);
            binding.CanExecute += new CanExecuteRoutedEventHandler(ZoomIn_CanExecute);
            InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.OemPlus, ModifierKeys.Control, "Ctrl++")));
            CommandBindings.Add(binding);

            binding = new CommandBinding(ZoomOutCommand);
            binding.Executed += new ExecutedRoutedEventHandler(ZoomOut_Executed);
            binding.CanExecute += new CanExecuteRoutedEventHandler(ZoomOut_CanExecute);
            InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.OemMinus, ModifierKeys.Control, "Ctrl+-")));
            CommandBindings.Add(binding);

            binding = new CommandBinding(ActualSizeCommand);
            binding.Executed += new ExecutedRoutedEventHandler(ActualSize_Executed);
            binding.CanExecute += new CanExecuteRoutedEventHandler(ActualSize_CanExecute);
            InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.D1, ModifierKeys.Control, "Ctrl+1")));
            CommandBindings.Add(binding);

            // Edit Commands 
            // We have an unusual situation here where we need to handle Copy/Paste/etc. from the menu
            // separately from the built in keyboard commands that you have in control itself.  This
            // is because of some difficulty in getting commands to go accross the WPF/WinForms barrier.
            // One artifact of this is the fact that we want to create the commands without InputGestures
            // because the WinForms controls will handle the keyboards stuff--so this is for Menu only.

            binding = new CommandBinding(CopyCommand);
            binding.Executed += new ExecutedRoutedEventHandler(Copy_Executed);
            binding.CanExecute += new CanExecuteRoutedEventHandler(Copy_CanExecute);
            CommandBindings.Add(binding);

            binding = new CommandBinding(CutCommand);
            binding.Executed += new ExecutedRoutedEventHandler(Cut_Executed);
            binding.CanExecute += new CanExecuteRoutedEventHandler(Cut_CanExecute);
            CommandBindings.Add(binding);

            binding = new CommandBinding(PasteCommand);
            binding.Executed += new ExecutedRoutedEventHandler(Paste_Executed);
            binding.CanExecute += new CanExecuteRoutedEventHandler(Paste_CanExecute);
            CommandBindings.Add(binding);

            binding = new CommandBinding(PasteImageCommand);
            binding.Executed += new ExecutedRoutedEventHandler(PasteImage_Executed);
            binding.CanExecute += new CanExecuteRoutedEventHandler(PasteImage_CanExecute);
            InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.V, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl+Shift+V")));
            CommandBindings.Add(binding);

            binding = new CommandBinding(DeleteCommand);
            binding.Executed += new ExecutedRoutedEventHandler(Delete_Executed);
            binding.CanExecute += new CanExecuteRoutedEventHandler(Delete_CanExecute);
            CommandBindings.Add(binding);

            binding = new CommandBinding(UndoCommand);
            binding.Executed += new ExecutedRoutedEventHandler(Undo_Executed);
            binding.CanExecute += new CanExecuteRoutedEventHandler(Undo_CanExecute);
            CommandBindings.Add(binding);

            binding = new CommandBinding(RedoCommand);
            binding.Executed += new ExecutedRoutedEventHandler(Redo_Executed);
            binding.CanExecute += new CanExecuteRoutedEventHandler(Redo_CanExecute);
            CommandBindings.Add(binding);

            binding = new CommandBinding(FindCommand);
            binding.Executed += new ExecutedRoutedEventHandler(Find_Executed);
            binding.CanExecute += new CanExecuteRoutedEventHandler(Find_CanExecute);
            CommandBindings.Add(binding);

            binding = new CommandBinding(FindNextCommand);
            binding.Executed += new ExecutedRoutedEventHandler(FindNext_Executed);
            binding.CanExecute += new CanExecuteRoutedEventHandler(FindNext_CanExecute);
            InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.F3, ModifierKeys.None, "F3")));
            CommandBindings.Add(binding);

            binding = new CommandBinding(ReplaceCommand);
            binding.Executed += new ExecutedRoutedEventHandler(Replace_Executed);
            binding.CanExecute += new CanExecuteRoutedEventHandler(Replace_CanExecute);
            InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.H, ModifierKeys.Control, "F3")));
            CommandBindings.Add(binding);

            PreviewKeyDown += new KeyEventHandler(MainWindow_PreviewKeyDown);

            // load or create startup documents

            if (App.StartupArgs.Length > 0)
            {
                foreach (string s in App.StartupArgs)
                {
                    if (File.Exists(s))
                    {
                        XamlDocument doc = XamlDocument.FromFile(s);
                        XamlDocuments.Add(doc);
                    }
                }
            }

            if (XamlDocuments.Count == 0)
            {
                WpfDocument doc = new WpfDocument(Directory.GetCurrentDirectory());
                XamlDocuments.Add(doc);
            }

        }

        void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            PluginView.OpenPlugin(e.Key, Keyboard.Modifiers);
        }

        #endregion        //-------------------------------------------------------------------
        //
        //  Dependency Properties
        //
        //-------------------------------------------------------------------


        #region XamlDocuments (DependencyProperty)

        /// <summary>
        /// The collection of XamlDocuments that are currently actively being edited.
        /// </summary>
        public ObservableCollection<XamlDocument> XamlDocuments
        { get => (ObservableCollection<XamlDocument>)GetValue(XamlDocumentsProperty); set => SetValue(XamlDocumentsProperty, value);
        }

        /// <summary>
        /// DependencyProperty for XamlDocuments
        /// </summary>
        public static readonly DependencyProperty XamlDocumentsProperty =
            DependencyProperty.Register("XamlDocuments", typeof(ObservableCollection<XamlDocument>), typeof(MainWindow), new FrameworkPropertyMetadata(new ObservableCollection<XamlDocument>(), new PropertyChangedCallback(XamlDocumentsChanged)));

        /// <summary>
        /// PropertyChangedCallback for XamlDocuments
        /// </summary>
        private static void XamlDocumentsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj is MainWindow)
            {
                MainWindow owner = (MainWindow)obj;
                // handle changed event here
            }
        }

        #endregion        //-------------------------------------------------------------------
        //
        //  Commands
        //
        //-------------------------------------------------------------------


        #region ParseCommand

        public readonly static RoutedUICommand ParseCommand = new RoutedUICommand("_Parse", "ParseCommand", typeof(MainWindow));

        void Parse_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (sender == this)
            {
                if (DocumentsView.SelectedView != null)
                {
                    DocumentsView.SelectedView.Parse();
                }
            }
        }

        void Parse_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            if (sender == this)
            {
                if (DocumentsView.SelectedView != null)
                {
                    args.CanExecute = true;
                }
                else
                {
                    args.CanExecute = false;
                }
            }
        }

        #endregion

        #region NewWPFTabCommand

        public readonly static RoutedUICommand NewWPFTabCommand = new RoutedUICommand("New WPF Tab", "NewWPFTabCommand", typeof(MainWindow));

        void NewWPFTab_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (sender == this)
            {
                WpfDocument doc = new WpfDocument(Directory.GetCurrentDirectory());
                XamlDocuments.Add(doc);

                DocumentsView.SelectedDocument = doc;

            }
        }

        void NewWPFTab_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            if (sender == this)
            {
                args.CanExecute = true;
            }
        }

        #endregion

        #region NewAgTabCommand

        public readonly static RoutedUICommand NewAgTabCommand = new RoutedUICommand("New Silverlight Tab", "NewAgTabCommand", typeof(MainWindow));

        void NewAgTab_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            if (sender == this)
            {
                args.CanExecute = true;
            }
        }

        #endregion

        #region CloseTabCommand

        public readonly static RoutedUICommand CloseTabCommand = new RoutedUICommand("Close Tab", "CloseTabCommand", typeof(MainWindow));

        void CloseTab_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (sender == this)
            {
                XamlDocument? document = null;

                if (args.Parameter != null)
                {
                    document = args.Parameter as XamlDocument;
                }
                else if (DocumentsView.SelectedView != null)
                {
                    document = DocumentsView.SelectedView.XamlDocument;
                }

                if (document != null)
                {
                    if (document.NeedsSave)
                    {
                        MessageBoxResult result = MessageBox.Show("The document " + document.Filename + " has not been saved. Would you like to save it before closing?", "Save Document", MessageBoxButton.YesNoCancel);

                        if (result == MessageBoxResult.Yes) document.Save();
                        if (result == MessageBoxResult.Cancel) return;
                    }

                    DocumentsView.XamlDocuments.Remove(document);
                }
            }
        }

        void CloseTab_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            if (sender == this)
            {
                if (DocumentsView.XamlDocuments.Count > 1)
                {
                    args.CanExecute = true;
                }
                else
                {
                    args.CanExecute = false;
                }
            }
        }


        #endregion

        #region SaveCommand

        public readonly static RoutedUICommand SaveCommand = new RoutedUICommand("_Save", "SaveCommand", typeof(MainWindow));

        void Save_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (sender == this)
            {
                if (DocumentsView.SelectedView != null)
                {
                    XamlDocument document = DocumentsView.SelectedView.XamlDocument;
                    Save(document);
                }
            }
        }

        void Save_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            if (sender == this)
            {
                if (DocumentsView.SelectedView != null)
                {
                    args.CanExecute = true;
                }
                else
                {
                    args.CanExecute = false;
                }
            }
        }

        #endregion

        #region SaveAsCommand

        public readonly static RoutedUICommand SaveAsCommand = new RoutedUICommand("Save As... ", "SaveAsCommand", typeof(MainWindow));

        void SaveAs_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (sender == this)
            {
                if (DocumentsView.SelectedView != null)
                {
                    XamlDocument document = DocumentsView.SelectedView.XamlDocument;
                    SaveAs(document);
                }
            }
        }

        void SaveAs_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            if (sender == this)
            {
                if (DocumentsView.SelectedView != null)
                {
                    args.CanExecute = true;
                }
                else
                {
                    args.CanExecute = false;
                }
            }
        }

        #endregion

        #region SaveAsImageCommand

        public readonly static RoutedUICommand SaveAsImageCommand = new RoutedUICommand("_SaveAsImage", "SaveAsImageCommand", typeof(MainWindow));

        void SaveAsImage_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (sender == this)
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = "Image files (*.png)|*.png|All files (*.*)|*.*"
                };

                if (sfd.ShowDialog(KaxamlInfo.MainWindow) == true)
                {
                    using (FileStream fs = new FileStream(sfd.FileName, FileMode.Create))
                    {
                        BitmapSource rtb = RenderContent();
                        PngBitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(rtb));
                        encoder.Save(fs);
                    }
                }
            }
        }

        void SaveAsImage_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            if (sender == this)
            {
                args.CanExecute = true;
            }
        }

        #endregion

        #region OpenCommand

        public readonly static RoutedUICommand OpenCommand = new RoutedUICommand("_Open", "OpenCommand", typeof(MainWindow));

        void Open_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (sender == this)
            {
                Open();
            }
        }

        void Open_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            if (sender == this)
            {
                args.CanExecute = true;
            }
        }

        #endregion

        #region ExitCommand

        public readonly static RoutedUICommand ExitCommand = new RoutedUICommand("_Exit", "ExitCommand", typeof(MainWindow));

        void Exit_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (sender == this)
            {
                Application.Current.Shutdown();
            }
        }

        void Exit_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            if (sender == this)
            {
                args.CanExecute = true;
            }
        }

        #endregion

        #region ZoomInCommand

        public readonly static RoutedUICommand ZoomInCommand = new RoutedUICommand("Zoom In", "ZoomInCommand", typeof(MainWindow));

        void ZoomIn_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (sender == this)
            {
                StatusView.ZoomIn();
            }
        }

        void ZoomIn_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            if (sender == this)
            {
                args.CanExecute = true;
            }
        }

        #endregion

        #region ZoomOutCommand

        public readonly static RoutedUICommand ZoomOutCommand = new RoutedUICommand("Zoom Out", "ZoomOutCommand", typeof(MainWindow));

        void ZoomOut_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (sender == this)
            {
                StatusView.ZoomOut();
            }
        }

        void ZoomOut_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            if (sender == this)
            {
                args.CanExecute = true;
            }
        }

        #endregion

        #region ActualSizeCommand

        public readonly static RoutedUICommand ActualSizeCommand = new RoutedUICommand("Zoom to Actual Size", "ActualSizeCommand", typeof(MainWindow));

        void ActualSize_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (sender == this)
            {
                StatusView.ActualSize();
            }
        }

        void ActualSize_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            if (sender == this)
            {
                args.CanExecute = true;
            }
        }

        #endregion

        #region PasteCommand

        public readonly static RoutedUICommand PasteCommand = new RoutedUICommand("Paste", "PasteCommand", typeof(MainWindow));

        void Paste_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (sender == this)
            {
                DocumentsView.SelectedView.TextEditor.InsertStringAtCaret(Clipboard.GetText());
            }
        }

        void Paste_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            if (sender == this)
            {
                args.CanExecute = Clipboard.ContainsText();
            }
        }

        #endregion

        #region PasteImageCommand

        public readonly static RoutedUICommand PasteImageCommand = new RoutedUICommand("Paste _Image", "PasteImageCommand", typeof(MainWindow));

        void PasteImage_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (sender == this)
            {
                if (Clipboard.ContainsImage())
                {
                    BitmapSource src = Clipboard.GetImage();

                    if (src != null)
                    {
                        try
                        {
                            // find and/or create the folder

                            string folder = Properties.Settings.Default.PasteImageFolder;
                            string absfolder = "";

                            if (!folder.Contains(":"))
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

                            if (!Directory.Exists(absfolder))
                            {
                                Directory.CreateDirectory(absfolder);
                            }

                            // create a unique filename

                            string filename = Properties.Settings.Default.PasteImageFile;
                            string tempfile = Path.Combine(absfolder, filename);
                            int number = 1;

                            string absfilename = tempfile.Replace("$number$", number.ToString());
                            while (File.Exists(absfilename))
                            {
                                number++;
                                absfilename = tempfile.Replace("$number$", number.ToString());
                            }

                            // save the image from the clipboard

                            using (FileStream fs = new FileStream(absfilename, FileMode.Create))
                            {
                                JpegBitmapEncoder encoder = new JpegBitmapEncoder
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
                            string rfilename = absfilename.Replace(docFolder + "\\", "");

                            // create and insert the xaml

                            string xaml = Properties.Settings.Default.PasteImageXaml;
                            xaml = xaml.Replace("$source$", rfilename);
                            DocumentsView.SelectedView.TextEditor.InsertStringAtCaret(xaml);
                        }
                        catch (Exception ex)
                        {
                            if (ex.IsCriticalException())
                            {
                                throw;
                            }
                        }

                    }
                }
            }
        }

        void PasteImage_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            if (sender == this)
            {
                args.CanExecute = Clipboard.ContainsImage();
            }
        }

        #endregion

        #region CopyCommand

        public readonly static RoutedUICommand CopyCommand = new RoutedUICommand("_Copy", "CopyCommand", typeof(MainWindow));

        void Copy_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (sender == this)
            {
                Clipboard.SetText(DocumentsView.SelectedView.TextEditor.SelectedText);
            }
        }

        void Copy_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            if (sender == this)
            {
                args.CanExecute = !string.IsNullOrEmpty(DocumentsView.SelectedView.TextEditor.SelectedText);
            }
        }

        #endregion

        #region CutCommand

        public readonly static RoutedUICommand CutCommand = new RoutedUICommand("Cu_t", "CutCommand", typeof(MainWindow));

        void Cut_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (sender == this)
            {
                Clipboard.SetText(DocumentsView.SelectedView.TextEditor.SelectedText);
                DocumentsView.SelectedView.TextEditor.ReplaceSelectedText("");
            }
        }

        void Cut_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            if (sender == this)
            {
                args.CanExecute = !string.IsNullOrEmpty(DocumentsView.SelectedView.TextEditor.SelectedText);
            }
        }

        #endregion

        #region DeleteCommand

        public readonly static RoutedUICommand DeleteCommand = new RoutedUICommand("_Delete", "DeleteCommand", typeof(MainWindow));

        void Delete_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (sender == this)
            {
                DocumentsView.SelectedView.TextEditor.ReplaceSelectedText("");
            }
        }

        void Delete_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            if (sender == this)
            {
                args.CanExecute = !string.IsNullOrEmpty(DocumentsView.SelectedView.TextEditor.SelectedText);
            }
        }

        #endregion

        #region RedoCommand

        public readonly static RoutedUICommand RedoCommand = new RoutedUICommand("_Redo", "RedoCommand", typeof(MainWindow));

        void Redo_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (sender == this)
            {
                DocumentsView.SelectedView.TextEditor.Redo();
            }
        }

        void Redo_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            if (sender == this)
            {
                args.CanExecute = true;
            }
        }

        #endregion

        #region UndoCommand

        public readonly static RoutedUICommand UndoCommand = new RoutedUICommand("_Undo", "UndoCommand", typeof(MainWindow));

        void Undo_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (sender == this)
            {
                DocumentsView.SelectedView.TextEditor.Undo();
            }
        }

        void Undo_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            if (sender == this)
            {
                args.CanExecute = true;
            }
        }

        #endregion

        #region FindCommand

        public readonly static RoutedUICommand FindCommand = new RoutedUICommand("_Find", "FindCommand", typeof(MainWindow));

        void Find_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (sender == this)
            {
                PluginView.SelectedPlugin = PluginView.GetFindPlugin();
            }
        }

        void Find_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            if (sender == this)
            {
                args.CanExecute = true;
            }
        }

        #endregion

        #region FindNextCommand

        public readonly static RoutedUICommand FindNextCommand = new RoutedUICommand("_FindNext", "FindNextCommand", typeof(MainWindow));

        void FindNext_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (sender == this)
            {
                var findPlugin = PluginView.GetFindPlugin();
                if (findPlugin?.Root is Find)
                {
                    string findText = ((Find)findPlugin.Root).FindText.Text;
                    KaxamlInfo.Editor?.Find(findText);
                }
            }
        }

        void FindNext_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            if (sender == this)
            {
                args.CanExecute = true;
            }
        }

        #endregion

        #region ReplaceCommand

        public readonly static RoutedUICommand ReplaceCommand = new RoutedUICommand("_Replace", "ReplaceCommand", typeof(MainWindow));

        void Replace_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (sender == this)
            {
                PluginView.SelectedPlugin = PluginView.GetFindPlugin();
            }
        }

        void Replace_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            if (sender == this)
            {
                args.CanExecute = true;
            }
        }

        #endregion        //-------------------------------------------------------------------
        //
        //  Methods
        //
        //-------------------------------------------------------------------


        #region Public Methods


        private OpenFileDialog? _OpenDialog;

        public bool Open()
        {
            if (_OpenDialog == null)
            {
                _OpenDialog = new OpenFileDialog
                {
                    AddExtension = true,
                    DefaultExt = ".xaml",
                    Filter = "XAML files|*.xaml|Backup files|*.backup|All files|*.*",
                    Multiselect = true,
                    CheckFileExists = true,
                    CheckPathExists = true,
                    RestoreDirectory = true
                };
            }

            if (_OpenDialog.ShowDialog() is true)
            {
                XamlDocument? first = null;

                foreach (string s in _OpenDialog.FileNames)
                {
                    XamlDocument doc = XamlDocument.FromFile(s);

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

        private SaveFileDialog? _SaveDialog;

        public bool Save(XamlDocument document)
        {
            if (document.UsingTemporaryFilename)
            {
                return SaveAs(document);
            }
            else
            {
                return document.Save();
            }
        }

        public bool SaveAs(XamlDocument document)
        {
            if (_SaveDialog == null)
            {
                _SaveDialog = new SaveFileDialog
                {
                    AddExtension = true,
                    DefaultExt = ".xaml",
                    Filter = "XAML file|*.xaml|All files|*.*"
                };
            }

            _SaveDialog.FileName = document.Filename;

            if (_SaveDialog.ShowDialog() is true)
            {
                if (document.SaveAs(_SaveDialog.FileName))
                {
                    return true;
                }

                MessageBox.Show("The file could not be saved as " + _SaveDialog.FileName + ".");
                return false;
            }

            return false;
        }

        public void Close(XamlDocument document)
        {

        }

        #endregion

        #region Private Methods

        private BitmapSource RenderContent()
        {
            if (KaxamlInfo.Frame?.Content is not FrameworkElement element)
            {
                element = KaxamlInfo.Frame 
                    ?? throw new Exception("Expecting a FrameworkElement");
            }

            var width = (int)(element.ActualWidth);
            var height = (int)(element.ActualHeight);
            var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(element);

            return rtb;
        }

        #endregion

        #region Overrides

        protected override void OnDrop(DragEventArgs e)
        {
            string[] filenames = (string[])e.Data.GetData("FileDrop", true);

            if ((null != filenames) &&
                (filenames.Length > 0))
            {
                var first = (XamlDocument?)null;
                foreach (string f in filenames)
                {
                    string ext = Path.GetExtension(f).ToLower();
                    if (ext.Equals(".png") || ext.Equals(".jpg") || ext.Equals(".jpeg") || ext.Equals(".bmp") || ext.Equals(".gif"))
                    {
                        // get a relative version of the file name
                        var docFolder = DocumentsView.SelectedDocument?.Folder ?? throw new Exception("Expecting Selected Document");
                        string rfilename = f.Replace(docFolder + "\\", "");

                        // create and insert the xaml
                        string xaml = Properties.Settings.Default.PasteImageXaml;
                        xaml = xaml.Replace("$source$", rfilename);
                        DocumentsView.SelectedView.TextEditor.InsertStringAtCaret(xaml);
                    }
                    else
                    {
                        XamlDocument doc = XamlDocument.FromFile(f);

                        if (doc != null)
                        {
                            DocumentsView.XamlDocuments.Add(doc);
                            if (first == null) first = doc;
                        }
                    }
                }

                if (first != null)
                {
                    DocumentsView.SelectedDocument = first;
                }
            }
        }

        #endregion
    }

    public class WindowTitleConverter : IMultiValueConverter
    {


        #region IValueConverter Members

        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2)
            {
                if (values[0] is string str && values[1] is bool b)
                {
                    string title = str;
                    if (b) title += "*";
                    title += " - Kaxaml";

                    return title;
                }
            }

            return null;
        }

        public object[]? ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {

            return null;
        }

        #endregion
    }
}