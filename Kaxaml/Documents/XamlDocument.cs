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

                var document = new WpfDocument(directory, sourceText);
                document.FullPath = fullPath;
                return document;
            }

            return null;
        }

        #endregion Static Methods


        #region Constructors

        public XamlDocument(string folder)
        {
            _Folder = folder;
        }

        #endregion

        #region Fields

        static string TempFilenamePreface = "default";
        static int TempFilenameCount = 0;

        #endregion

        #region Public Properties

        private string _Folder = "";
        public string Folder
        {
            get => _Folder;
            set
            {
                if (_Folder != value)
                {
                    _Folder = value;
                    NotifyPropertyChanged("Folder");
                    NotifyPropertyChanged("FullPath");
                }
            }
        }

        private string _Filename;
        public string Filename
        {
            get
            {
                if (string.IsNullOrEmpty(_Filename))
                {
                    return TemporaryFilename;
                }
                else
                {
                    return _Filename;
                }
            }
            set
            {
                if (_Filename != value)
                {
                    _Filename = value;
                    NotifyPropertyChanged("Filename");
                    NotifyPropertyChanged("FullPath");
                }
            }
        }

        string _TemporaryFilename = "";
        public string TemporaryFilename
        {
            get
            {
                if (string.IsNullOrEmpty(_TemporaryFilename))
                {
                    string temp = "";
                    if (TempFilenameCount == 0)
                    {
                        temp = TempFilenamePreface + ".xaml";
                    }
                    else
                    {
                        temp = TempFilenamePreface + TempFilenameCount + ".xaml";
                    }
                    _TemporaryFilename = temp;
                    TempFilenameCount++;
                }
                return _TemporaryFilename;
            }
        }


        private string _SourceText;
        public string SourceText
        {
            get => _SourceText;
            set
            {
                if (_SourceText != value)
                {
                    _SourceText = value;
                    NeedsSave = true;
                    NotifyPropertyChanged("SourceText");
                }
            }
        }

        private bool _NeedsSave = false;
        public bool NeedsSave
        {
            get => _NeedsSave;
            private set
            {
                if (_NeedsSave != value)
                {
                    _NeedsSave = value;
                    NotifyPropertyChanged("NeedsSave");
                }
            }
        }

        public bool UsingTemporaryFilename
        {
            get
            {
                return (string.IsNullOrEmpty(_Filename));
            }
        }

        public string FullPath
        {
            get
            {
                if (string.IsNullOrEmpty(Filename))
                {
                    return Path.Combine(Folder, TemporaryFilename);
                }
                else
                {
                    return Path.Combine(Folder, Filename);
                }
            }
            set
            {
                Folder = Path.GetDirectoryName(value);
                Filename = Path.GetFileName(value);
            }
        }

        public string BackupPath
        {
            get
            {
                return Path.Combine(Path.GetDirectoryName(FullPath), Path.GetFileNameWithoutExtension(FullPath) + ".backup");
            }
        }

        private ImageSource _PreviewImage;
        public ImageSource PreviewImage
        {
            get
            {
                if (_PreviewImage == null)
                {
                    // look for a preview image on disk and load it
                    // Path.Combine(Path.GetDirectoryName(FullPath), Path.GetFileNameWithoutExtension(FullPath) + ".preview");
                }

                return _PreviewImage;
            }
            set
            {
                if (_PreviewImage != value)
                {
                    _PreviewImage = value;
                    NotifyPropertyChanged("PreviewImage");
                }
            }
        }

        private XamlDocumentType _xamlDocumentType;
        public XamlDocumentType XamlDocumentType
        {
            get => (_xamlDocumentType); protected set => _xamlDocumentType = value;
        }

        #endregion

        #region Protected, Internal and Private Methods

        protected void InitializeSourceText(string text)
        {
            _SourceText = text;
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

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion
    }

    public enum XamlDocumentType
    {
        WpfDocument,
        AgDocument
    }
}
