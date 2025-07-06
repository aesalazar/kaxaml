using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;


namespace Kaxaml.Documents
{
    public class XamlDocument : INotifyPropertyChanged
    {

        #region Static Methods

        public static XamlDocument? FromFile(string fullPath)
        {
            if (File.Exists(fullPath))
            {
                var sourceText = File.ReadAllText(fullPath);
                var directory = Path.GetDirectoryName(fullPath) 
                    ?? throw new Exception($"No directory found: {fullPath}");

                var document = new WpfDocument(directory, sourceText)
                {
                    FullPath = fullPath
                };
                return document;
            }

            return null;
        }

        #endregion Static Methods


        #region Constructors

        public XamlDocument(string folder)
        {
            _folder = folder;
        }

        #endregion

        #region Fields

        private const string TempFilenamePreface = "default";
        private static int _tempFilenameCount;

        #endregion

        #region Public Properties

        private string _folder;
        public string Folder
        {
            get => _folder;
            set
            {
                if (_folder != value)
                {
                    _folder = value;
                    NotifyPropertyChanged("Folder");
                    NotifyPropertyChanged("FullPath");
                }
            }
        }

        private string _filename = string.Empty;
        public string Filename
        {
            get
            {
                if (string.IsNullOrEmpty(_filename))
                {
                    return TemporaryFilename;
                }

                return _filename;
            }
            set
            {
                if (_filename != value)
                {
                    _filename = value;
                    NotifyPropertyChanged("Filename");
                    NotifyPropertyChanged("FullPath");
                }
            }
        }

        private string _temporaryFilename = "";
        public string TemporaryFilename
        {
            get
            {
                if (string.IsNullOrEmpty(_temporaryFilename))
                {
                    string temp;
                    if (_tempFilenameCount == 0)
                    {
                        temp = TempFilenamePreface + ".xaml";
                    }
                    else
                    {
                        temp = TempFilenamePreface + _tempFilenameCount + ".xaml";
                    }
                    _temporaryFilename = temp;
                    _tempFilenameCount++;
                }
                return _temporaryFilename;
            }
        }


        private string? _sourceText;
        public string? SourceText
        {
            get => _sourceText;
            set
            {
                if (_sourceText != value)
                {
                    _sourceText = value;
                    NeedsSave = true;
                    NotifyPropertyChanged("SourceText");
                }
            }
        }

        private bool _needsSave;
        public bool NeedsSave
        {
            get => _needsSave;
            private set
            {
                if (_needsSave != value)
                {
                    _needsSave = value;
                    NotifyPropertyChanged("NeedsSave");
                }
            }
        }

        public bool UsingTemporaryFilename => string.IsNullOrEmpty(_filename);

        public string FullPath
        {
            get
            {
                if (string.IsNullOrEmpty(Filename))
                {
                    return Path.Combine(Folder, TemporaryFilename);
                }

                return Path.Combine(Folder, Filename);
            }
            set
            {
                Folder = Path.GetDirectoryName(value) ?? string.Empty;
                Filename = Path.GetFileName(value);
            }
        }

        public string BackupPath => Path.Combine(
            Path.GetDirectoryName(FullPath) ?? string.Empty, 
            Path.GetFileNameWithoutExtension(FullPath) + ".backup");

        private ImageSource? _previewImage;
        public ImageSource? PreviewImage
        {
            get
            {
                if (_previewImage == null)
                {
                    // look for a preview image on disk and load it
                    // Path.Combine(Path.GetDirectoryName(FullPath), Path.GetFileNameWithoutExtension(FullPath) + ".preview");
                }

                return _previewImage;
            }
            set
            {
                if (_previewImage != value)
                {
                    _previewImage = value;
                    NotifyPropertyChanged("PreviewImage");
                }
            }
        }

        public XamlDocumentType XamlDocumentType { get; protected set; }

        #endregion

        #region Protected, Internal and Private Methods

        protected void InitializeSourceText(string text)
        {
            _sourceText = text;
        }

        private bool SaveFile(string fullPath)
        {
            File.WriteAllText(fullPath, SourceText);
            return true;
        }

        #endregion

        #region Public Methods

        public bool SaveAs(string fullPath)
        {
            if (SaveFile(fullPath))
            {
                NeedsSave = false;
                FullPath = fullPath;
                return true;
            }
            return false;
        }

        public bool Save()
        {
            if (SaveFile(FullPath))
            {
                NeedsSave = false;
                return true;
            }
            return false;
        }

        public bool SaveBackup()
        {
            return SaveFile(BackupPath);
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

        #endregion
    }

    public enum XamlDocumentType
    {
        WpfDocument,
        AgDocument
    }
}
