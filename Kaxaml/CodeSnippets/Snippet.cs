using System;
using System.ComponentModel;

namespace Kaxaml.CodeSnippets;

public sealed class Snippet : INotifyPropertyChanged
{
    #region Constructors

    public Snippet(string name, string shortcut, string text, SnippetCategory category)
    {
        _name = name;
        _shortcut = shortcut;
        _text = text;
        _category = category;
    }

    #endregion Constructors

    #region Events

    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion Events

    #region Overridden Methods

    public override string ToString() => Text;

    #endregion Overridden Methods

    #region Private Methods

    private void OnPropertyChanged(string info)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
    }

    #endregion Private Methods

    #region Public Methods

    public string IndentedText(int count, bool skipFirstLine)
    {
        var t = Text.Replace("\r\n", "\n");

        if (string.Compare(t, Text, StringComparison.Ordinal) != 0)
        {
            // separate Text into lines
            var lines = t.Split('\n');

            // generate the "indent" string
            var indent = "";
            for (var i = 0; i < count; i++) indent = indent + " ";

            // append indent to the beginning of each string and
            // generate the result string (with newly inserted line ends)

            var result = "";
            for (var i = 0; i < lines.Length; i++)
                if (skipFirstLine && i == 0)
                {
                    result = result + lines[i] + "\r\n";
                }
                else if (i == lines.Length - 1)
                {
                    lines[i] = lines[i].Replace("\n", "");
                    result = result + indent + lines[i];
                }
                else
                {
                    lines[i] = lines[i].Replace("\n", "");
                    result = result + indent + lines[i] + "\r\n";
                }

            return result;
        }

        return Text;
    }

    #endregion Public Methods

    #region Fields

    private string _name;
    private string _shortcut;
    private string _text;

    private SnippetCategory _category;

    #endregion Fields

    #region Properties

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged("Name");
            }
        }
    }

    public string Shortcut
    {
        get => _shortcut;
        set
        {
            if (_shortcut != value)
            {
                _shortcut = value;
                OnPropertyChanged("Shortcut");
            }
        }
    }

    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                OnPropertyChanged("Text");
            }
        }
    }


    public SnippetCategory Category
    {
        get => _category;
        set
        {
            if (_category != value)
            {
                _category = value;
                OnPropertyChanged("Category");
            }
        }
    }

    #endregion Properties
}