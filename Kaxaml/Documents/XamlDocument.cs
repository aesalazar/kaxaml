using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using KaxamlPlugins.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kaxaml.Documents;

public class XamlDocument : INotifyPropertyChanged
{
    private readonly ILogger _logger;

    #region Constructors

    public XamlDocument(string folder)
    {
        _logger = ApplicationDiServiceProvider
            .Services
            .GetRequiredService<ILogger<XamlDocument>>();

        _folder = folder;
        _logger.LogInformation("Created XAML Document at: {Folder}", _folder);
    }

    #endregion

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
            if (string.IsNullOrEmpty(_filename)) return TemporaryFilename;

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
                    temp = TempFilenamePreface + ".xaml";
                else
                    temp = TempFilenamePreface + _tempFilenameCount + ".xaml";
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
        get => Path.Combine(Folder, string.IsNullOrEmpty(Filename) ? TemporaryFilename : Filename);
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
        _logger.LogDebug("Writing text to file: {Path}", fullPath);
        File.WriteAllText(fullPath, SourceText);
        return true;
    }

    #endregion

    #region Public Methods

    public bool SaveAs(string fullPath)
    {
        var success = SaveFile(fullPath);

        if (success)
        {
            NeedsSave = false;
            FullPath = fullPath;
            _logger.LogDebug("File saved to: {Path}", fullPath);
        }
        else
        {
            _logger.LogError("Could not save file to: {Path}", fullPath);
        }

        return success;
    }

    public bool Save()
    {
        var path = FullPath;
        var success = SaveFile(path);

        if (success)
        {
            NeedsSave = false;
            _logger.LogDebug("File saved to: {Path}", path);
        }
        else
        {
            _logger.LogError("Could not save file to: {Path}", path);
        }

        return success;
    }

    public bool SaveBackup()
    {
        var path = BackupPath;
        var success = SaveFile(path);

        if (success)
            _logger.LogDebug("Backup saved to: {Path}", path);
        else
            _logger.LogError("Could not save backup to: {Path}", path);

        return success;
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