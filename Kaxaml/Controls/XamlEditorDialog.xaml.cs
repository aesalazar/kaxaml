using System;
using System.ComponentModel;
using System.Windows;

namespace Kaxaml.Controls;

/// <summary>
/// Interaction logic for XamlEditorDialog.xaml
/// </summary>
public partial class XamlEditorDialog : Window
{
    #region fields

    private static string _returnText = "";

    #endregion

    private static XamlEditorDialog? _instance;

    private bool _closedFromButton;

    public XamlEditorDialog()
    {
        InitializeComponent();
    }

    public static string ShowModal(string text, string title, Window owner)
    {
        if (_instance == null) _instance = new XamlEditorDialog();

        _instance.Owner = owner;
        _instance.Text = text;
        _instance.Title = title;

        var result = _instance.ShowDialog() is true;

        if (result) return _returnText;

        return text;
    }

    private void DoDone(object sender, RoutedEventArgs e)
    {
        _returnText = Text;
        _closedFromButton = true;

        DialogResult = true;
        Close();
        _instance = null;
    }

    private void DoCancel(object sender, RoutedEventArgs e)
    {
        _closedFromButton = true;

        DialogResult = false;
        Close();
        _instance = null;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_closedFromButton)
        {
            var r = MessageBox.Show("Do you want to keep the changes you made?", "Keep Changes?",
                MessageBoxButton.YesNoCancel);

            if (r == MessageBoxResult.Yes)
            {
                _returnText = Text;
            }
            else if (r == MessageBoxResult.No)
            {
                // do nothing
            }
            else
            {
                e.Cancel = true;
            }
        }

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _instance = null;
    }

    #region Text (DependencyProperty)

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(XamlEditorDialog),
            new FrameworkPropertyMetadata(default(string)));

    #endregion
}