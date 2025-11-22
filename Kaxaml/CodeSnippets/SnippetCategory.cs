using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Kaxaml.CodeSnippets;

public sealed class SnippetCategory : INotifyPropertyChanged
{
    #region Fields

    private string _name = string.Empty;

    #endregion Fields

    #region Events

    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion Events

    #region Private Methods

    private void OnPropertyChanged(string info)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
    }

    #endregion Private Methods

    #region Public Methods

    public void AddSnippet(string name, string shortcut, string text)
    {
        var s = new Snippet(name, shortcut, text, this);
        Snippets.Add(s);
    }

    #endregion Public Methods

    #region Properties

    public ObservableCollection<Snippet> Snippets { get; } = [];

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

    #endregion Properties
}