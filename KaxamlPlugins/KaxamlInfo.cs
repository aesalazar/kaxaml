using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace KaxamlPlugins;

/// <summary>
/// Global references for the application.
/// TODO: Find a way to DI some or all of this
/// </summary>
public static class KaxamlInfo
{
    public delegate void ContentLoadedDelegate();

    public delegate void EditSelectionChangedDelegate(string? selectedText);

    public delegate void ParseRequestedDelegate();

    private static IKaxamlInfoTextEditor? _editor;

    private static Window? _mainWindow;

    private static Frame? _frame;

    public static IKaxamlInfoTextEditor? Editor
    {
        get => _editor;
        set
        {
            // remove current event handler
            if (_editor != null) _editor.TextSelectionChanged -= _Editor_TextSelectionChanged;

            _editor = value;

            // add new event handler
            if (_editor != null) _editor.TextSelectionChanged += _Editor_TextSelectionChanged;
        }
    }

    public static Window? MainWindow
    {
        get => _mainWindow;
        set
        {
            _mainWindow = value;
            NotifyPropertyChanged("MainWindow");
        }
    }

    public static Frame? Frame
    {
        get => _frame;
        set
        {
            if (_frame != value)
            {
                _frame = value;
                NotifyPropertyChanged("Frame");
            }
        }
    }

    public static event EditSelectionChangedDelegate? EditSelectionChanged;
    public static event ParseRequestedDelegate? ParseRequested;
    public static event ContentLoadedDelegate? ContentLoaded;

    private static void _Editor_TextSelectionChanged(object sender, RoutedEventArgs e)
    {
        EditSelectionChanged?.Invoke(_editor?.SelectedText);
    }

    public static void Parse()
    {
        var handler = ParseRequested;
        handler?.Invoke();
    }

    public static void RaiseContentLoaded()
    {
        var handler = ContentLoaded;
        handler?.Invoke();
    }


    #region INotifyPropertyChanged

    public static event PropertyChangedEventHandler? PropertyChanged;

    private static void NotifyPropertyChanged(string? info)
    {
        PropertyChanged?.Invoke(null, new PropertyChangedEventArgs(info));
    }

    #endregion
}